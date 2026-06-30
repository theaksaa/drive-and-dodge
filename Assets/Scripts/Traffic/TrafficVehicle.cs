using UnityEngine;

public class TrafficVehicle : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 4f;

    public float MoveSpeed => moveSpeed;
    public int LaneIndex { get; private set; } = -1;

    private Camera mainCamera;
    private LaneSystem laneSystem;

    private void Awake()
    {
        mainCamera = Camera.main;
    }

    public void Initialize(int laneIndex, LaneSystem laneSystem)
    {
        LaneIndex = laneIndex;
        this.laneSystem = laneSystem;
    }

    private void Update()
    {
        if (GameManager.Instance != null && GameManager.Instance.IsGameOver)
            return;

        transform.position += Vector3.down * moveSpeed * Time.deltaTime;

        DestroyIfOutsideScreen();
    }

    public float GetHalfLength()
    {
        BoxCollider2D boxCollider = GetComponent<BoxCollider2D>();

        if (boxCollider != null)
        {
            return boxCollider.size.y * Mathf.Abs(transform.localScale.y) / 2f;
        }

        SpriteRenderer spriteRenderer = GetComponent<SpriteRenderer>();

        if (spriteRenderer != null && spriteRenderer.sprite != null)
        {
            return spriteRenderer.sprite.bounds.size.y * Mathf.Abs(transform.localScale.y) / 2f;
        }

        return 0.5f;
    }

    private void DestroyIfOutsideScreen()
    {
        float bottomY;

        if (laneSystem != null)
        {
            bottomY = laneSystem.GetBottomY();
        }
        else
        {
            float distanceFromCamera = Mathf.Abs(mainCamera.transform.position.z - transform.position.z);

            Vector3 bottomLeft = mainCamera.ViewportToWorldPoint(
                new Vector3(0f, 0f, distanceFromCamera)
            );

            bottomY = bottomLeft.y;
        }

        if (transform.position.y < bottomY - GetHalfLength() - 1f)
        {
            Destroy(gameObject);
        }
    }
}