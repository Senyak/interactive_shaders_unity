using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class InteractiveSnow : MonoBehaviour
{
    [Header("Snow Settings")]
    public Vector2Int snowResolution = new Vector2Int(1024, 1024);
    public float meshSize = 10f;
    public float maxSnowHeight = 0.5f;
    public float snowFalloff = 0.25f;
    public float snowEngulf = 0.1f;
    public float radiusMultiplier = 0.5f;
    public int maxSnowActors = 20;

    [Header("Terrain Height Map (optional)")]
    public Texture2D terrainHeightMap;
    
    [Header("Footprint Mask Texture")]
    public Texture2D footprintMaskTex;
    [Range(0f, 360f)] public float maskRotation = 0f;

    [Header("Compute Shader")]
    public ComputeShader snowOffsetShader;

    [Header("Snow Material")]
    public Material snowMaterial;
    
    [Header("Edge Smoothness")]
    [Range(0f, 2f)] public float edgeSmoothness = 1.0f; 

    private RenderTexture snowOffsetTex;
    private Material materialInstance;
    private BoxCollider boxCollider;
    private List<SnowActor> snowActors = new List<SnowActor>();
    private GraphicsBuffer snowActorBuffer;

    private struct SnowActorData
    {
        public Vector3 positionUV;
        public float radiusUV;
    }

    void Start()
    {
        snowOffsetTex = new RenderTexture(snowResolution.x, snowResolution.y, 0, RenderTextureFormat.RFloat);
        snowOffsetTex.enableRandomWrite = true;
        snowOffsetTex.Create();

        boxCollider = GetComponent<BoxCollider>();
        if (boxCollider == null) boxCollider = gameObject.AddComponent<BoxCollider>();
        boxCollider.isTrigger = true;
        Vector3 size = boxCollider.size;
        size.y = maxSnowHeight;
        boxCollider.size = size;
        Vector3 center = boxCollider.center;
        center.y = maxSnowHeight / 2f;
        boxCollider.center = center;

        materialInstance = Instantiate(snowMaterial);
        GetComponent<Renderer>().material = materialInstance;
        materialInstance.SetTexture("_SnowOffsetTex", snowOffsetTex);
        materialInstance.SetFloat("_MaxSnowHeight", maxSnowHeight);

        snowActorBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, maxSnowActors, 16);
        snowOffsetShader.SetFloat("_SnowFalloff", snowFalloff / meshSize);
        snowOffsetShader.SetVector("_SnowResolution", new Vector2(snowResolution.x, snowResolution.y));
        snowOffsetShader.SetInt("_UseTerrainHeightMap", terrainHeightMap != null ? 1 : 0);

        if (terrainHeightMap != null)
            snowOffsetShader.SetTexture(0, "_TerrainHeightTex", terrainHeightMap);

        int kernelInit = snowOffsetShader.FindKernel("InitializeOffsets");
        snowOffsetShader.SetTexture(kernelInit, "_SnowOffsetTex", snowOffsetTex);
        if (terrainHeightMap != null)
            snowOffsetShader.SetTexture(kernelInit, "_TerrainHeightTex", terrainHeightMap);
        snowOffsetShader.Dispatch(kernelInit, snowResolution.x / 8, snowResolution.y / 8, 1);
        snowOffsetShader.SetFloat("_EdgeSmoothness", edgeSmoothness);
        
        int useMask = footprintMaskTex != null ? 1 : 0;
        snowOffsetShader.SetInt("_UseFootprintMask", useMask);
        if (useMask == 1)
        {
            snowOffsetShader.SetTexture(kernelInit, "_FootprintMaskTex", footprintMaskTex);
            snowOffsetShader.SetFloat("_MaskRotation", maskRotation * Mathf.Deg2Rad);
        }
        
    }

    void OnTriggerEnter(Collider other)
    {
        SnowActor actor = other.GetComponent<SnowActor>();
        if (actor != null && snowActors.Count < maxSnowActors && !snowActors.Contains(actor))
            snowActors.Add(actor);
    }

    void OnTriggerExit(Collider other)
    {
        SnowActor actor = other.GetComponent<SnowActor>();
        if (actor != null && snowActors.Contains(actor))
            snowActors.Remove(actor);
    }

    void Update()
    {
        if (snowActors.Count == 0) return;

        UpdateActorBuffer();
        int kernel = snowOffsetShader.FindKernel("ApplyOffsets");
        snowOffsetShader.SetTexture(kernel, "_SnowOffsetTex", snowOffsetTex);
        snowOffsetShader.SetBuffer(kernel, "_SnowActors", snowActorBuffer);
        snowOffsetShader.SetInt("_SnowActorCount", snowActors.Count);
        snowOffsetShader.SetInt("_UseTerrainHeightMap", terrainHeightMap != null ? 1 : 0);
    
        int useMask = footprintMaskTex != null ? 1 : 0;
        snowOffsetShader.SetInt("_UseFootprintMask", useMask);
        if (useMask == 1)
        {
            snowOffsetShader.SetTexture(kernel, "_FootprintMaskTex", footprintMaskTex);
            snowOffsetShader.SetFloat("_MaskRotation", maskRotation * Mathf.Deg2Rad);
        }
    
        if (terrainHeightMap != null)
            snowOffsetShader.SetTexture(kernel, "_TerrainHeightTex", terrainHeightMap);
    
        snowOffsetShader.Dispatch(kernel, snowResolution.x / 8, snowResolution.y / 8, 1);
    }

    void UpdateActorBuffer()
    {
        SnowActorData[] data = new SnowActorData[snowActors.Count];
        for (int i = 0; i < snowActors.Count; i++)
        {
            var actor = snowActors[i];
            Vector3 localPos = transform.InverseTransformPoint(actor.GetGroundPosition());
            float u = 1.0f - ((localPos.x / meshSize) + 0.5f);
            float v = 1.0f - ((localPos.z / meshSize) + 0.5f);
            float heightNormalized = Mathf.Clamp01(localPos.y / maxSnowHeight);
            data[i].positionUV = new Vector3(u, heightNormalized, v);
            float radiusWorld = actor.GetRadius() * radiusMultiplier;
            float radiusUV = (radiusWorld - snowEngulf) / meshSize;
            data[i].radiusUV = radiusUV;
        }
        snowActorBuffer.SetData(data);
    }

    void OnDestroy()
    {
        if (snowOffsetTex != null) snowOffsetTex.Release();
        if (snowActorBuffer != null) snowActorBuffer.Dispose();
        if (materialInstance != null) Destroy(materialInstance);
    }
}