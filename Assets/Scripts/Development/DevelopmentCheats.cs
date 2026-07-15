using UnityEngine;
#if UNITY_EDITOR
using UnityEngine.InputSystem;
#endif

public sealed class DevelopmentCheats : MonoBehaviour
{
#if UNITY_EDITOR
    private PlayerHealth playerHealth;
    private FuelSystem fuelSystem;
    private SideRoadSpawner sideRoadSpawner;

    private void Update()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null)
            return;

        if (keyboard.digit1Key.wasPressedThisFrame)
        {
            playerHealth ??= FindAnyObjectByType<PlayerHealth>();
            playerHealth?.RepairFull();
        }

        if (keyboard.digit2Key.wasPressedThisFrame)
        {
            fuelSystem ??= FindAnyObjectByType<FuelSystem>();
            fuelSystem?.FillFull();
        }

        if (keyboard.digit3Key.wasPressedThisFrame)
            SpawnRandomSideRoad(SideRoadDirection.Left);

        if (keyboard.digit4Key.wasPressedThisFrame)
            SpawnRandomSideRoad(SideRoadDirection.Right);
    }

    private void SpawnRandomSideRoad(SideRoadDirection direction)
    {
        sideRoadSpawner ??= FindAnyObjectByType<SideRoadSpawner>();
        if (sideRoadSpawner == null)
            return;

        float scrollSpeed = GameManager.Instance != null
            ? GameManager.Instance.CurrentGameSpeed
            : 4f;

        if (!sideRoadSpawner.TryCreateSpawnRequest(
                direction,
                Time.time,
                0f,
                scrollSpeed,
                out SideRoadSpawnRequest request))
            return;

        sideRoadSpawner.ExecuteSpawn(request);
    }
#endif
}
