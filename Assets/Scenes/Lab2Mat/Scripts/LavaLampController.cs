using UnityEngine;
using System.Collections;

public class LavaLampController : MonoBehaviour
{
    [Header("Flask")]
    [SerializeField] private Material flaskMaterial;
    [SerializeField] private Color[] flaskColors;
    [SerializeField] private float emissionIntensity = 2.5f;
    
    [Header("Initial Color")]
    [SerializeField] private bool useFirstColorAsInitial = true;
    [SerializeField] private Color customInitialColor = Color.white;
    
    [Header("VFX Effects")]
    [SerializeField] private GameObject pulsingVFX;
    [SerializeField] private GameObject continuousVFX;
    
    [Header("VFX Settings")]
    [SerializeField] private float vfxRestartDelay = 0.05f;
    
    [Header("Wax Blobs Material")]
    [SerializeField] private LavaLampWaxBouncing waxBouncing;
    [SerializeField] private LavaLampWax waxNormal;
    
    private int currentColorIndex = -1;
    
    void Start()
    {
        if (pulsingVFX != null) pulsingVFX.SetActive(false);
        if (continuousVFX != null) continuousVFX.SetActive(false);
        if (waxBouncing != null) waxBouncing.enabled = false;
        if (waxNormal != null) waxNormal.enabled = false;
        
        
        if (flaskMaterial != null)
        {
            Color initialColor = useFirstColorAsInitial && flaskColors.Length > 0 ? flaskColors[0] : customInitialColor;
            currentColorIndex = useFirstColorAsInitial ? 0 : -1;
            flaskMaterial.SetColor("_EmissionColor", initialColor * emissionIntensity);
            flaskMaterial.EnableKeyword("_EMISSION");
        }
    }
    
    private void DisableWaxNormal()
    {
        if (waxNormal != null && waxNormal.enabled)
        {
            waxNormal.ClearAllBlobs();
            waxNormal.enabled = false;
        }
    }
    
    private void DisableWaxBouncing()
    {
        if (waxBouncing != null && waxBouncing.enabled)
        {
            waxBouncing.ClearAllBlobs();
            waxBouncing.enabled = false;
        }
    }
    
    public void OnButton1()
    {
        if (flaskMaterial != null && flaskColors.Length > 0)
        {
            currentColorIndex = (currentColorIndex + 1) % flaskColors.Length;
            flaskMaterial.SetColor("_EmissionColor", flaskColors[currentColorIndex] * emissionIntensity);
        }
        if (pulsingVFX != null) pulsingVFX.SetActive(false);
        if (continuousVFX != null) continuousVFX.SetActive(false);
        DisableWaxNormal();
        DisableWaxBouncing();
    }
    
    public void OnButton2()
    {
        if (continuousVFX != null) continuousVFX.SetActive(false);
        DisableWaxNormal();
        DisableWaxBouncing();
        if (pulsingVFX != null) StartCoroutine(RestartVFX(pulsingVFX));
    }
    
    public void OnButton3()
    {
        if (pulsingVFX != null) pulsingVFX.SetActive(false);
        DisableWaxNormal();
        DisableWaxBouncing();
        if (continuousVFX != null && !continuousVFX.activeSelf) continuousVFX.SetActive(true);
    }
    
    public void OnButton4()
    {
        if (continuousVFX != null && !continuousVFX.activeSelf) continuousVFX.SetActive(true);
        DisableWaxNormal();  
        if (waxBouncing != null) waxBouncing.enabled = true;
    }
    
    public void OnButton5()
    {
        if (continuousVFX != null && !continuousVFX.activeSelf) continuousVFX.SetActive(true);
        
        if (waxBouncing != null) waxBouncing.enabled = true;
        
        if (waxNormal != null) waxNormal.enabled = true;
    }
    
    private IEnumerator RestartVFX(GameObject vfx)
    {
        bool wasActive = vfx.activeSelf;
        if (wasActive) vfx.SetActive(false);
        yield return new WaitForSeconds(vfxRestartDelay);
        vfx.SetActive(true);
    }
}