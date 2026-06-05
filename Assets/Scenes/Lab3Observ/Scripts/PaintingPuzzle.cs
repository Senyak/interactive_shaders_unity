using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;                           // ← обязательно добавьте пространство имён TMP

public class PaintingPuzzle : MonoBehaviour
{
    [Header("Puzzle Settings")]
    public Texture2D referenceTexture;
    [Range(0, 1)] public float requiredCoverage = 0.8f;
    public bool useHSVComparison = true;

    [Header("RGB Tolerance")]
    [Range(0, 0.5f)] public float rgbTolerance = 0.2f;

    [Header("HSV Tolerance")]
    [Range(0, 0.5f)] public float hueTolerance = 0.1f;
    [Range(0, 0.5f)] public float saturationTolerance = 0.2f;
    [Range(0, 0.5f)] public float valueTolerance = 0.2f;

    [Header("Thresholds")]
    [Range(0, 1)] public float alphaThreshold = 0.5f;

    [Header("UI (TextMeshPro)")]
    public TMP_Text coverageText; 
    public TMP_Text successMessageText;

    [Header("Check Trigger")]
    public Key  checkKey = Key.R;
    public bool autoCheckOnStrokeEnd = true;

    private PaintingController paintingController;
    private Texture2D paintReadTexture;
    private bool puzzleCompleted = false;

    void Start()
    {
        paintingController = GetComponent<PaintingController>();
        if (paintingController == null)
        {
            Debug.LogError("PaintingPuzzle требует PaintingController на том же объекте!");
            enabled = false;
            return;
        }

        if (referenceTexture == null)
        {
            Debug.LogError("Reference texture not assigned!");
            enabled = false;
            return;
        }

        int size = paintingController.GetPaintTextureSize();
        paintReadTexture = new Texture2D(size, size, TextureFormat.RGBA32, false);

        if (autoCheckOnStrokeEnd)
        {
            paintingController.OnStrokeEnded += OnStrokeEndedCallback;
        }

        if (successMessageText != null)
            successMessageText.gameObject.SetActive(false);
    }

    

    void Update()
    {
        if (puzzleCompleted) return;
    
        if (Keyboard.current != null && Keyboard.current[checkKey].wasPressedThisFrame)
        {
            CheckPuzzle();
        }
    }

    private void OnStrokeEndedCallback()
    {
        CheckPuzzle(); 
    }
    
    public bool CheckPuzzle()
    {
        if (puzzleCompleted) return true;

        RenderTexture paintRT = paintingController.GetPaintTexture();
        if (paintRT == null) return false;

        RenderTexture.active = paintRT;
        paintReadTexture.ReadPixels(new Rect(0, 0, paintRT.width, paintRT.height), 0, 0);
        paintReadTexture.Apply();
        RenderTexture.active = null;

        int refWidth = referenceTexture.width;
        int refHeight = referenceTexture.height;
        int paintWidth = paintRT.width;
        int paintHeight = paintRT.height;

        float correctPixels = 0;
        float requiredPixels = 0;

        for (int y = 0; y < paintHeight; y++)
        {
            for (int x = 0; x < paintWidth; x++)
            {
                int refX = Mathf.FloorToInt((float)x / paintWidth * refWidth);
                int refY = Mathf.FloorToInt((float)y / paintHeight * refHeight);
                Color refColor = referenceTexture.GetPixel(refX, refY);

                if (refColor.a < 0.5f) continue;
                requiredPixels++;

                Color paintColor = paintReadTexture.GetPixel(x, y);
                if (paintColor.a < alphaThreshold) continue;

                if (ColorsMatch(refColor, paintColor))
                    correctPixels++;
            }
        }

        float coverage = (requiredPixels > 0) ? correctPixels / requiredPixels : 0f;
        float percent = coverage * 100f;

        if (coverageText != null)
            coverageText.text = $"Coverage: {percent:F1}% (target {requiredCoverage * 100}%)";

        bool completed = (coverage >= requiredCoverage);
        if (completed && !puzzleCompleted)
        {
            puzzleCompleted = true;
            Debug.Log("Puzzle completed!");
            if (successMessageText != null)
            {
                successMessageText.text = "Success! Object correctly painted!";
                successMessageText.gameObject.SetActive(true);
            }
        }

        return completed;
    }

    private bool ColorsMatch(Color target, Color actual)
    {
        if (!useHSVComparison)
        {
            float diffR = target.r - actual.r;
            float diffG = target.g - actual.g;
            float diffB = target.b - actual.b;
            float distance = Mathf.Sqrt(diffR * diffR + diffG * diffG + diffB * diffB);
            return distance <= rgbTolerance;
        }
        else
        {
            Color.RGBToHSV(target, out float targetH, out float targetS, out float targetV);
            Color.RGBToHSV(actual, out float actualH, out float actualS, out float actualV);
            float hueDiff = Mathf.Abs(targetH - actualH);
            hueDiff = Mathf.Min(hueDiff, 1f - hueDiff);
            return hueDiff <= hueTolerance &&
                   Mathf.Abs(targetS - actualS) <= saturationTolerance &&
                   Mathf.Abs(targetV - actualV) <= valueTolerance;
        }
    }
    void OnDestroy()
    {
        if (paintingController != null && autoCheckOnStrokeEnd)
            paintingController.OnStrokeEnded -= OnStrokeEndedCallback;
        
        if (paintingController != null && autoCheckOnStrokeEnd)
            paintingController.OnStrokeEnded -= OnStrokeEndedCallback;
    }
}