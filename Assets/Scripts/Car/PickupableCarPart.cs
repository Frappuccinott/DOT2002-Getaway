using UnityEngine;

[RequireComponent(typeof(Collider))]
public class PickupableCarPart : MonoBehaviour, IInteractable
{
    [Header("Parça Ayarları")]
    [SerializeField] private CarPartType partType;
    [SerializeField] private string promptText = "Grab Part [F]";

    private PlayerInteraction cachedPlayer;

    public CarPartType PartType => partType;

    public string InteractionPrompt => promptText;
    public InteractionType Type => InteractionType.Pickup;

    public bool CanInteract
    {
        get
        {
            if (cachedPlayer == null)
                cachedPlayer = GameReferences.Instance?.PlayerInteraction;
            return cachedPlayer == null || (!cachedPlayer.HasCarPart && !cachedPlayer.HasFluidContainer);
        }
    }

}
