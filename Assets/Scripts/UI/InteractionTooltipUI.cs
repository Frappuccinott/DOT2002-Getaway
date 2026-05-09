using UnityEngine;
using TMPro;

public class InteractionTooltipUI : MonoBehaviour
{
    [Header("Text Referansları")]
    [SerializeField] private TextMeshProUGUI fluidInfoText;
    [SerializeField] private TextMeshProUGUI interactionPromptText;

    public static InteractionTooltipUI Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        if (fluidInfoText != null) fluidInfoText.gameObject.SetActive(false);
        if (interactionPromptText != null) interactionPromptText.gameObject.SetActive(false);
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    public void ShowFluidInfo(string text)
    {
        if (fluidInfoText == null) return;
        fluidInfoText.text = text;
        fluidInfoText.gameObject.SetActive(true);
    }

    public void HideFluidInfo()
    {
        if (fluidInfoText == null) return;
        fluidInfoText.gameObject.SetActive(false);
    }

    public void ShowPrompt(string text)
    {
        if (interactionPromptText == null) return;
        interactionPromptText.text = text;
        interactionPromptText.gameObject.SetActive(true);
    }

    public void HidePrompt()
    {
        if (interactionPromptText == null) return;
        interactionPromptText.gameObject.SetActive(false);
    }
}
