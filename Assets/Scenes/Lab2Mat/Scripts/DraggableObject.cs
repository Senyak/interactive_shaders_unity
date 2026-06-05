using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Collider))]
public class DraggableObject : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private Camera targetCamera;
    [SerializeField] private Collider boundaryCollider; 
    [SerializeField] private float sensitivity = 1f; 

    private PlayerControls playerControls;
    private InputAction dragAction;
    private InputAction deltaAction;

    private bool isDragging = false;
    private Vector3 dragOffset; 
    private Plane dragPlane;  

    private Vector2 currentDelta = Vector2.zero;
    private Camera currentCamera;

    private void Awake()
    {
        if (boundaryCollider == null)
            Debug.LogError("Boundary Collider не назначен!", this);

        currentCamera = targetCamera != null ? targetCamera : Camera.main;
        if (currentCamera == null)
            Debug.LogError("Камера не найдена!", this);

        playerControls = new PlayerControls();

        dragAction = playerControls.Player.DragAction;   
        deltaAction = playerControls.Player.DeltaAction;  

        if (dragAction == null)
            Debug.LogError("DragAction не найдено в PlayerControls! Проверьте названия.", this);
        if (deltaAction == null)
            Debug.LogError("DeltaAction не найдено в PlayerControls! Проверьте названия.", this);
    }

    private void OnEnable()
    {
        if (dragAction != null)
        {
            dragAction.performed += OnDragStarted;
            dragAction.canceled += OnDragEnded;
            dragAction.Enable();
        }

        if (deltaAction != null)
        {
            deltaAction.performed += OnDeltaPerformed;
            deltaAction.Enable();
        }
    }

    private void OnDisable()
    {
        if (dragAction != null)
        {
            dragAction.performed -= OnDragStarted;
            dragAction.canceled -= OnDragEnded;
            dragAction.Disable();
        }

        if (deltaAction != null)
        {
            deltaAction.performed -= OnDeltaPerformed;
            deltaAction.Disable();
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
            dragOffset = transform.position - hit.point;

            dragPlane = new Plane(Vector3.up, transform.position);

            isDragging = true;
        }
    }

    private void OnDragEnded(InputAction.CallbackContext context)
    {
        isDragging = false;
        currentDelta = Vector2.zero;
    }

    private void OnDeltaPerformed(InputAction.CallbackContext context)
    {
        currentDelta = context.ReadValue<Vector2>();
    }

    private void Update()
    {
        if (isDragging && (Mouse.current == null || !Mouse.current.leftButton.isPressed))
        {
            isDragging = false;
            return;
        }

        if (!isDragging) return;

        if (currentDelta == Vector2.zero) return;

        Vector2 currentMousePosition = Mouse.current.position.ReadValue();
        Ray ray = currentCamera.ScreenPointToRay(currentMousePosition);

        if (dragPlane.Raycast(ray, out float enter))
        {
            Vector3 hitPoint = ray.GetPoint(enter);
            
            Vector3 desiredPosition = hitPoint + dragOffset;

            Vector3 clampedPosition = ClampPositionByCollider(desiredPosition);

            clampedPosition.y = transform.position.y;
            transform.position = clampedPosition;

            dragPlane.SetNormalAndPosition(Vector3.up, transform.position);
        }

        currentDelta = Vector2.zero;
    }

    private Vector3 ClampPositionByCollider(Vector3 position)
    {
        if (boundaryCollider == null)
            return position;

        return boundaryCollider.ClosestPoint(position);
    }

    private void OnDrawGizmosSelected()
    {
        if (boundaryCollider != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireCube(boundaryCollider.bounds.center, boundaryCollider.bounds.size);
        }
    }
}