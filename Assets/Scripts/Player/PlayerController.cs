using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [SerializeField] private Joystick joystick;
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float screenPadding = 0.15f;

    private Camera mainCamera;
    private SpriteRenderer spriteRenderer;

    private void Awake()
    {
        mainCamera = Camera.main;
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    private void Update()
    {
        if (GameManager.Instance != null && GameManager.Instance.IsGameOver) return;

        Vector2 input = joystick.Direction;

        Vector3 movement = new Vector3(input.x, input.y, 0f) * moveSpeed * Time.deltaTime;
        transform.position += movement;

        ClampToCameraBounds();
    }

    private void ClampToCameraBounds()
    {
        if (mainCamera == null || spriteRenderer == null)
            return;

        float distanceFromCamera = Mathf.Abs(mainCamera.transform.position.z - transform.position.z);

        Vector3 bottomLeft = mainCamera.ViewportToWorldPoint(new Vector3(0f, 0f, distanceFromCamera));
        Vector3 topRight = mainCamera.ViewportToWorldPoint(new Vector3(1f, 1f, distanceFromCamera));

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

        if (other.CompareTag("Enemy"))
        {
            Debug.Log("Hit enemy");
            GameManager.Instance.GameOver();
        }
    }
}