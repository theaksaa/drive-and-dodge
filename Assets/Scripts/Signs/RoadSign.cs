using UnityEngine;

public class RoadSign : MonoBehaviour
{
    [SerializeField] private float fallbackMoveSpeed = 5f;

    private float despawnY;

    public void Setup(float despawnPositionY)
    {
        despawnY = despawnPositionY;
    }

    private void Update()
    {
        if (GameManager.Instance != null && GameManager.Instance.IsGameplayStopped)
            return;

        float speed = GameManager.Instance != null
            ? GameManager.Instance.CurrentGameSpeed
            : fallbackMoveSpeed;

        transform.position += Vector3.down * speed * Time.deltaTime;

        if (transform.position.y <= despawnY)
            Destroy(gameObject);
    }
}
