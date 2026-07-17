using System.Collections;
using UnityEngine;

public class PlayerStartupEffects : MonoBehaviour
{
    [Header("Driving Smoke")]
    [SerializeField] private GameObject[] drivingSmokeObjects;

    [Header("Startup Smoke")]
    [SerializeField] private SpriteRenderer[] smokeRenderers;
    [SerializeField] private Sprite[] smokeFrames;
    [SerializeField, Min(1f)] private float smokeFramesPerSecond = 12f;

    [Header("Tire Marks")]
    [SerializeField] private GameObject tireMarksTemplate;

    private GameManager gameManager;
    private bool hasPlayed;

    private IEnumerator Start()
    {
        SetDrivingSmokeVisible(false);
        SetSmokeVisible(false);

        while (GameManager.Instance == null)
            yield return null;

        gameManager = GameManager.Instance;
        gameManager.GameplayStarted += PlayOnce;

        if (!gameManager.IsStarting)
            PlayOnce();
    }

    private void OnDestroy()
    {
        if (gameManager != null)
            gameManager.GameplayStarted -= PlayOnce;
    }

    private void PlayOnce()
    {
        if (hasPlayed)
            return;

        hasPlayed = true;
        SetDrivingSmokeVisible(true);
        SpawnTireMarks();
        StartCoroutine(PlaySmokeAnimation());
    }

    private IEnumerator PlaySmokeAnimation()
    {
        if (smokeRenderers == null || smokeFrames == null || smokeFrames.Length == 0)
            yield break;

        SetSmokeVisible(true);
        float frameDuration = 1f / smokeFramesPerSecond;

        foreach (Sprite frame in smokeFrames)
        {
            foreach (SpriteRenderer smokeRenderer in smokeRenderers)
            {
                if (smokeRenderer != null)
                    smokeRenderer.sprite = frame;
            }

            yield return new WaitForSeconds(frameDuration);
        }

        SetSmokeVisible(false);
    }

    private void SpawnTireMarks()
    {
        if (tireMarksTemplate == null)
            return;

        GameObject tireMarks = Instantiate(
            tireMarksTemplate,
            tireMarksTemplate.transform.position,
            tireMarksTemplate.transform.rotation);

        tireMarks.name = "Startup Tire Marks";
        tireMarks.transform.localScale = tireMarksTemplate.transform.lossyScale;
        tireMarks.SetActive(true);
    }

    private void SetSmokeVisible(bool visible)
    {
        if (smokeRenderers == null)
            return;

        foreach (SpriteRenderer smokeRenderer in smokeRenderers)
        {
            if (smokeRenderer != null)
                smokeRenderer.enabled = visible;
        }
    }

    private void SetDrivingSmokeVisible(bool visible)
    {
        if (drivingSmokeObjects == null)
            return;

        foreach (GameObject smokeObject in drivingSmokeObjects)
        {
            if (smokeObject != null)
                smokeObject.SetActive(visible);
        }
    }
}
