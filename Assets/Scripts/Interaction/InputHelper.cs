using UnityEngine;
using UnityEngine.InputSystem;

public static class InputHelper
{
    public static InputAction FindDrivingAction(string actionName, bool throwIfNotFound = false)
    {
        PlayerInput playerInput = Object.FindAnyObjectByType<PlayerInput>();
        if (playerInput != null && playerInput.actions != null)
        {
            InputActionMap drivingMap = playerInput.actions.FindActionMap("Driving");
            if (drivingMap != null)
            {
                InputAction action = drivingMap.FindAction(actionName, throwIfNotFound);
                if (action != null) return action;
            }
        }

        InputActionAsset[] allAssets = Resources.FindObjectsOfTypeAll<InputActionAsset>();
        foreach (var asset in allAssets)
        {
            InputActionMap drivingMap = asset.FindActionMap("Driving");
            if (drivingMap != null)
            {
                InputAction action = drivingMap.FindAction(actionName, throwIfNotFound);
                if (action != null) return action;
            }
        }

        return null;
    }
}
