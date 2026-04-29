using UnityEngine;

public class PlayerInputProvider : MonoBehaviour
{
    private static PlayerInputActions actions;

    public static PlayerInputActions Actions
    {
        get
        {
            if (actions == null) actions = new PlayerInputActions();
            return actions;
        }
    }

    private void Awake()
    {
        if (actions == null) actions = new PlayerInputActions();
    }

    private void OnDestroy()
    {
        actions?.Dispose();
        actions = null;
    }
}
