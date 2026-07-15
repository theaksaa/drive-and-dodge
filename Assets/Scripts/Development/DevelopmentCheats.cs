using UnityEngine;
#if UNITY_EDITOR
using UnityEngine.InputSystem;
#endif

public sealed class DevelopmentCheats : MonoBehaviour
{
#if UNITY_EDITOR
    private PlayerHealth playerHealth;
    private FuelSystem fuelSystem;

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
    }
#endif
}
