using UnityEngine;

public class PlayerLaneTracker : MonoBehaviour
{
    [SerializeField] private LaneSystem laneSystem;

    public int CurrentLaneIndex { get; private set; }

    private void Update()
    {
        if (laneSystem == null)
            return;

        CurrentLaneIndex = laneSystem.GetClosestLaneIndex(transform.position.x);
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
}