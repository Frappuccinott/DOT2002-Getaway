using UnityEngine;
using TMPro;

public class KeybindUI : MonoBehaviour
{
    public string actionName;
    public string compositePartName = ""; // Örn: "Up", "Down", "Left", "Right"
    public TextMeshProUGUI buttonText;
    public GameObject waitingPanel;

    private void Start()
    {
        if (KeybindManager.Instance != null && KeybindManager.Instance.inputActions != null)
        {
            var action = KeybindManager.Instance.inputActions.asset.FindAction(actionName);
            if (action != null)
            {
                KeybindManager.Instance.UpdateButtonText(action, buttonText, compositePartName);
            }
        }
    }

    public void Rebind()
    {
        if (KeybindManager.Instance != null)
        {
            KeybindManager.Instance.StartRebinding(actionName, compositePartName, buttonText, waitingPanel);
        }
    }
}
