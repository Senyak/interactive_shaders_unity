using UnityEngine;
using UnityEngine.VFX;

public class VFXController :  MonoBehaviour
{
    public VisualEffect visualEffect;
    
    [Header("Pulse Mode")]
    public int burstCount = 20;
    public float pulseInterval = 1.5f;
    
    [Header("Continuous Mode")]
    public float continuousRate = 30f;
    
    private bool isContinuousMode = false;
    private float timer;
    
    private static readonly int SpawnRateID = Shader.PropertyToID("SpawnRate");
    private static readonly int BurstAmountID = Shader.PropertyToID("BurstCount");
    private static readonly int OnBurstEventID = Shader.PropertyToID("OnBurst");
    
    void Start()
    {
        if (visualEffect == null) visualEffect = GetComponent<VisualEffect>();
        
        visualEffect.SetFloat(SpawnRateID, 0f);
        visualEffect.SetUInt(BurstAmountID, (uint)burstCount);
    }
    
    void Update()
    {
        if (!isContinuousMode && visualEffect != null && visualEffect.isActiveAndEnabled)
        {
            timer += Time.deltaTime;
            if (timer >= pulseInterval)
            {
                timer = 0f;
                visualEffect.SendEvent(OnBurstEventID);
            }
        }
    }
    
    public void SetImpulseMode()
    {
        isContinuousMode = false;
        visualEffect.SetFloat(SpawnRateID, 0f);
        timer = 0f;
        if (!visualEffect.isActiveAndEnabled) visualEffect.Play();
    }
    
    public void SetContinuousMode(bool continuous)
    {
        isContinuousMode = continuous;
        visualEffect.SetFloat(SpawnRateID, continuous ? continuousRate : 0f);
        if (continuous && !visualEffect.isActiveAndEnabled) visualEffect.Play();
    }
}