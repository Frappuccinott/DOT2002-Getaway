using UnityEngine;

[RequireComponent(typeof(Collider))]
public class FluidContainer : MonoBehaviour, IInteractable
{
    [Header("Sıvı Ayarları")]
    [SerializeField] private FluidType fluidType = FluidType.Gasoline;
    [SerializeField] private float maxCapacity = 10f;
    [SerializeField] private float initialFluid = 0f;

    [Header("Random Sıvı")]
    [SerializeField] private float randomFluidMin = 2f;
    [SerializeField] private float randomFluidMax = 10f;

    private float currentFluid;
    private PlayerInteraction cachedPlayer;

    public float CurrentFluid => currentFluid;
    public float MaxCapacity => maxCapacity;
    public FluidType FluidType => fluidType;
    public bool IsEmpty => currentFluid <= 0f;

    public string InteractionPrompt => $"Grab {fluidType.GetDisplayName()} [F]";
    public InteractionType Type => InteractionType.Pickup;

    public bool CanInteract
    {
        get
        {
            if (cachedPlayer == null)
                cachedPlayer = GameReferences.Instance?.PlayerInteraction;
            if (cachedPlayer == null) return true;
            return !cachedPlayer.HasCarPart && !cachedPlayer.HasFluidContainer;
        }
    }

    protected virtual void Start()
    {
        if (initialFluid > 0f)
        {
            currentFluid = Mathf.Clamp(initialFluid, 0f, maxCapacity);
        }
        else
        {
            float min = Mathf.Clamp(randomFluidMin, 0f, maxCapacity);
            float max = Mathf.Clamp(randomFluidMax, min, maxCapacity);
            currentFluid = Random.Range(min, max);
        }
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
        return $"{currentFluid:F2}/{maxCapacity:F0} L {fluidType.GetDisplayName()}";
    }
}
