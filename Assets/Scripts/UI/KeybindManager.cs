using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using TMPro;

public class KeybindManager : MonoBehaviour
{
    public static KeybindManager Instance;
    public PlayerInputActions inputActions;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        inputActions = PlayerInputProvider.Actions;
        LoadBindings();
    }

    public void StartRebinding(string actionName, TextMeshProUGUI buttonText, GameObject waitingTextObj)
    {
        InputAction actionToRebind = inputActions.asset.FindAction(actionName);
        if (actionToRebind == null)
        {
            Debug.LogError("Action bulunamadı: " + actionName);
            return;
        }

        actionToRebind.Disable();

        // 1D Axis veya Button ayrımı (WASD için daha karmaşık, şimdilik sadece Butonları destekleyelim)
        // Eğer WASD (Move) gibi bir vektör ise özel işlem gerekir. Biz şimdilik genel tuşları destekliyoruz.
        var rebindOperation = actionToRebind.PerformInteractiveRebinding()
            .WithControlsExcluding("Mouse") // Mouse hareketlerini kaydetmemesi için
            .OnMatchWaitForAnother(0.1f)
            .OnComplete(operation =>
            {
                operation.Dispose();
                actionToRebind.Enable();
                UpdateButtonText(actionToRebind, buttonText);
                SaveBindings();
                if (waitingTextObj != null) waitingTextObj.SetActive(false);
            })
            .OnCancel(operation =>
            {
                operation.Dispose();
                actionToRebind.Enable();
                if (waitingTextObj != null) waitingTextObj.SetActive(false);
            })
            .Start();

        if (waitingTextObj != null) waitingTextObj.SetActive(true);
        buttonText.text = "...";
    }

    public void UpdateButtonText(InputAction action, TextMeshProUGUI buttonText)
    {
        // İlk bağlanan tuşun adını al
        int bindingIndex = action.GetBindingIndexForControl(action.controls[0]);
        if (bindingIndex >= 0)
        {
            buttonText.text = InputControlPath.ToHumanReadableString(
                action.bindings[bindingIndex].effectivePath,
                InputControlPath.HumanReadableStringOptions.OmitDevice);
        }
    }

    public void SaveBindings()
    {
        string rebinds = inputActions.asset.SaveBindingOverridesAsJson();
        PlayerPrefs.SetString("rebinds", rebinds);
        PlayerPrefs.Save();
    }

    public void LoadBindings()
    {
        string rebinds = PlayerPrefs.GetString("rebinds", string.Empty);
        if (!string.IsNullOrEmpty(rebinds))
        {
            inputActions.asset.LoadBindingOverridesFromJson(rebinds);
        }
    }
}
