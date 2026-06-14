using UnityEngine;
using TMPro;

public class KeybindUI : MonoBehaviour
{
    public string actionName;
    public TextMeshProUGUI buttonText;
    public GameObject waitingPanel;

    private void Start()
    {
        // Başlangıçta tuş ismini güncelle
        if (KeybindManager.Instance != null && KeybindManager.Instance.inputActions != null)
        {
            var action = KeybindManager.Instance.inputActions.asset.FindAction(actionName);
            if (action != null)
            {
                KeybindManager.Instance.UpdateButtonText(action, buttonText);
            }
        }
    }

    public void Rebind()
    {
        if (KeybindManager.Instance != null)
        {
            KeybindManager.Instance.StartRebinding(actionName, buttonText, waitingPanel);
        }
    }
}
