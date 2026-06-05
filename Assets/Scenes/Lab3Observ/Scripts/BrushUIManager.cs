using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.InputSystem;

public class BrushUIManager : MonoBehaviour
{
    [Header("References")]
    public PaintingController paintingController;

    [Header("UI Elements")]
    public Image currentColorDisplay;
    public Slider brushSizeSlider;
    public Slider opacitySlider;
    public Button toggleModeButton;
    public TMP_Text modeButtonText;

    [Header("Color Presets")]
    public Button[] colorPresetButtons;
    public Color[] colorPresets;

    [Header("Optional Texts")]
    public TMP_Text brushSizeText;
    public TMP_Text opacityText;

    private void Start()
    {
        if (paintingController == null)
            paintingController = FindObjectOfType<PaintingController>();

        if (paintingController == null)
        {
            Debug.LogError("BrushUIManager: PaintingController not found!");
            enabled = false;
            return;
        }

        if (brushSizeSlider != null)
        {
            brushSizeSlider.minValue = 0.01f;
            brushSizeSlider.maxValue = 0.5f;
            brushSizeSlider.value = paintingController.brushRadiusWorld;
            brushSizeSlider.onValueChanged.AddListener(OnBrushSizeChanged);
            OnBrushSizeChanged(brushSizeSlider.value);
        }

        if (opacitySlider != null)
        {
            opacitySlider.minValue = 0f;
            opacitySlider.maxValue = 1f;
            opacitySlider.value = paintingController.opacity;
            opacitySlider.onValueChanged.AddListener(OnOpacityChanged);
            OnOpacityChanged(opacitySlider.value);
        }

        if (currentColorDisplay != null)
            currentColorDisplay.color = paintingController.brushColor;

        if (colorPresetButtons != null && colorPresets != null)
        {
            int count = Mathf.Min(colorPresetButtons.Length, colorPresets.Length);
            for (int i = 0; i < count; i++)
            {
                int index = i;
                colorPresetButtons[i].onClick.AddListener(() => SetBrushColor(colorPresets[index]));
                
                ColorBlock cb = colorPresetButtons[i].colors;
                cb.normalColor = colorPresets[index];
                cb.selectedColor = colorPresets[index];
                colorPresetButtons[i].colors = cb;
            }
        }

        if (toggleModeButton != null)
            toggleModeButton.onClick.AddListener(ToggleMode);
        
        UpdateModeButton();
    }

    private void OnBrushSizeChanged(float value)
    {
        paintingController.brushRadiusWorld = value;
        if (brushSizeText != null)
            brushSizeText.text = $"Size: {value:F2}";
    }

    private void OnOpacityChanged(float value)
    {
        paintingController.opacity = value;
        if (opacityText != null)
            opacityText.text = $"Opacity: {value * 100:F0}%";
    }

    private void SetBrushColor(Color color)
    {
        paintingController.brushColor = color;
        if (currentColorDisplay != null)
            currentColorDisplay.color = color;

        if (paintingController.currentMode == PaintingController.BrushMode.Erase)
        {
            paintingController.currentMode = PaintingController.BrushMode.Paint;
            UpdateModeButton();
        }
    }

    private void ToggleMode()
    {
        paintingController.currentMode = paintingController.currentMode == PaintingController.BrushMode.Paint
            ? PaintingController.BrushMode.Erase
            : PaintingController.BrushMode.Paint;
        UpdateModeButton();
    }

    private void UpdateModeButton()
    {
        if (modeButtonText != null)
        {
            bool isPaint = paintingController.currentMode == PaintingController.BrushMode.Paint;
            modeButtonText.text = isPaint ? "Brush" : "Eraser";
        }
    }
}