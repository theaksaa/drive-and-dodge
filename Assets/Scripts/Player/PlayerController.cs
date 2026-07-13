using UnityEngine;
using UnityEngine.EventSystems;
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

    [Header("References")]
    [SerializeField] private LaneSystem laneSystem;

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
    private PlayerCollisionPush collisionPush;

    private bool isDragging;
    private Vector3 dragTargetPosition;

    private Vector3 initialPosition;
    private float lockedY;
    private float environmentSpeedMultiplier = 1f;

    private void Awake()
    {
        mainCamera = Camera.main;
        laneSystem ??= FindAnyObjectByType<LaneSystem>();
        spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        playerCollider = GetComponentInChildren<Collider2D>();
        collisionPush = GetComponent<PlayerCollisionPush>();

        initialPosition = transform.position;
        lockedY = transform.position.y;
        dragTargetPosition = transform.position;

        if (mainCamera == null)
            Debug.LogError("PlayerController: Main Camera not found. Camera needs MainCamera tag.");

        if (spriteRenderer == null)
            Debug.LogError("PlayerController: SpriteRenderer not found.");

        if (playerCollider == null)
            Debug.LogWarning("PlayerController: Collider2D not found. Drag when touching player will not work.");

        if (laneSystem == null)
            Debug.LogWarning("PlayerController: LaneSystem is not assigned. Player will fallback to camera bounds.");
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
        if (GameManager.Instance != null && GameManager.Instance.IsGameplayStopped)
            return;

        if (collisionPush != null && collisionPush.IsBounceActive)
        {
            collisionPush.ApplyBounceStep(transform);
        }
        else if (movementMode == MovementMode.Joystick)
        {
            MoveWithJoystick();
        }
        else if (movementMode == MovementMode.Drag)
        {
            MoveWithDrag();
        }
        LockVerticalPosition();
        ClampToMovementBounds();
    }

    private void MoveWithJoystick()
    {
        if (joystick == null)
            return;

        Vector2 input = joystick.Direction;

        Vector3 movement = new Vector3(input.x, 0f, 0f) * moveSpeed * environmentSpeedMultiplier * Time.deltaTime;
        transform.position += movement;
    }

    private void MoveWithDrag()
    {
        HandleTouchOrMouseDrag();

        if (!isDragging)
            return;

        Vector3 targetPosition = new Vector3(
            dragTargetPosition.x,
            lockedY,
            transform.position.z
        );

        if (instantDrag)
        {
            transform.position = targetPosition;
        }
        else
        {
            transform.position = Vector3.Lerp(
                transform.position,
                targetPosition,
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

        if (IsPointerOverUi(touch))
        {
            if (touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled)
                isDragging = false;

            return;
        }

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
                SetDragTargetXOnly(worldPosition);
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

        if (IsPointerOverUi())
        {
            if (Mouse.current.leftButton.wasReleasedThisFrame)
                isDragging = false;

            return;
        }

        Vector2 screenPosition = Mouse.current.position.ReadValue();
        Vector3 worldPosition = GetWorldPositionFromScreen(screenPosition);

        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            TryStartDrag(worldPosition);
        }

        if (Mouse.current.leftButton.isPressed && isDragging)
        {
            SetDragTargetXOnly(worldPosition);
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
            SetDragTargetXOnly(worldPosition);

            Debug.Log("Started dragging player horizontally");
        }
        else
        {
            Debug.Log("Pressed, but not on player");
        }
    }

    private void SetDragTargetXOnly(Vector3 worldPosition)
    {
        dragTargetPosition = new Vector3(
            worldPosition.x,
            lockedY,
            transform.position.z
        );
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

    private static bool IsPointerOverUi()
    {
        return EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
    }

    private static bool IsPointerOverUi(Touch touch)
    {
        return EventSystem.current != null && EventSystem.current.IsPointerOverGameObject(touch.touchId);
    }

    private bool IsPointOnPlayer(Vector3 worldPosition)
    {
        if (playerCollider == null)
            return false;

        return playerCollider.OverlapPoint(worldPosition);
    }

    private void LockVerticalPosition()
    {
        Vector3 pos = transform.position;
        pos.y = lockedY;
        transform.position = pos;
    }

    private void ClampToMovementBounds()
    {
        Vector3 pos = transform.position;
        float halfWidth = GetPlayerHalfWidth();

        if (laneSystem != null)
        {
            pos.x = laneSystem.GetClampedXInsideAllowedArea(pos.x, halfWidth);
        }
        else
        {
            ClampToCameraBoundsFallback(ref pos, halfWidth);
        }

        pos.y = lockedY;

        transform.position = pos;
    }

    private float GetPlayerHalfWidth()
    {
        if (playerCollider != null)
            return playerCollider.bounds.extents.x;

        if (spriteRenderer != null)
            return spriteRenderer.bounds.extents.x;

        return 0f;
    }

    private void ClampToCameraBoundsFallback(ref Vector3 pos, float halfWidth)
    {
        if (mainCamera == null)
            return;

        float distanceFromCamera = Mathf.Abs(mainCamera.transform.position.z - transform.position.z);

        Vector3 bottomLeft = mainCamera.ViewportToWorldPoint(
            new Vector3(0f, 0f, distanceFromCamera)
        );

        Vector3 topRight = mainCamera.ViewportToWorldPoint(
            new Vector3(1f, 1f, distanceFromCamera)
        );

        pos.x = Mathf.Clamp(
            pos.x,
            bottomLeft.x + halfWidth + screenPadding,
            topRight.x - halfWidth - screenPadding
        );
    }

    public void SetEnvironmentSpeedMultiplier(float multiplier)
    {
        environmentSpeedMultiplier = Mathf.Max(0f, multiplier);
    }

    public void ResetToEnvironmentStart()
    {
        float targetX = initialPosition.x;

        if (laneSystem != null)
            targetX = (laneSystem.RoadLeftX + laneSystem.RoadRightX) * 0.5f;

        lockedY = initialPosition.y;
        transform.position = new Vector3(targetX, lockedY, initialPosition.z);
        dragTargetPosition = transform.position;
        isDragging = false;

        ClampToMovementBounds();
    }

    public void CancelDragForExternalMovement()
    {
        if (movementMode == MovementMode.Drag)
        {
            dragTargetPosition = transform.position;
            isDragging = false;
        }
    }

    public void SyncDragTargetToCurrentPosition()
    {
        if (movementMode == MovementMode.Drag)
            dragTargetPosition = transform.position;
    }
}
