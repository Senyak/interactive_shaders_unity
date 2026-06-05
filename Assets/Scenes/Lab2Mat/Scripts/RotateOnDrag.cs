using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Collider))]
public class RotateOnDrag : MonoBehaviour
{
    [Header("Input Action")]
    [SerializeField] private InputActionReference dragAction;

    [Header("Settings")]
    [SerializeField] private float sensitivity = 0.5f;
    [SerializeField] private bool invertX = false;
    [SerializeField] private bool invertY = false;
    [SerializeField] private Camera targetCamera;

    private bool isDragging = false;
    private Camera currentCamera;

    private void Awake()
    {
        if (dragAction == null)
            Debug.LogError("Drag Action не назначен!", this);

        currentCamera = targetCamera != null ? targetCamera : Camera.main;
        if (currentCamera == null)
            Debug.LogError("Камера не найдена!", this);
    }

    private void OnEnable()
    {
        if (dragAction != null)
        {
            dragAction.action.performed += OnDragStarted;
            dragAction.action.canceled += OnDragEnded;
            dragAction.action.Enable();
        }
    }

    private void OnDisable()
    {
        if (dragAction != null)
        {
            dragAction.action.performed -= OnDragStarted;
            dragAction.action.canceled -= OnDragEnded;
            dragAction.action.Disable();
        }
        isDragging = false;
    }

    private void OnDragStarted(InputAction.CallbackContext context)
    {
        if (currentCamera == null) return;

        if (Mouse.current == null || !Mouse.current.leftButton.isPressed)
            return;

        Vector2 mousePosition = Mouse.current.position.ReadValue();
        Ray ray = currentCamera.ScreenPointToRay(mousePosition);

        if (Physics.Raycast(ray, out RaycastHit hit) && hit.transform == transform)
        {
            isDragging = true;
        }
    }

    private void OnDragEnded(InputAction.CallbackContext context)
    {
        isDragging = false;
    }

    private void Update()
    {
        if (isDragging && (Mouse.current == null || !Mouse.current.leftButton.isPressed))
        {
            isDragging = false;
            return;
        }

        if (!isDragging) return;

        Vector2 delta = Mouse.current.delta.ReadValue();

        if (delta == Vector2.zero) return;

        float deltaX = delta.x * sensitivity * (invertX ? -1f : 1f);
        float deltaY = delta.y * sensitivity * (invertY ? -1f : 1f);

        transform.Rotate(Vector3.up, deltaX, Space.World);
        transform.Rotate(Vector3.right, deltaY, Space.Self);
    }
}