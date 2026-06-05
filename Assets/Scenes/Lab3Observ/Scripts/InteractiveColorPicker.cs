using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using UnityEngine.InputSystem;

public class InteractiveColorPicker : MonoBehaviour, IPointerDownHandler, IDragHandler
{
    [Header("References")]
    public PaintingController paintingController;
    public Slider hueSlider; 
    public RawImage svImage;
    public Image previewImage; 
    public TMP_Text hexText;

    [Header("Settings")]
    public int pickerSize = 256; 

    private Texture2D svTexture;  
    private float currentHue = 0f;
    private Color currentColor = Color.red;
    private bool isDragging = false;

    private void Start()
    {
        if (paintingController == null)
            paintingController = FindObjectOfType<PaintingController>();

        svTexture = new Texture2D(pickerSize, pickerSize, TextureFormat.RGBA32, false);
        svTexture.filterMode = FilterMode.Bilinear;
        svTexture.wrapMode = TextureWrapMode.Clamp;
        GenerateSVTexture(0f);

        if (svImage != null)
            svImage.texture = svTexture;

        if (hueSlider != null)
        {
            hueSlider.minValue = 0f;
            hueSlider.maxValue = 1f;
            hueSlider.value = currentHue;
            hueSlider.onValueChanged.AddListener(OnHueChanged);
        }

        if (paintingController != null)
            SetColor(paintingController.brushColor);
        else
            SetColor(Color.red);
    }

    private void OnDestroy()
    {
        if (svTexture != null)
            Destroy(svTexture);
    }

    private void OnHueChanged(float hue)
    {
        currentHue = hue;
        GenerateSVTexture(currentHue);
    }

    private void GenerateSVTexture(float hue)
    {
        for (int y = 0; y < pickerSize; y++)
        {
            float v = (float)y / (pickerSize - 1);
            for (int x = 0; x < pickerSize; x++)
            {
                float s = (float)x / (pickerSize - 1);
                Color pixelColor = Color.HSVToRGB(hue, s, v);
                svTexture.SetPixel(x, y, pixelColor);
            }
        }
        svTexture.Apply();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        isDragging = true;
        PickColorFromSV(eventData);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (isDragging)
            PickColorFromSV(eventData);
    }

    private void PickColorFromSV(PointerEventData eventData)
    {
        if (svImage == null) return;

        RectTransform rect = svImage.rectTransform;
        Vector2 localPoint;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(rect, eventData.position, eventData.pressEventCamera, out localPoint);

        Vector2 uv = new Vector2(
            (localPoint.x - rect.rect.x) / rect.rect.width,
            (localPoint.y - rect.rect.y) / rect.rect.height
        );

     
        uv.x = Mathf.Clamp01(uv.x);
        uv.y = Mathf.Clamp01(uv.y);

        float v = uv.y; 
        float s = uv.x;

        Color selected = Color.HSVToRGB(currentHue, s, v);
        SetColor(selected);
    }

    private void SetColor(Color color)
    {
        currentColor = color;
        if (previewImage != null)
            previewImage.color = currentColor;
        if (paintingController != null)
            paintingController.brushColor = currentColor;
        if (hexText != null)
            hexText.text = ColorUtility.ToHtmlStringRGBA(currentColor);
    }

   
}