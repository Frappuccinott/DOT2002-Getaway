using UnityEngine;
using UnityEngine.InputSystem;

public partial class CarController
{
    private void OnEnable()
    {
        if (moveAction != null) moveAction.Enable();
        if (handbrakeAction != null)
        {
            handbrakeAction.Enable();
            handbrakeAction.performed += ToggleHandbrake;
        }
        if (headlightsAction != null)
        {
            headlightsAction.Enable();
            headlightsAction.performed += ToggleHeadlights;
        }
    }

    private void OnDisable()
    {
        if (moveAction != null) moveAction.Disable();
        if (handbrakeAction != null)
        {
            handbrakeAction.Disable();
            handbrakeAction.performed -= ToggleHandbrake;
        }
        if (headlightsAction != null)
        {
            headlightsAction.Disable();
            headlightsAction.performed -= ToggleHeadlights;
        }
    }

    private void ToggleHandbrake(InputAction.CallbackContext context)
    {
        if (!isHandsOnWheel) return;

        isHandbrakeEngaged = !isHandbrakeEngaged;
        targetHandbrakeRot = isHandbrakeEngaged ? -30f : 10f;
        OnHandbrakeToggled?.Invoke(isHandbrakeEngaged);
    }

    private void ToggleHeadlights(InputAction.CallbackContext context)
    {
        if (!isHandsOnWheel) return;
        if (currentBatteryPercent <= 0f) return;

        areHeadlightsOn = !areHeadlightsOn;
        if (headlights != null)
        {
            foreach (var light in headlights) { if (light != null) light.SetActive(areHeadlightsOn); }
        }
        OnHeadlightsToggled?.Invoke(areHeadlightsOn);
    }
}
