using UnityEngine;

[RequireComponent(typeof(CapsuleCollider))]
public class SnowActor : MonoBehaviour
{
    public Vector3 groundOffset = Vector3.down * 0.5f;

    private CapsuleCollider capsule;
    private Vector3 lastPos;
    private bool isMoving;

    void Start()
    {
        capsule = GetComponent<CapsuleCollider>();
        lastPos = transform.position;
    }

    void Update()
    {
        isMoving = Vector3.Distance(transform.position, lastPos) > 0.001f;
        lastPos = transform.position;
    }

    public Vector3 GetGroundPosition()
    {
        return transform.position + groundOffset;
    }

    public float GetRadius()
    {
        Vector3 scale = transform.lossyScale;
        float maxScale = Mathf.Max(scale.x, scale.z);
        return capsule.radius * maxScale;
    }

    public bool IsMoving() => isMoving;
}