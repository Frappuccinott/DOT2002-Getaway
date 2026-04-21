using UnityEngine;

[RequireComponent(typeof(Collider))]
public class CarIgnition : MonoBehaviour, IInteractable
{
    [Header("References")]
    [SerializeField] private CarStartSystem startSystem;
    [SerializeField] private CarPartSlot driverSeatSlot;

    [Header("Prompts")]
    [SerializeField] private string putHandsPrompt = "Put Hands on Wheel [E]";
    [SerializeField] private string removeHandsPrompt = "Remove Hands [E]";

    public string InteractionPrompt => (carController != null && carController.isHandsOnWheel) ? removeHandsPrompt : putHandsPrompt;
    public InteractionType Type => InteractionType.Interact;

    private CarController carController;

    private void Start()
    {
        carController = GetComponentInParent<CarController>();
    }

    public bool CanInteract
    {
        get
        {
            if (carController == null) return false;

            PlayerController player = FindAnyObjectByType<PlayerController>();
            if (player == null || !player.IsSitting || player.CurrentSeat == null) return false;

            if (driverSeatSlot == null) return true;

            if (player.CurrentSeat.IsChildOf(driverSeatSlot.transform) || player.CurrentSeat == driverSeatSlot.transform)
                return true;

            CarSeat seat = player.CurrentSeat.GetComponentInParent<CarSeat>();
            if (seat != null)
            {
                if (seat.transform.IsChildOf(driverSeatSlot.transform) || seat.transform == driverSeatSlot.transform)
                    return true;
            }

            return false;
        }
    }

    public void Interact() { }

    public void ToggleHandsOnWheel()
    {
        if (carController == null) return;
        carController.isHandsOnWheel = !carController.isHandsOnWheel;
    }
}
