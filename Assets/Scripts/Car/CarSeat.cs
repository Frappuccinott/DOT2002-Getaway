using UnityEngine;

// [RequireComponent(typeof(Collider))]
public class CarSeat : MonoBehaviour, IInteractable
{
    [Header("Oturma Ayarları")]
    [Tooltip("Karakterin oturacağı hedef pozisyon ve yön (boş bırakılırsa bu obje baz alınır)")]
    [SerializeField] private Transform sitPoint;
    
    [Header("Interaction Strings")]
    [SerializeField] private string sitPromptText = "Sit [E]";

    public Transform SitPoint => sitPoint != null ? sitPoint : transform;

    public string InteractionPrompt => sitPromptText;
    public InteractionType Type => InteractionType.Interact;

    public bool CanInteract => true;

    public void Interact() { }
}
