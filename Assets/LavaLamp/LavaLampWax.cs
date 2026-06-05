using UnityEngine;
using System.Collections.Generic;

public class LavaLampWax : MonoBehaviour
{
    public Transform flask;
    public float spawnHeightOffset = -1.0f;
    public float topHeightOffset = -1.0f;
    public float wallOffset = 0.02f;

    public int maxBlobs = 30;
    public float spawnInterval = 0.4f;
    public Vector2 sizeRange = new Vector2(0.06f, 0.18f);
    public float upSpeed = 0.4f;
    public float downSpeed = 0.35f;
    public float lateralDrift = 0.08f;

    public float globalBlendRadius = 0.12f;

    public bool enableScaleChange = true;
    public Vector3 scaleSpeedRange = new Vector3(0.2f, 0.2f, 0.2f);
    public float minScale = 0.6f;
    public float maxScale = 1.5f;

    public Material waxMaterial;

    struct BlobData
    {
        public Vector3 position;
        public Quaternion rotation;
        public Vector3 scale;
        public float blend;
    }

    class Blob
    {
        public Vector3 position;
        public Vector3 velocity;
        public Quaternion rotation;
        public Vector3 angularVelocity;
        public Vector3 scale;
        public Vector3 scaleVelocity;
    }

    List<Blob> blobs = new List<Blob>();
    float bottomY, topY, radius;
    float deleteY;
    float timer;
    GraphicsBuffer blobBuffer;
    BlobData[] blobArray;
    float prevSpawnHeightOffset, prevTopHeightOffset, prevWallOffset;

