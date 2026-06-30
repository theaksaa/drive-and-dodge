using UnityEngine;

public class EnemyCar : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 4f;

    private Camera mainCamera;

    private void Awake()
    {
        mainCamera = Camera.main;
    }

    private void Update()
    {
        transform.position += Vector3.down * moveSpeed * Time.deltaTime;

        DestroyIfOutsideScreen();
    }

    private void DestroyIfOutsideScreen()
    {
        float distanceFromCamera = Mathf.Abs(mainCamera.transform.position.z - transform.position.z);

        Vector3 bottomLeft = mainCamera.ViewportToWorldPoint(
            new Vector3(0f, 0f, distanceFromCamera)
        );

        float destroyPadding = 2f;

        if (transform.position.y < bottomLeft.y - destroyPadding)
        {
            Destroy(gameObject);
        }
    }
}