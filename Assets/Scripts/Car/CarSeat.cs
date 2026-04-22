using UnityEngine;

public class CarSeat : MonoBehaviour, IInteractable
{
    [Header("Oturma Ayarları")]
    [SerializeField] private Transform sitPoint;
    
    [Header("Interaction Strings")]
    [SerializeField] private string sitPromptText = "Sit [E]";

    public Transform SitPoint => sitPoint != null ? sitPoint : transform;

    public string InteractionPrompt => sitPromptText;
    public InteractionType Type => InteractionType.Interact;

    public bool CanInteract => true;

}
