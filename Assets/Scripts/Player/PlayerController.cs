using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;

using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;
using TouchPhase = UnityEngine.InputSystem.TouchPhase;

public class PlayerController : MonoBehaviour
{
    private enum MovementMode
    {
        Joystick,
        Drag
    }

    [Header("Movement")]
    [SerializeField] private MovementMode movementMode = MovementMode.Joystick;
    [SerializeField] private Joystick joystick;
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float screenPadding = 0.15f;

    [Header("Drag Debug Movement")]
    [SerializeField] private bool onlyDragWhenTouchingPlayer = false;
    [SerializeField] private bool instantDrag = true;
    [SerializeField] private float dragFollowSpeed = 25f;

    private Camera mainCamera;
    private SpriteRenderer spriteRenderer;
    private Collider2D playerCollider;

    private bool isDragging;
    private Vector3 dragTargetPosition;

    private void Awake()
    {
        mainCamera = Camera.main;
        spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        playerCollider = GetComponentInChildren<Collider2D>();

        dragTargetPosition = transform.position;

        if (mainCamera == null)
            Debug.LogError("PlayerController: Main Camera not found. Camera needs MainCamera tag.");

        if (spriteRenderer == null)
            Debug.LogError("PlayerController: SpriteRenderer not found.");

        if (playerCollider == null)
            Debug.LogWarning("PlayerController: Collider2D not found. Drag when touching player will not work.");
    }

    private void OnEnable()
    {
        EnhancedTouchSupport.Enable();
    }

    private void OnDisable()
    {
        EnhancedTouchSupport.Disable();
    }

    private void Update()
    {
        if (GameManager.Instance != null && GameManager.Instance.IsGameOver)
            return;

        if (movementMode == MovementMode.Joystick)
        {
            MoveWithJoystick();
        }
        else if (movementMode == MovementMode.Drag)
        {
            MoveWithDrag();
        }

        ClampToCameraBounds();
    }

    private void MoveWithJoystick()
    {
        if (joystick == null)
            return;

        Vector2 input = joystick.Direction;

        Vector3 movement = new Vector3(input.x, input.y, 0f) * moveSpeed * Time.deltaTime;
        transform.position += movement;
    }

    private void MoveWithDrag()
    {
        HandleTouchOrMouseDrag();

        if (!isDragging)
            return;

        if (instantDrag)
        {
            transform.position = dragTargetPosition;
        }
        else
        {
            transform.position = Vector3.Lerp(
                transform.position,
                dragTargetPosition,
                dragFollowSpeed * Time.deltaTime
            );
        }
    }

    private void HandleTouchOrMouseDrag()
    {
        if (Touch.activeTouches.Count > 0)
        {
            HandleTouchDrag();
            return;
        }

        HandleMouseDrag();
    }

    private void HandleTouchDrag()
    {
        Touch touch = Touch.activeTouches[0];

        Vector2 screenPosition = touch.screenPosition;
        Vector3 worldPosition = GetWorldPositionFromScreen(screenPosition);

        if (touch.phase == TouchPhase.Began)
        {
            TryStartDrag(worldPosition);
        }
        else if (touch.phase == TouchPhase.Moved ||
                 touch.phase == TouchPhase.Stationary)
        {
            if (isDragging)
            {
                dragTargetPosition = worldPosition;
            }
        }
        else if (touch.phase == TouchPhase.Ended ||
                 touch.phase == TouchPhase.Canceled)
        {
            isDragging = false;
        }
    }

    private void HandleMouseDrag()
    {
        if (Mouse.current == null)
            return;

        Vector2 screenPosition = Mouse.current.position.ReadValue();
        Vector3 worldPosition = GetWorldPositionFromScreen(screenPosition);

        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            TryStartDrag(worldPosition);
        }

        if (Mouse.current.leftButton.isPressed && isDragging)
        {
            dragTargetPosition = worldPosition;
        }

        if (Mouse.current.leftButton.wasReleasedThisFrame)
        {
            isDragging = false;
        }
    }

    private void TryStartDrag(Vector3 worldPosition)
    {
        if (!onlyDragWhenTouchingPlayer || IsPointOnPlayer(worldPosition))
        {
            isDragging = true;
            dragTargetPosition = worldPosition;

            Debug.Log("Started dragging player");
        }
        else
        {
            Debug.Log("Pressed, but not on player");
        }
    }

    private Vector3 GetWorldPositionFromScreen(Vector2 screenPosition)
    {
        if (mainCamera == null)
            return transform.position;

        float distanceFromCamera = Mathf.Abs(mainCamera.transform.position.z - transform.position.z);

        Vector3 worldPosition = mainCamera.ScreenToWorldPoint(
            new Vector3(screenPosition.x, screenPosition.y, distanceFromCamera)
        );

        worldPosition.z = transform.position.z;

        return worldPosition;
    }

    private bool IsPointOnPlayer(Vector3 worldPosition)
    {
        if (playerCollider == null)
            return false;

        return playerCollider.OverlapPoint(worldPosition);
    }

    private void ClampToCameraBounds()
    {
        if (mainCamera == null || spriteRenderer == null)
            return;

        float distanceFromCamera = Mathf.Abs(mainCamera.transform.position.z - transform.position.z);

        Vector3 bottomLeft = mainCamera.ViewportToWorldPoint(
            new Vector3(0f, 0f, distanceFromCamera)
        );

        Vector3 topRight = mainCamera.ViewportToWorldPoint(
            new Vector3(1f, 1f, distanceFromCamera)
        );

        Vector3 pos = transform.position;

        float halfWidth = spriteRenderer.bounds.extents.x;
        float halfHeight = spriteRenderer.bounds.extents.y;

        pos.x = Mathf.Clamp(
            pos.x,
            bottomLeft.x + halfWidth + screenPadding,
            topRight.x - halfWidth - screenPadding
        );

        pos.y = Mathf.Clamp(
            pos.y,
            bottomLeft.y + halfHeight + screenPadding,
            topRight.y - halfHeight - screenPadding
        );

        transform.position = pos;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        Debug.Log("Trigger detected with: " + other.gameObject.name);

        if (other.CompareTag("Traffic"))
        {
            Debug.Log("Hit traffic");

            if (GameManager.Instance != null)
                GameManager.Instance.GameOver();
        }
    }
}