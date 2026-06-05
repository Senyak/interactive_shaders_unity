using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.InputSystem;
using System.Collections.Generic;


public class PaintingController : MonoBehaviour
   {
       public enum BrushMode { Paint, Erase }
   
       [Header("Paint settings")]
       public BrushMode currentMode = BrushMode.Paint;
       public Color brushColor = Color.red;
       [Range(0.01f, 0.5f)] public float brushRadiusWorld = 0.1f;
       [Range(0f, 1f)] public float opacity = 0.8f;
       public int brushType = 0; 
   
       [Header("Brush Texture")]
       public bool useBrushTexture = false;
       public Texture2D brushTexture; 
   
       [Header("Texture resolution")]
       public int paintTextureSize = 1024;
   
       [Header("Continuous drawing (world space)")]
       public float maxStepWorld = 0.05f; 
   
       [Header("Undo")]
       public int maxUndoSteps = 10;
   
       [Header("Overlay material (optional)")]
       public Material overlayMaterial;
       public RenderTexture GetPaintTexture() => paintRT;
       public int GetPaintTextureSize() => paintTextureSize;
       public System.Action OnStrokeEnded;
   
       private MeshFilter targetMeshFilter;
       private MeshRenderer targetRenderer;
       private Material activeTargetMaterial;
       private Material dilateMat;
   
       private RenderTexture paintRT;
       private RenderTexture strokeRT;
       private Material paintBrushWorldMat; 
       private Material applyStrokeMat;
   
       private bool isPainting;
       private Vector3 lastWorldPoint; 
   
       private PlayerControls controls;
       
       private void Awake()
       {
           controls = new PlayerControls();
       }
   
       private void OnEnable()
       {
           controls.Enable();
           controls.Player.Paint.performed += StartPainting;
           controls.Player.Paint.canceled += StopPainting;
       }
   
       private void OnDisable()
       {
           controls.Player.Paint.performed -= StartPainting;
           controls.Player.Paint.canceled -= StopPainting;
           controls.Disable();
       }
   
       private void Start()
       {
           targetMeshFilter = GetComponent<MeshFilter>();
           targetRenderer = GetComponent<MeshRenderer>();
   
           if (targetMeshFilter == null || targetRenderer == null)
           {
               Debug.LogError($"MeshPainterContinuous: на объекте {gameObject.name} отсутствует MeshFilter или MeshRenderer.");
               enabled = false;
               return;
           }
   
           if (overlayMaterial != null)
               activeTargetMaterial = overlayMaterial;
           else
               activeTargetMaterial = targetRenderer.material;
   
           paintRT = new RenderTexture(paintTextureSize, paintTextureSize, 0, RenderTextureFormat.ARGB32);
           paintRT.enableRandomWrite = true;
           paintRT.Create();
           ClearPaintTexture();
   
           strokeRT = new RenderTexture(paintTextureSize, paintTextureSize, 0, RenderTextureFormat.ARGB32);
           strokeRT.enableRandomWrite = true;
           strokeRT.Create();
           ClearStrokeTexture();
   
           Shader brushWorldShader = Shader.Find("Hidden/PaintBrushWorld");
           if (brushWorldShader == null || !brushWorldShader.isSupported)
           {
               Debug.LogError("Brush world shader not found or not supported!");
               enabled = false;
               return;
           }
           paintBrushWorldMat = new Material(brushWorldShader);
           
           if (!paintBrushWorldMat.shader.isSupported)
           {
               Debug.LogError("PaintBrushWorld material shader is not supported on this platform!");
           }
   
           Shader applyStrokeShader = Shader.Find("Hidden/ApplyStroke");
           if (applyStrokeShader == null)
           {
               Debug.LogError("ApplyStroke shader 'Hidden/ApplyStroke' not found!");
               enabled = false;
               return;
           }
           applyStrokeMat = new Material(applyStrokeShader);
   
           activeTargetMaterial.SetTexture("_PaintTex", paintRT);
           activeTargetMaterial.SetTexture("_StrokeTex", strokeRT);
           
           dilateMat = new Material(Shader.Find("Hidden/Dilate"));
           if (dilateMat == null) Debug.LogError("Dilate shader not found!");
       }
   
       private void Update()
       {
           if (!isPainting) return;
   
           Vector2 mousePos = Mouse.current.position.ReadValue();
           Ray ray = Camera.main.ScreenPointToRay(mousePos);
   
           if (Physics.Raycast(ray, out RaycastHit hit) && hit.collider.gameObject == targetMeshFilter.gameObject)
           {
               Vector3 currentWorldPoint = hit.point;
   
               if (lastWorldPoint == Vector3.zero)
               {
                   PaintAtWorldPoint(currentWorldPoint);
               }
               else
               {
                   float dist = Vector3.Distance(currentWorldPoint, lastWorldPoint);
                   if (dist > maxStepWorld)
                   {
                       int steps = Mathf.CeilToInt(dist / maxStepWorld);
                       for (int i = 1; i <= steps; i++)
                       {
                           float t = i / (float)steps;
                           Vector3 interpolatedPoint = Vector3.Lerp(lastWorldPoint, currentWorldPoint, t);
                           PaintAtWorldPoint(interpolatedPoint);
                       }
                   }
                   else
                   {
                       PaintAtWorldPoint(currentWorldPoint);
                   }
               }
               lastWorldPoint = currentWorldPoint;
           }
       }
   
   
       private void StartPainting(InputAction.CallbackContext ctx)
       {
           isPainting = true;
           lastWorldPoint = Vector3.zero;
           ClearStrokeTexture();
           activeTargetMaterial.SetTexture("_StrokeTex", strokeRT);
           activeTargetMaterial.SetInt("_IsErasing", currentMode == BrushMode.Erase ? 1 : 0);
       }
   
       private void StopPainting(InputAction.CallbackContext ctx)
       {
           if (!isPainting) return;
           isPainting = false;
           lastWorldPoint = Vector3.zero;

           DilateTexture(strokeRT, 2);

           ApplyStrokeToPaint();
           ClearStrokeTexture();
           activeTargetMaterial.SetTexture("_StrokeTex", strokeRT);
           OnStrokeEnded?.Invoke();
       }
       
       
       private void DilateTexture(RenderTexture rt, int radius = 2)
       {
           if (dilateMat == null) return;
           RenderTexture temp = RenderTexture.GetTemporary(rt.descriptor);
           dilateMat.SetVector("_TexelSize", new Vector2(1f / rt.width, 1f / rt.height));
           dilateMat.SetInt("_Radius", radius);
           Graphics.Blit(rt, temp, dilateMat);
           Graphics.Blit(temp, rt);
           RenderTexture.ReleaseTemporary(temp);
       }
   
       private void PaintAtWorldPoint(Vector3 worldPoint)
       {
           if (paintBrushWorldMat == null || strokeRT == null) return;
   
           paintBrushWorldMat.SetMatrix("_ObjectToWorld", targetMeshFilter.transform.localToWorldMatrix);
           paintBrushWorldMat.SetVector("_BrushCenterWorld", worldPoint);
           paintBrushWorldMat.SetFloat("_BrushRadius", brushRadiusWorld);
           paintBrushWorldMat.SetFloat("_BrushOpacity", opacity);
           paintBrushWorldMat.SetColor("_BrushColor", brushColor);
           paintBrushWorldMat.SetFloat("_IsErasing", currentMode == BrushMode.Erase ? 1f : 0f);
           paintBrushWorldMat.SetTexture("_MainTex", strokeRT);
   
           if (!paintBrushWorldMat.SetPass(0))
           {
               Debug.LogError("PaintBrushWorld material shader pass 0 is invalid!");
               return;
           }
   
           RenderTexture.active = strokeRT;
           Graphics.SetRenderTarget(strokeRT);
           GL.PushMatrix();
           GL.LoadIdentity();
           GL.LoadPixelMatrix(0, strokeRT.width, strokeRT.height, 0);
           Graphics.DrawMeshNow(targetMeshFilter.sharedMesh, Matrix4x4.identity, 0);
           GL.PopMatrix();
           RenderTexture.active = null;
           
           activeTargetMaterial.SetTexture("_StrokeTex", strokeRT);
           activeTargetMaterial.SetInt("_IsErasing", currentMode == BrushMode.Erase ? 1 : 0);
       }
   
       private void ApplyStrokeToPaint()
       {
           if (strokeRT == null || applyStrokeMat == null) return;
   
           RenderTexture tempRT = RenderTexture.GetTemporary(paintRT.descriptor);
           Graphics.Blit(paintRT, tempRT);
           applyStrokeMat.SetTexture("_StrokeTex", strokeRT);
           applyStrokeMat.SetInt("_IsErasing", currentMode == BrushMode.Erase ? 1 : 0);
           Graphics.Blit(tempRT, paintRT, applyStrokeMat);
           RenderTexture.ReleaseTemporary(tempRT);
   
           activeTargetMaterial.SetTexture("_PaintTex", paintRT);
       }
   
       private void ClearPaintTexture()
       {
           RenderTexture.active = paintRT;
           GL.Clear(false, true, new Color(0, 0, 0, 0));
           RenderTexture.active = null;
       }
   
       private void ClearStrokeTexture()
       {
           RenderTexture.active = strokeRT;
           GL.Clear(false, true, new Color(0, 0, 0, 0));
           RenderTexture.active = null;
       }
   
       
       private void OnDestroy()
       {
           if (paintRT != null) paintRT.Release();
           if (strokeRT != null) strokeRT.Release();
           if (paintBrushWorldMat != null) Destroy(paintBrushWorldMat);
           if (applyStrokeMat != null) Destroy(applyStrokeMat);
           if (controls != null) controls.Dispose();
       }
   
   }