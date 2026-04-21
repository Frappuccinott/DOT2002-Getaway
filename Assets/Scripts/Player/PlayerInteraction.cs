using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInteraction : MonoBehaviour
{
    [Header("Raycast")]
    [SerializeField] private float interactionRange = 3f;
    [SerializeField] private LayerMask interactionLayer = ~0;

    [Header("Elde Tutma")]
    [SerializeField] private float holdDistanceMin = 0.5f;
    [SerializeField] private float holdDistanceMax = 2f;
    [SerializeField] private float holdDistanceDefault = 1f;
    [SerializeField] private float scrollSensitivity = 0.1f;
    [SerializeField] private float heldPartScale = 0.5f;

    [Header("Motor Taşıma")]
    [SerializeField] private float engineCarrySpeedMultiplier = 0.5f;

    [Header("Sıvı Aktarımı")]
    [SerializeField] private float fluidTransferRate = 2f;

    [Header("Etkileşim Metinleri")]
    [SerializeField] private string takeFluidContainerPrompt = "Press [F] to grab";
    [SerializeField] private string takeCarPartPrompt = "Press [F] to grab";
    [SerializeField] private string dropPrompt = "Press [F] to drop";
    [SerializeField] private string installPartPrompt = "Press [F] to install";
    [SerializeField] private string removePartPrompt = "Press [F] to remove";
    [SerializeField] private string fillTankPrompt = "Hold [E] to fill";
    [SerializeField] private string openCloseFuelCapPrompt = "Press [E] to open/close";
    [SerializeField] private string dragDoorPrompt = "Hold [LMB] to drag";

    [Header("Referanslar")]
    [SerializeField] private Camera playerCamera;
    [SerializeField] private PlayerController playerController;

    private PlayerInputActions inputActions;

    private IInteractable currentInteractable;
    private HingeDoor currentDoor;
    private CarPartSlot lastLookedSlot;
    private CarFluidTank currentFluidTank;
    private CarIgnition currentIgnition;
    private CarSeat currentSeat;
    private bool isLookingAtCarInterior;

    private PickupableCarPart heldPart;
    private Vector3 heldPartOriginalScale;
    private float currentHoldDistance;

    private float _cameraDefaultYPosition = 0f;

    private FluidContainer heldFluidContainer;
    private Vector3 heldFluidContainerOriginalScale;

    private HingeDoor draggedDoor;
    private bool isTransferring;
    private bool interactPressedLastFrame;

    private static readonly Vector3 VIEWPORT_CENTER = new Vector3(0.5f, 0.5f, 0f);
    private Collider lastHitCollider;
    private CarStartSystem lastHitCarSys;

    public bool HasCarPart => heldPart != null;
    public bool HasFluidContainer => heldFluidContainer != null;
    public FluidType HeldFluidType => heldFluidContainer != null ? heldFluidContainer.FluidType : default;
    public CarPartType HeldPartType => heldPart != null ? heldPart.PartType : default;

    private void Awake()
    {
        inputActions = new PlayerInputActions();
        currentHoldDistance = holdDistanceDefault;

        if (playerCamera == null)
        {
            playerCamera = GetComponentInChildren<Camera>();
            if (playerCamera == null) Debug.LogError("[PlayerInteraction] Kamera bulunamadı!");
        }
        
        if (playerCamera != null)
        {
            _cameraDefaultYPosition = playerCamera.transform.localPosition.y;
        }

        if (playerController == null) playerController = GetComponent<PlayerController>();
    }

    private void OnEnable()
    {
        inputActions.Player.Enable();
        inputActions.Player.Pickup.performed += OnPickupPressed;
    }

    private void OnDisable()
    {
        inputActions.Player.Pickup.performed -= OnPickupPressed;
        inputActions.Player.Disable();
    }

    private void OnDestroy() => inputActions?.Dispose();

    private void Update()
    {
        PerformRaycast();

        if (HasCarPart)
        {
            HandleScrollWheel();
            UpdateHeldObjectPosition(heldPart.transform);
        }
        else if (HasFluidContainer)
        {
            HandleScrollWheel();
            UpdateHeldObjectPosition(heldFluidContainer.transform);
        }

        HandleDoorDrag();
        HandleInteractKey();
        UpdateUI();
    }

    private void PerformRaycast()
    {
        if (playerCamera == null) return;

        Ray ray = playerCamera.ViewportPointToRay(VIEWPORT_CENTER);

        if (!Physics.Raycast(ray, out RaycastHit hit, interactionRange, interactionLayer))
        {
            ClearSlotPreview();
            lastHitCollider = null;
            currentInteractable = null;
            currentDoor = null;
            currentFluidTank = null;
            currentIgnition = null;
            currentSeat = null;
            isLookingAtCarInterior = false;
            return;
        }

        isLookingAtCarInterior = false;

        // OPTIMIZASYON: Sadece baktığımız obje değiştiyse pahalı GetComponent çağrılarını yap.
        if (hit.collider != lastHitCollider)
        {
            lastHitCollider = hit.collider;
            
            currentInteractable = hit.collider.GetComponentInParent<IInteractable>();
            currentSeat = hit.collider.GetComponentInParent<CarSeat>();

            HingeDoor door = hit.collider.GetComponentInParent<HingeDoor>();
            currentDoor = (door != null && door.CanOperate) ? door : null;

            currentFluidTank = hit.collider.GetComponentInParent<CarFluidTank>();
            currentIgnition = hit.collider.GetComponentInParent<CarIgnition>();
            lastHitCarSys = hit.collider.GetComponentInParent<CarStartSystem>();

            CarPartSlot lookedSlot = hit.collider.GetComponentInParent<CarPartSlot>();
            if (lookedSlot != lastLookedSlot)
            {
                lastLookedSlot?.SetLookedAt(false, false);
                lastLookedSlot = lookedSlot;
            }
        }

        if (playerController.IsSitting && playerController.CurrentSeat != null)
        {
            Rigidbody seatRb = playerController.CurrentSeat.GetComponentInParent<Rigidbody>();
            Rigidbody hitRb = hit.collider.attachedRigidbody;

            if (currentIgnition == null)
            {
                if ((seatRb != null && hitRb == seatRb) ||
                    hit.collider.transform.root == playerController.CurrentSeat.root ||
                    lastHitCarSys != null)
                {
                    isLookingAtCarInterior = true;
                }
            }
        }

        if (lastLookedSlot != null)
        {
            bool hasCorrectPart = HasCarPart && heldPart.PartType == lastLookedSlot.AcceptedPartType;
            lastLookedSlot.SetLookedAt(true, hasCorrectPart);
        }
    }

    private void ClearSlotPreview()
    {
        if (lastLookedSlot != null)
        {
            lastLookedSlot.SetLookedAt(false, false);
            lastLookedSlot = null;
        }
    }

    private void OnPickupPressed(InputAction.CallbackContext context)
    {
        if (HasFluidContainer) { DropFluidContainer(); return; }

        if (!HasCarPart && currentInteractable != null && currentInteractable.CanInteract)
        {
            if (currentInteractable is FluidContainer fc) { GrabFluidContainer(fc); return; }
            if (currentInteractable is PickupableCarPart pp) { GrabPart(pp); return; }
            if (currentInteractable is CarPartSlot slot && slot.IsInstalled)
            {
                PickupableCarPart removed = slot.Uninstall();
                if (removed != null) GrabPart(removed);
                return;
            }
        }

        if (HasCarPart)
        {
            if (currentInteractable is CarPartSlot ts && !ts.IsInstalled && heldPart.PartType == ts.AcceptedPartType)
            { InstallPart(ts); return; }

            DropPart();
        }
    }

    private void HandleInteractKey()
    {
        bool isInteractHeld = inputActions.Player.Interact.ReadValue<float>() > 0.5f;

        if (isInteractHeld && !interactPressedLastFrame)
        {
            if (currentIgnition != null)
            {
                if (currentIgnition.CanInteract)
                {
                    currentIgnition.ToggleHandsOnWheel();
                }
            }
            else if (currentDoor != null && currentDoor.Type == HingeDoor.DoorType.FuelCap)
            {
                currentDoor.ToggleOpen();
            }
            else if (playerController.IsSitting && !isLookingAtCarInterior && currentIgnition == null && currentDoor == null)
            {
                if (CanExitVehicle())
                {
                    if (playerController.CurrentSeat != null)
                    {
                        var carCtrl = playerController.CurrentSeat.GetComponentInParent<CarController>();
                        if (carCtrl != null) carCtrl.isHandsOnWheel = false;
                    }
                    playerController.StandUp();
                }
            }
            else if (currentSeat != null && !HasCarPart && !HasFluidContainer)
            {
                playerController.Sit(currentSeat.SitPoint);
            }
        }
        interactPressedLastFrame = isInteractHeld;

        if (isInteractHeld && HasFluidContainer && currentFluidTank != null &&
            heldFluidContainer.FluidType == currentFluidTank.AcceptedFluidType &&
            !heldFluidContainer.IsEmpty && !currentFluidTank.IsFull)
        {
            float consumed = heldFluidContainer.ConsumeFluid(fluidTransferRate * Time.deltaTime);
            currentFluidTank.AddFluid(consumed);
            isTransferring = true;
        }
        else
        {
            isTransferring = false;
        }
    }

    private bool CanExitVehicle()
    {
        if (playerCamera == null || playerController == null) return false;

        Vector3 headPos = playerCamera.transform.position;
        Vector3 exitPos = playerController.StandPosition;
        LayerMask obstacleLayer = ~0;

        if (Physics.Linecast(headPos, exitPos, obstacleLayer))
        {
            return false;
        }

        return true;
    }

    private void GrabObject(GameObject obj, out Vector3 originalScale)
    {
        obj.SetActive(true);

        Rigidbody rb = obj.GetComponent<Rigidbody>();
        if (rb != null) { rb.isKinematic = true; rb.useGravity = false; }

        originalScale = obj.transform.localScale;
        obj.transform.SetParent(playerCamera.transform);
        obj.transform.localScale = originalScale * heldPartScale;
        SetCollidersEnabled(obj, false);
        currentHoldDistance = holdDistanceDefault;
    }

    private void DropObject(GameObject obj, Vector3 originalScale)
    {
        obj.transform.SetParent(null);
        obj.transform.localScale = originalScale;
        SetCollidersEnabled(obj, true);
        obj.SetActive(true);

        Rigidbody rb = obj.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = false;
            rb.useGravity = true;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.AddForce(playerCamera.transform.forward * 1.5f + Vector3.down * 0.5f, ForceMode.VelocityChange);
            rb.AddTorque(Random.insideUnitSphere * 2f, ForceMode.VelocityChange);
        }
    }

    private void GrabPart(PickupableCarPart part)
    {
        heldPart = part;
        GrabObject(part.gameObject, out heldPartOriginalScale);

        if (part.PartType == CarPartType.Engine && playerController != null)
            playerController.SetCarrySpeedMultiplier(engineCarrySpeedMultiplier);
    }

    private void InstallPart(CarPartSlot slot)
    {
        PickupableCarPart part = heldPart;
        slot.Install(part);

        GameObject obj = part.gameObject;
        obj.transform.SetParent(null);
        obj.transform.localScale = heldPartOriginalScale;
        obj.SetActive(false);

        ResetCarryState();
    }

    private void DropPart()
    {
        if (heldPart == null) return;
        DropObject(heldPart.gameObject, heldPartOriginalScale);
        ResetCarryState();
    }

    private void ResetCarryState()
    {
        heldPart = null;
        if (playerController != null) playerController.SetCarrySpeedMultiplier(1f);
    }

    private void GrabFluidContainer(FluidContainer container)
    {
        heldFluidContainer = container;
        GrabObject(container.gameObject, out heldFluidContainerOriginalScale);
    }

    private void DropFluidContainer()
    {
        if (heldFluidContainer == null) return;
        DropObject(heldFluidContainer.gameObject, heldFluidContainerOriginalScale);
        isTransferring = false;
        heldFluidContainer = null;
    }

    private void UpdateHeldObjectPosition(Transform heldTransform)
    {
        if (playerCamera == null || heldTransform == null) return;
        
        float headBobOffset = playerCamera.transform.localPosition.y - _cameraDefaultYPosition;
        Vector3 baseTarget = playerCamera.transform.position + playerCamera.transform.forward * currentHoldDistance;
        Vector3 finalTarget = baseTarget + (playerCamera.transform.up * headBobOffset);

        heldTransform.position = Vector3.Lerp(heldTransform.position, finalTarget, 15f * Time.deltaTime);
        heldTransform.rotation = playerCamera.transform.rotation;
    }

    private void UpdateUI()
    {
        InteractionTooltipUI tooltip = InteractionTooltipUI.Instance;
        if (tooltip == null) return;

        UpdateFluidInfo(tooltip);
        UpdateInteractionPrompt(tooltip);
    }

    private void UpdateFluidInfo(InteractionTooltipUI tooltip)
    {
        if (isTransferring && currentFluidTank != null)
        { tooltip.ShowFluidInfo(currentFluidTank.GetTooltipText()); return; }

        if (HasFluidContainer && currentFluidTank != null && heldFluidContainer.FluidType == currentFluidTank.AcceptedFluidType)
        { tooltip.ShowFluidInfo(currentFluidTank.GetTooltipText()); return; }

        if (HasFluidContainer)
        { tooltip.ShowFluidInfo(heldFluidContainer.GetTooltipText()); return; }

        if (currentInteractable is FluidContainer looked)
        { tooltip.ShowFluidInfo(looked.GetTooltipText()); return; }

        if (currentFluidTank != null)
        { tooltip.ShowFluidInfo(currentFluidTank.GetTooltipText()); return; }

        tooltip.HideFluidInfo();
    }

    private void UpdateInteractionPrompt(InteractionTooltipUI tooltip)
    {
        if (playerController.IsSitting)
        {
            if (currentIgnition != null) { tooltip.ShowPrompt(currentIgnition.InteractionPrompt); return; }
            if (currentDoor != null && currentDoor.Type == HingeDoor.DoorType.FuelCap) { tooltip.ShowPrompt(openCloseFuelCapPrompt); return; }
            if (currentDoor != null) { tooltip.ShowPrompt(dragDoorPrompt); return; }

            if (!isLookingAtCarInterior && CanExitVehicle())
            {
                tooltip.ShowPrompt("Get Out [E]");
            }
            else
            {
                tooltip.HidePrompt();
            }
            return;
        }

        if (HasFluidContainer)
        {
            if (currentFluidTank != null &&
                heldFluidContainer.FluidType == currentFluidTank.AcceptedFluidType &&
                !heldFluidContainer.IsEmpty && !currentFluidTank.IsFull)
            { tooltip.ShowPrompt(fillTankPrompt); return; }

            tooltip.ShowPrompt(dropPrompt); return;
        }

        if (HasCarPart)
        {
            if (currentInteractable is CarPartSlot ts && !ts.IsInstalled && heldPart.PartType == ts.AcceptedPartType)
            { tooltip.ShowPrompt(installPartPrompt); return; }

            tooltip.ShowPrompt(dropPrompt); return;
        }

        if (currentInteractable is FluidContainer) { tooltip.ShowPrompt(takeFluidContainerPrompt); return; }
        if (currentInteractable is PickupableCarPart) { tooltip.ShowPrompt(takeCarPartPrompt); return; }
        if (currentInteractable is CarPartSlot s && s.IsInstalled) { tooltip.ShowPrompt(removePartPrompt); return; }
        if (currentSeat != null && !HasCarPart && !HasFluidContainer) { tooltip.ShowPrompt(currentSeat.InteractionPrompt); return; }
        if (currentIgnition != null) { tooltip.ShowPrompt(currentIgnition.InteractionPrompt); return; }
        if (currentDoor != null && currentDoor.Type == HingeDoor.DoorType.FuelCap) { tooltip.ShowPrompt(openCloseFuelCapPrompt); return; }
        if (currentDoor != null) { tooltip.ShowPrompt(dragDoorPrompt); return; }

        tooltip.HidePrompt();
    }

    private void HandleScrollWheel()
    {
        Vector2 scroll = inputActions.Player.ScrollWheel.ReadValue<Vector2>();
        if (Mathf.Abs(scroll.y) > 0.01f)
        {
            currentHoldDistance += scroll.y * scrollSensitivity * Time.deltaTime;
            currentHoldDistance = Mathf.Clamp(currentHoldDistance, holdDistanceMin, holdDistanceMax);
        }
    }

    private void HandleDoorDrag()
    {
        bool isLeftMouseHeld = inputActions.Player.Attack.ReadValue<float>() > 0.5f;

        if (isLeftMouseHeld)
        {
            if (draggedDoor == null && currentDoor != null) draggedDoor = currentDoor;
            if (draggedDoor != null) draggedDoor.DragDoor(inputActions.Player.Look.ReadValue<Vector2>());
        }
        else
        {
            draggedDoor = null;
        }
    }

    private static void SetCollidersEnabled(GameObject obj, bool enabled)
    {
        foreach (Collider col in obj.GetComponentsInChildren<Collider>())
            col.enabled = enabled;
    }
}
