using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class RoadTireMarks : MonoBehaviour
{
    [Header("Trail Reveal")]
    [SerializeField, Min(0.01f)] private float revealDuration = 0.3f;
    [SerializeField, Range(0f, 0.5f)] private float initialRevealAmount = 0.12f;

    [Header("Cleanup")]
    [SerializeField] private float destroyPadding = 2f;

    private static Sprite revealMaskSprite;

    private Camera mainCamera;
    private SpriteRenderer marksRenderer;
    private Transform revealMaskTransform;
    private float spriteLocalWidth;
    private float spriteLocalHeight;
    private float spriteTopLocalY;
    private float revealElapsed;
    private bool revealComplete;

    private void Awake()
    {
        mainCamera = Camera.main;
        marksRenderer = GetComponent<SpriteRenderer>();
        CreateRevealMask();
    }

    private void Update()
    {
        if (GameManager.Instance == null || GameManager.Instance.IsGameplayStopped)
            return;

        if (!revealComplete)
            UpdateReveal();

        transform.position += Vector3.down * GameManager.Instance.CurrentGameSpeed * Time.deltaTime;

        if (mainCamera == null)
            mainCamera = Camera.main;

        if (mainCamera == null)
            return;

        float distanceFromCamera = Mathf.Abs(mainCamera.transform.position.z - transform.position.z);
        float bottomY = mainCamera.ViewportToWorldPoint(new Vector3(0f, 0f, distanceFromCamera)).y;

        if (transform.position.y < bottomY - destroyPadding)
            Destroy(gameObject);
    }

    private void CreateRevealMask()
    {
        if (marksRenderer.sprite == null)
        {
            revealComplete = true;
            return;
        }

        spriteLocalWidth = marksRenderer.sprite.bounds.size.x;
        spriteLocalHeight = marksRenderer.sprite.bounds.size.y;
        spriteTopLocalY = marksRenderer.sprite.bounds.max.y;

        GameObject maskObject = new GameObject("Tire Marks Reveal Mask");
        revealMaskTransform = maskObject.transform;
        revealMaskTransform.SetParent(transform, false);

        SpriteMask spriteMask = maskObject.AddComponent<SpriteMask>();
        spriteMask.sprite = GetRevealMaskSprite();
        spriteMask.alphaCutoff = 0.01f;
        spriteMask.isCustomRangeActive = true;
        spriteMask.frontSortingLayerID = marksRenderer.sortingLayerID;
        spriteMask.backSortingLayerID = marksRenderer.sortingLayerID;
        spriteMask.frontSortingOrder = marksRenderer.sortingOrder + 1;
        spriteMask.backSortingOrder = marksRenderer.sortingOrder - 1;

        marksRenderer.maskInteraction = SpriteMaskInteraction.VisibleInsideMask;
        SetRevealAmount(initialRevealAmount);
    }

    private void UpdateReveal()
    {
        revealElapsed += Time.deltaTime;
        float normalizedTime = Mathf.Clamp01(revealElapsed / revealDuration);
        float fastStartReveal = 1f - (1f - normalizedTime) * (1f - normalizedTime);
        SetRevealAmount(Mathf.Lerp(initialRevealAmount, 1f, fastStartReveal));

        if (normalizedTime < 1f)
            return;

        revealComplete = true;
        marksRenderer.maskInteraction = SpriteMaskInteraction.None;

        if (revealMaskTransform != null)
            Destroy(revealMaskTransform.gameObject);
    }

    private void SetRevealAmount(float amount)
    {
        if (revealMaskTransform == null)
            return;

        float visibleHeight = spriteLocalHeight * Mathf.Clamp01(amount);
        float safeHeight = Mathf.Max(visibleHeight, 0.0001f);

        revealMaskTransform.localScale = new Vector3(spriteLocalWidth, safeHeight, 1f);
        revealMaskTransform.localPosition = new Vector3(
            0f,
            spriteTopLocalY - safeHeight * 0.5f,
            0f);
    }

    private static Sprite GetRevealMaskSprite()
    {
        if (revealMaskSprite != null)
            return revealMaskSprite;

        Texture2D texture = new Texture2D(1, 1, TextureFormat.RGBA32, false);
        texture.name = "Runtime Tire Marks Reveal Mask";
        texture.hideFlags = HideFlags.HideAndDontSave;
        texture.filterMode = FilterMode.Point;
        texture.SetPixel(0, 0, Color.white);
        texture.Apply();

        revealMaskSprite = Sprite.Create(
            texture,
            new Rect(0f, 0f, 1f, 1f),
            new Vector2(0.5f, 0.5f),
            1f);
        revealMaskSprite.name = "Runtime Tire Marks Reveal Mask";
        revealMaskSprite.hideFlags = HideFlags.HideAndDontSave;
        return revealMaskSprite;
    }
}
