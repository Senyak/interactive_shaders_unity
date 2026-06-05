using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(MeshRenderer))] 
public class MagicTVController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Camera targetCamera; 
    [SerializeField] private Material tvMaterial;

    [Header("Hidden Parameters (fixed)")]
    [SerializeField] private float crtWarp = 0.05f;
    [SerializeField] private float scanlineStrength = 0.3f;
    [SerializeField] private float scanlineScale = 300f;
    [SerializeField] private float vignette = 0.5f;
    [SerializeField] private float noiseAmount = 0.05f;

    [Header("UI Sliders")]
    [SerializeField] private Slider gammaSlider;
    [SerializeField] private Slider hueShiftSlider;
    [SerializeField] private Slider saturationSlider;
    [SerializeField] private Slider contrastSlider;
    [SerializeField] private Slider brightnessSlider;
    [SerializeField] private Slider colorDriftXSlider;
    [SerializeField] private Slider colorDriftYSlider;
    [SerializeField] private Slider chromaNRSlider;
    [SerializeField] private Toggle reverseToggle;
    [SerializeField] private Slider cameraRotZSlider;
    [SerializeField] private Slider cameraOffsetZSlider;

    [Header("Camera Limits")]
    [SerializeField] private float cameraOffsetZMin = -3f;
    [SerializeField] private float cameraOffsetZMax = 3f;

    private float currentCameraRotZ; 
    private float currentCameraOffsetZ; 

    void Start()
    {
        if (tvMaterial == null) return;

        tvMaterial.SetFloat("_CRTWarp", crtWarp);
        tvMaterial.SetFloat("_ScanlineStrength", scanlineStrength);
        tvMaterial.SetFloat("_ScanlineScale", scanlineScale);
        tvMaterial.SetFloat("_Vignette", vignette);
        tvMaterial.SetFloat("_NoiseAmount", noiseAmount);

        gammaSlider.onValueChanged.AddListener(OnGammaChanged);
        hueShiftSlider.onValueChanged.AddListener(OnHueShiftChanged);
        saturationSlider.onValueChanged.AddListener(OnSaturationChanged);
        contrastSlider.onValueChanged.AddListener(OnContrastChanged);
        brightnessSlider.onValueChanged.AddListener(OnBrightnessChanged);
        colorDriftXSlider.onValueChanged.AddListener(OnColorDriftXChanged);
        colorDriftYSlider.onValueChanged.AddListener(OnColorDriftYChanged);
        chromaNRSlider.onValueChanged.AddListener(OnChromaNRChanged);
        reverseToggle.onValueChanged.AddListener(OnReverseChanged);
        cameraRotZSlider.onValueChanged.AddListener(OnCameraRotZChanged);
        cameraOffsetZSlider.onValueChanged.AddListener(OnCameraOffsetZChanged);

        OnGammaChanged(gammaSlider.value);
        OnHueShiftChanged(hueShiftSlider.value);
        OnSaturationChanged(saturationSlider.value);
        OnContrastChanged(contrastSlider.value);
        OnBrightnessChanged(brightnessSlider.value);
        OnColorDriftXChanged(colorDriftXSlider.value);
        OnColorDriftYChanged(colorDriftYSlider.value);
        OnChromaNRChanged(chromaNRSlider.value);
        OnReverseChanged(reverseToggle.isOn);
        OnCameraRotZChanged(cameraRotZSlider.value);
        OnCameraOffsetZChanged(cameraOffsetZSlider.value);
    }

    private float MapGamma(float t) => Mathf.Lerp(0.1f, 0.8f, t);
    private float MapHueShift(float t) => Mathf.Lerp(-0.5f, 0.5f, t);
    private float MapSaturation(float t) => Mathf.Lerp(1.3f, 3.0f, t);
    private float MapContrast(float t) => Mathf.Lerp(0.9f, 1.3f, t);
    private float MapBrightness(float t) => Mathf.Lerp(0.0f, 0.5f, t);
    private float MapColorDrift(float t) => Mathf.Lerp(-1f, 1f, t);   // если шейдер поддерживает -1..1
    private float MapChromaNR(float t) => Mathf.Lerp(0.0f, 1.0f, t);

    public void OnGammaChanged(float value) => tvMaterial.SetFloat("_Gamma", MapGamma(value));
    public void OnHueShiftChanged(float value) => tvMaterial.SetFloat("_HueShift", MapHueShift(value));
    public void OnSaturationChanged(float value) => tvMaterial.SetFloat("_Saturation", MapSaturation(value));
    public void OnContrastChanged(float value) => tvMaterial.SetFloat("_Contrast", MapContrast(value));
    public void OnBrightnessChanged(float value) => tvMaterial.SetFloat("_Brightness", MapBrightness(value));
    public void OnColorDriftXChanged(float value) => tvMaterial.SetFloat("_ColorDriftX", MapColorDrift(value));
    public void OnColorDriftYChanged(float value) => tvMaterial.SetFloat("_ColorDriftY", MapColorDrift(value));
    public void OnChromaNRChanged(float value) => tvMaterial.SetFloat("_ChromaNR", MapChromaNR(value));
    public void OnReverseChanged(bool isOn) => tvMaterial.SetFloat("_Reverse", isOn ? 1f : 0f);

    public void OnCameraRotZChanged(float value)
    {
        currentCameraRotZ = Mathf.Lerp(0f, 360f, value);
        ApplyCameraTransform();
    }

    public void OnCameraOffsetZChanged(float value)
    {
        currentCameraOffsetZ = value;
        ApplyCameraTransform();
    }

    private void ApplyCameraTransform()
    {
        if (targetCamera == null) return;

        Vector3 euler = targetCamera.transform.eulerAngles;
        euler.z = currentCameraRotZ;
        targetCamera.transform.eulerAngles = euler;

        Vector3 localPos = targetCamera.transform.localPosition;
        float mappedOffset = Mathf.Lerp(cameraOffsetZMin, cameraOffsetZMax, currentCameraOffsetZ);
        localPos.z = mappedOffset;
        targetCamera.transform.localPosition = localPos;
    }
}