using UnityEngine;

[RequireComponent(typeof(Collider))]
public class CarFluidTank : MonoBehaviour, IInteractable
{
    [Header("Depo Ayarları")]
    [SerializeField] private FluidType acceptedFluidType = FluidType.Gasoline;
    [SerializeField] private float maxCapacity = 40f;
    [SerializeField] private float currentFluid = 0f;

    private PlayerInteraction cachedPlayer;

    public float CurrentFluid => currentFluid;
    public float MaxCapacity => maxCapacity;
    public FluidType AcceptedFluidType => acceptedFluidType;
    public bool IsFull => currentFluid >= maxCapacity;
    public float RemainingSpace => maxCapacity - currentFluid;

    public string InteractionPrompt => $"Fill {acceptedFluidType.GetDisplayName()} [E]";
    public InteractionType Type => InteractionType.Interact;

    public bool CanInteract
    {
        get
        {
            if (cachedPlayer == null)
                cachedPlayer = GameReferences.Instance?.PlayerInteraction;
            if (cachedPlayer == null) return false;
            return cachedPlayer.HasFluidContainer
                && cachedPlayer.HeldFluidType == acceptedFluidType
                && !IsFull;
        }
    }


    public float AddFluid(float amount)
    {
        float space = maxCapacity - currentFluid;
        float added = Mathf.Min(amount, space);
        currentFluid += added;
        if (currentFluid > maxCapacity - 0.005f) currentFluid = maxCapacity;
        return added;
    }

    public float ConsumeFluid(float amount)
    {
        float consumed = Mathf.Min(amount, currentFluid);
        currentFluid -= consumed;
        if (currentFluid < 0.005f) currentFluid = 0f;
        return consumed;
    }

    public string GetTooltipText()
    {
        return $"{currentFluid:F2}/{maxCapacity:F0} L {acceptedFluidType.GetDisplayName()}";
    }
}