    void Start()
    {
        if (flask == null || waxMaterial == null) return;
        RecalculateBounds();
        blobArray = new BlobData[maxBlobs];
        blobBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, maxBlobs,
            System.Runtime.InteropServices.Marshal.SizeOf(typeof(BlobData)));
        prevSpawnHeightOffset = spawnHeightOffset;
        prevTopHeightOffset = topHeightOffset;
        prevWallOffset = wallOffset;
        UpdateShaderParams();
    }

    void OnEnable()
    {
        if (flask == null || waxMaterial == null) return;
        if (blobBuffer != null)
            blobBuffer.Release();
        blobArray = new BlobData[maxBlobs];
        blobBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, maxBlobs,
            System.Runtime.InteropServices.Marshal.SizeOf(typeof(BlobData)));
        RecalculateBounds();
        UpdateShaderParams();
        blobs.Clear();
        timer = 0f;
    }

    void Update()
    {
        if (blobBuffer == null) return;

        if (!Mathf.Approximately(spawnHeightOffset, prevSpawnHeightOffset) ||
            !Mathf.Approximately(topHeightOffset, prevTopHeightOffset) ||
            !Mathf.Approximately(wallOffset, prevWallOffset))
        {
            RecalculateBounds();
            AdjustBlobsToNewBounds();
            prevSpawnHeightOffset = spawnHeightOffset;
            prevTopHeightOffset = topHeightOffset;
            prevWallOffset = wallOffset;
        }

        deleteY = bottomY + spawnHeightOffset;

        timer += Time.deltaTime;
        if (timer >= spawnInterval && blobs.Count < maxBlobs)
        {
            timer = 0f;
            SpawnBlob();
        }

        for (int i = blobs.Count - 1; i >= 0; i--)
        {
            Blob b = blobs[i];
            b.position += b.velocity * Time.deltaTime;
            b.rotation = Quaternion.Euler(b.angularVelocity * Time.deltaTime) * b.rotation;

            if (enableScaleChange)
            {
                b.scale += b.scaleVelocity * Time.deltaTime;
                for (int axis = 0; axis < 3; axis++)
                {
                    float val = b.scale[axis];
                    if (val <= minScale)
                    {
                        b.scale[axis] = minScale;
                        b.scaleVelocity[axis] = Mathf.Abs(b.scaleVelocity[axis]);
                    }
                    else if (val >= maxScale)
                    {
                        b.scale[axis] = maxScale;
                        b.scaleVelocity[axis] = -Mathf.Abs(b.scaleVelocity[axis]);
                    }
                }
            }

            if (b.velocity.y > 0 && b.position.y >= topY)
                b.velocity.y = -downSpeed;
            else if (b.velocity.y < 0 && b.position.y <= deleteY)
            {
                blobs.RemoveAt(i);
                continue;
            }

            float dist = new Vector2(b.position.x, b.position.z).magnitude;
            if (dist > radius)
            {
                Vector2 dir = new Vector2(b.position.x, b.position.z).normalized;
                b.velocity.x -= dir.x * lateralDrift;
                b.velocity.z -= dir.y * lateralDrift;
                b.position.x = dir.x * radius;
                b.position.z = dir.y * radius;
            }
            blobs[i] = b;
        }

        int count = blobs.Count;
        if (count > 0)
        {
            for (int i = 0; i < count; i++)
            {
                Blob b = blobs[i];
                blobArray[i] = new BlobData
                {
                    position = b.position,
                    rotation = b.rotation,
                    scale = b.scale,
                    blend = globalBlendRadius
                };
            }
            blobBuffer.SetData(blobArray, 0, 0, count);
        }
        waxMaterial.SetInt("_BlobsCount", count);
    }

    void SpawnBlob()
    {
        float sizeX = Random.Range(sizeRange.x, sizeRange.y);
        float sizeY = sizeX * Random.Range(0.6f, 1.4f);
        float sizeZ = sizeX * Random.Range(0.6f, 1.4f);
        float spawnY = bottomY + spawnHeightOffset + 0.01f;
        Vector3 pos = new Vector3(
            Random.Range(-radius * 0.8f, radius * 0.8f),
            spawnY,
            Random.Range(-radius * 0.8f, radius * 0.8f)
        );
        Vector3 scaleVel = new Vector3(
            Random.Range(-scaleSpeedRange.x, scaleSpeedRange.x),
            Random.Range(-scaleSpeedRange.y, scaleSpeedRange.y),
            Random.Range(-scaleSpeedRange.z, scaleSpeedRange.z)
        );
        blobs.Add(new Blob
        {
            position = pos,
            velocity = new Vector3(
                Random.Range(-lateralDrift, lateralDrift),
                upSpeed,
                Random.Range(-lateralDrift, lateralDrift)
            ),
            rotation = Random.rotationUniform,
            angularVelocity = new Vector3(
                Random.Range(-15f, 15f),
                Random.Range(-15f, 15f),
                Random.Range(-15f, 15f)
            ),
            scale = new Vector3(sizeX, sizeY, sizeZ),
            scaleVelocity = scaleVel
        });
    }

    void RecalculateBounds()
    {
        if (flask == null) return;
        MeshFilter mf = flask.GetComponent<MeshFilter>();
        Bounds bounds = (mf != null && mf.sharedMesh != null) ? mf.sharedMesh.bounds : new Bounds(Vector3.zero, Vector3.one);
        Vector3 scl = flask.lossyScale;
        float h = bounds.size.y * scl.y;
        float r = bounds.size.x * scl.x * 0.5f;
        Vector3 flaskPos = flask.position;
        bottomY = flaskPos.y - h * 0.5f + wallOffset;
        topY = flaskPos.y + h * 0.5f - wallOffset + topHeightOffset;
        radius = r - wallOffset;
        deleteY = bottomY + spawnHeightOffset;
    }

    void UpdateShaderParams()
    {
        if (waxMaterial == null || flask == null) return;
        MeshFilter mf = flask.GetComponent<MeshFilter>();
        Bounds bounds = (mf != null && mf.sharedMesh != null) ? mf.sharedMesh.bounds : new Bounds(Vector3.zero, Vector3.one);
        float h = bounds.size.y * flask.lossyScale.y;
        float fullHeight = h - wallOffset * 2f;
        waxMaterial.SetVector("_CylinderCenter", flask.position);
        waxMaterial.SetVector("_CylinderUp", flask.up);
        waxMaterial.SetFloat("_CylinderRadius", radius);
        waxMaterial.SetFloat("_CylinderHeight", fullHeight);
        waxMaterial.SetBuffer("_BlobBuffer", blobBuffer);
    }

    void AdjustBlobsToNewBounds()
    {
        float newDeleteY = bottomY + spawnHeightOffset;
        for (int i = 0; i < blobs.Count; i++)
        {
            Blob b = blobs[i];
            if (b.position.y > topY)
            {
                b.position.y = topY;
                if (b.velocity.y > 0) b.velocity.y = -downSpeed;
            }
            else if (b.position.y < newDeleteY)
            {
                blobs.RemoveAt(i);
                i--;
                continue;
            }
            float dist = new Vector2(b.position.x, b.position.z).magnitude;
            if (dist > radius)
            {
                Vector2 dir = new Vector2(b.position.x, b.position.z).normalized;
                b.position.x = dir.x * radius;
                b.position.z = dir.y * radius;
                if (Vector2.Dot(new Vector2(b.velocity.x, b.velocity.z), dir) > 0)
                {
                    b.velocity.x -= dir.x * lateralDrift;
                    b.velocity.z -= dir.y * lateralDrift;
                }
            }
            if (enableScaleChange)
            {
                for (int axis = 0; axis < 3; axis++)
                {
                    float val = b.scale[axis];
                    if (val < minScale)
                    {
                        b.scale[axis] = minScale;
                        b.scaleVelocity[axis] = Mathf.Abs(b.scaleVelocity[axis]);
                    }
                    else if (val > maxScale)
                    {
                        b.scale[axis] = maxScale;
                        b.scaleVelocity[axis] = -Mathf.Abs(b.scaleVelocity[axis]);
                    }
                }
            }
            blobs[i] = b;
        }
    }

    public void ClearAllBlobs()
    {
        blobs.Clear();
        if (blobBuffer != null)
        {
            blobBuffer.Release();
            blobBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, maxBlobs,
                System.Runtime.InteropServices.Marshal.SizeOf(typeof(BlobData)));
            UpdateShaderParams();
        }
        waxMaterial.SetInt("_BlobsCount", 0);
        timer = 0f;
    }

    void OnDestroy()
    {
        blobBuffer?.Release();
    }
    
    void OnDrawGizmosSelected()
    {
        if (flask == null) return;
        MeshFilter mf = flask.GetComponent<MeshFilter>();
        if (mf == null || mf.sharedMesh == null) return;

        Bounds bounds = mf.sharedMesh.bounds;
        Vector3 scl = flask.lossyScale;
        float h = bounds.size.y * scl.y;
        float r = bounds.size.x * scl.x * 0.5f;
        Vector3 flaskPos = flask.position;

        float drawBottomY = flaskPos.y - h * 0.5f + wallOffset;
        float drawTopY = flaskPos.y + h * 0.5f - wallOffset + topHeightOffset;
        float drawRadius = r - wallOffset;
        float deleteY = drawBottomY + spawnHeightOffset;

        Gizmos.color = Color.green;
        DrawCircle(flaskPos, deleteY, drawRadius);
        DrawCircle(flaskPos, drawTopY, drawRadius);
    }

    void DrawCircle(Vector3 center, float y, float radius)
    {
        const int segments = 32;
        Vector3 prevPoint = Vector3.zero;
        for (int i = 0; i <= segments; i++)
        {
            float angle = i * 2f * Mathf.PI / segments;
            Vector3 point = new Vector3(Mathf.Cos(angle) * radius, y, Mathf.Sin(angle) * radius);
            point += center;
            if (i > 0) Gizmos.DrawLine(prevPoint, point);
            prevPoint = point;
        }
    }
}