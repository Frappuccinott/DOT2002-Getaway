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

    public void StartRebinding(string actionName, string compositePartName, TextMeshProUGUI buttonText, GameObject waitingTextObj)
    {
        InputAction actionToRebind = inputActions.asset.FindAction(actionName);
        if (actionToRebind == null)
        {
            Debug.LogError("Action bulunamadı: " + actionName);
            return;
        }

        int bindingIndex = ResolveBindingIndex(actionToRebind, compositePartName);
        if (bindingIndex < 0 && string.IsNullOrEmpty(compositePartName))
        {
            if (actionToRebind.controls.Count > 0)
                bindingIndex = actionToRebind.GetBindingIndexForControl(actionToRebind.controls[0]);
            else
                bindingIndex = 0;
        }

        if (bindingIndex < 0)
        {
            Debug.LogError("Binding index çözülemedi! compositePartName: " + compositePartName);
            return;
        }

        actionToRebind.Disable();

        var rebindOperation = actionToRebind.PerformInteractiveRebinding(bindingIndex)
            .WithControlsExcluding("Mouse") // Mouse hareketlerini kaydetmemesi için
            .OnMatchWaitForAnother(0.1f)
            .OnComplete(operation =>
            {
                operation.Dispose();
                actionToRebind.Enable();
                UpdateButtonText(actionToRebind, buttonText, compositePartName);

                // --- Ortak Tuş (Yaya / Araç) Senkronizasyonu ---
                if (actionName == "Driving/Move" || actionName == "Player/Move")
                {
                    string otherActionName = actionName == "Driving/Move" ? "Player/Move" : "Driving/Move";
                    InputAction otherAction = inputActions.asset.FindAction(otherActionName);
                    if (otherAction != null)
                    {
                        int otherIndex = ResolveBindingIndex(otherAction, compositePartName);
                        if (otherIndex >= 0 && otherIndex < otherAction.bindings.Count)
                        {
                            otherAction.ApplyBindingOverride(otherIndex, actionToRebind.bindings[bindingIndex].effectivePath);
                        }
                    }
                }
                // ----------------------------------------------

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

    private int ResolveBindingIndex(InputAction action, string compositePartName)
    {
        if (string.IsNullOrEmpty(compositePartName)) return -1;
        
        string lowerPart = compositePartName.ToLower();
        
        // Önce özellikle klavyeye atanmış olanı bulmayı dene
        for (int i = 0; i < action.bindings.Count; i++)
        {
            var binding = action.bindings[i];
            if (binding.isPartOfComposite && binding.name.ToLower() == lowerPart && binding.path.Contains("<Keyboard>"))
            {
                return i;
            }
        }
        
        // Klavyeli bulamazsan, sadece isme göre ilk bulduğunu dön
        for (int i = 0; i < action.bindings.Count; i++)
        {
            var binding = action.bindings[i];
            if (binding.isPartOfComposite && binding.name.ToLower() == lowerPart)
            {
                return i;
            }
        }
        return -1;
    }

    public void UpdateButtonText(InputAction action, TextMeshProUGUI buttonText, string compositePartName = "")
    {
        int indexToUse = ResolveBindingIndex(action, compositePartName);
        
        if (indexToUse < 0)
        {
            if (action.controls.Count > 0)
                indexToUse = action.GetBindingIndexForControl(action.controls[0]);
            else
                indexToUse = 0;
        }
        
        if (indexToUse >= 0 && indexToUse < action.bindings.Count)
        {
            buttonText.text = InputControlPath.ToHumanReadableString(
                action.bindings[indexToUse].effectivePath,
                InputControlPath.HumanReadableStringOptions.OmitDevice);
        }
    }

    public void SaveBindings()
    {
        string rebinds = inputActions.asset.SaveBindingOverridesAsJson();
        PlayerPrefs.SetString("rebinds", rebinds);
        PlayerPrefs.Save();

        PlayerInput playerInput = Object.FindAnyObjectByType<PlayerInput>();
        if (playerInput != null && playerInput.actions != null)
        {
            playerInput.actions.LoadBindingOverridesFromJson(rebinds);
        }
    }

    public void LoadBindings()
    {
        string rebinds = PlayerPrefs.GetString("rebinds", string.Empty);
        if (!string.IsNullOrEmpty(rebinds))
        {
            inputActions.asset.LoadBindingOverridesFromJson(rebinds);

            PlayerInput playerInput = Object.FindAnyObjectByType<PlayerInput>();
            if (playerInput != null && playerInput.actions != null)
            {
                playerInput.actions.LoadBindingOverridesFromJson(rebinds);
            }
        }
    }
}
