using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInteraction : MonoBehaviour
{
    [Header("Raycast")]
    [SerializeField] private float interactionRange = 3f;
    [SerializeField] private LayerMask interactionLayer = ~0;
    [SerializeField] private LayerMask exitObstacleLayer = ~0;

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
    [SerializeField] private string removePartPrompt = "Hold [F] to remove";
    [SerializeField] private string fillTankPrompt = "Hold [E] to fill";
    [SerializeField] private string openCloseFuelCapPrompt = "Press [E] to open/close";
    [SerializeField] private string dragDoorPrompt = "Hold [LMB] to drag";

    [Header("Sökme Ayarları")]
    [SerializeField] private float removeHoldDuration = 1f;

    [Header("Referanslar")]
    [SerializeField] private Camera playerCamera;
    [SerializeField] private PlayerController playerController;

    private PlayerInputActions inputActions;
    private Transform cachedSeatTransform;
    private Rigidbody cachedSeatRb;

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
    private float heldObjectOriginalMass;

    private float _cameraDefaultYPosition = 0f;

    private FluidContainer heldFluidContainer;
    private Vector3 heldFluidContainerOriginalScale;

    private HingeDoor draggedDoor;
    private bool isTransferring;
    private bool interactPressedLastFrame;

    private float removeHoldTimer;
    private bool isHoldingForRemove;
    private CarPartSlot removeTargetSlot;

    private static readonly Vector3 VIEWPORT_CENTER = new Vector3(0.5f, 0.5f, 0f);
    private Collider lastHitCollider;
    private CarStartSystem lastHitCarSys;

    public bool HasCarPart => heldPart != null;
    public bool HasFluidContainer => heldFluidContainer != null;
    public FluidType HeldFluidType => heldFluidContainer != null ? heldFluidContainer.FluidType : default;
    public CarPartType HeldPartType => heldPart != null ? heldPart.PartType : default;

    private void Awake()
    {
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
        inputActions = PlayerInputProvider.Actions;
        if (inputActions == null) return;
        inputActions.Player.Enable();
        inputActions.Player.Pickup.performed += OnPickupPressed;
    }

    private void OnDisable()
    {
        if (inputActions == null) return;
        inputActions.Player.Pickup.performed -= OnPickupPressed;
    }

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

        if (HasCarPart || HasFluidContainer)
        {
            if (inputActions.Player.Attack.WasPressedThisFrame())
            {
                bool isLookingAtInteractable = currentInteractable != null || currentDoor != null || currentFluidTank != null || currentIgnition != null || currentSeat != null || lastLookedSlot != null;
                if (!isLookingAtInteractable)
                {
                    ThrowHeldObject();
                    return;
                }
            }
        }

        HandleRemoveHold();
        HandleDoorDrag();
        HandleInteractKey();
        UpdateUI();
    }

    private RaycastHit[] hits = new RaycastHit[20];

    private void PerformRaycast()
    {
        if (playerCamera == null) return;

        Ray ray = playerCamera.ViewportPointToRay(VIEWPORT_CENTER);
        Debug.DrawRay(ray.origin, ray.direction * interactionRange, Color.red);

        int hitCount = Physics.RaycastNonAlloc(ray, hits, interactionRange, interactionLayer, QueryTriggerInteraction.Collide);
        if (hitCount == 0)
        {
            ClearInteractionState();
            return;
        }

        System.Array.Sort(hits, 0, hitCount, new RaycastHitComparer());

        Collider bestCollider = null;
        IInteractable foundInteractable = null;
        CarSeat foundSeat = null;
        HingeDoor foundDoor = null;
        CarFluidTank foundFluidTank = null;
        CarIgnition foundIgnition = null;
        CarPartSlot foundSlot = null;
        CarStartSystem foundCarSys = null;
        Vector3 hitPoint = Vector3.zero;

        Collider fallbackCarCollider = null;
        Vector3 fallbackHitPoint = Vector3.zero;

        for (int i = 0; i < hitCount; i++)
        {
            var hit = hits[i];
            var interactables = hit.collider.GetComponentsInParent<IInteractable>();
            IInteractable interactable = null;
            foreach (var ia in interactables)
            {
                if (ia is MonoBehaviour mb && !mb.enabled) continue;
                interactable = ia;
                break;
            }

            CarSeat seat = hit.collider.GetComponentInParent<CarSeat>();
            HingeDoor door = hit.collider.GetComponentInParent<HingeDoor>();
            CarFluidTank fluidTank = hit.collider.GetComponentInParent<CarFluidTank>();
            CarIgnition ignition = hit.collider.GetComponentInParent<CarIgnition>();
            CarPartSlot slot = hit.collider.GetComponentInParent<CarPartSlot>();

            if (slot != null && !slot.IsInstalled && !slot.CanInteract)
            {
                slot = null;
                if (interactable is CarPartSlot) interactable = null;
            }

            if (interactable != null && interactable as MonoBehaviour != null)
            {
                if (HasCarPart && (interactable as MonoBehaviour).gameObject == heldPart.gameObject)
                {
                    interactable = null;
                }
            }

            if (interactable == null && seat == null && door == null && fluidTank == null && ignition == null && slot == null)
            {
                if (fallbackCarCollider == null && (hit.collider.transform.root.GetComponentInChildren<CarController>() != null || 
                    hit.collider.transform.root.GetComponentInChildren<CarStartSystem>() != null))
                {
                    fallbackCarCollider = hit.collider;
                    fallbackHitPoint = hit.point;
                }
                continue;
            }

            bestCollider = hit.collider;
            foundInteractable = interactable;
            foundSeat = seat;
            foundDoor = door;
            foundFluidTank = fluidTank;
            foundIgnition = ignition;
            foundSlot = slot;
            foundCarSys = hit.collider.GetComponentInParent<CarStartSystem>();
            hitPoint = hit.point;
            break;
        }

        if (bestCollider == null)
        {
            if (fallbackCarCollider != null)
            {
                bestCollider = fallbackCarCollider;
                hitPoint = fallbackHitPoint;
            }
            else
            {
                ClearInteractionState();
                return;
            }
        }

        if (bestCollider != lastHitCollider)
        {
            lastHitCollider = bestCollider;
            currentInteractable = foundInteractable;

            if (foundSeat == null && foundDoor == null && foundFluidTank == null && foundIgnition == null)
            {
                Transform root = bestCollider.transform.root;
                if (root.GetComponentInChildren<CarController>() != null || root.GetComponentInChildren<CarStartSystem>() != null)
                {
                    CarSeat[] allSeats = root.GetComponentsInChildren<CarSeat>();
                    float closestDist = float.MaxValue;
                    foreach (var s in allSeats)
                    {
                        if (!s.enabled || (s.TryGetComponent<PickupableCarPart>(out var p) && p.enabled)) continue;
                        
                        float dist = Vector3.Distance(hitPoint, s.transform.position);
                        if (dist < closestDist && dist < 1.0f)
                        {
                            closestDist = dist;
                            foundSeat = s;
                        }
                    }
                }
            }

            bool hasActivePickupable = foundSeat != null && foundSeat.TryGetComponent<PickupableCarPart>(out var pcp) && pcp.enabled;
            if (foundSeat != null && foundSeat.enabled && !hasActivePickupable)
                currentSeat = foundSeat;
            else
                currentSeat = null;

            currentDoor = (foundDoor != null && foundDoor.CanOperate) ? foundDoor : null;
            currentFluidTank = foundFluidTank;
            currentIgnition = foundIgnition;
            lastHitCarSys = foundCarSys;

            if (foundSlot != lastLookedSlot)
            {
                lastLookedSlot?.SetLookedAt(false, false);
                lastLookedSlot = foundSlot;
            }
        }

        isLookingAtCarInterior = false;
        if (bestCollider != null && playerController != null && playerController.IsSitting && playerController.CurrentSeat != null)
        {
            if (bestCollider.transform.root == playerController.CurrentSeat.root)
            {
                isLookingAtCarInterior = true;

                CarPartSlot slot = bestCollider.GetComponentInParent<CarPartSlot>();
                if (slot != null && !slot.IsInstalled)
                {
                    if (slot.AcceptedPartType == CarPartType.FrontDoorLeft ||
                        slot.AcceptedPartType == CarPartType.FrontDoorRight ||
                        slot.AcceptedPartType == CarPartType.RearDoorLeft ||
                        slot.AcceptedPartType == CarPartType.RearDoorRight)
                    {
                        isLookingAtCarInterior = false;
                    }
                }
            }

            if (cachedSeatTransform != playerController.CurrentSeat)
            {
                cachedSeatTransform = playerController.CurrentSeat;
                cachedSeatRb = cachedSeatTransform.GetComponentInParent<Rigidbody>();
            }
        }

        if (lastLookedSlot != null)
        {
            bool hasCorrectPart = HasCarPart && heldPart.PartType == lastLookedSlot.AcceptedPartType;
            lastLookedSlot.SetLookedAt(true, hasCorrectPart);
        }
    }

    private void ClearInteractionState()
    {
        ClearSlotPreview();
        lastHitCollider = null;
        currentInteractable = null;
        currentDoor = null;
        currentFluidTank = null;
        currentIgnition = null;
        currentSeat = null;
        isLookingAtCarInterior = false;
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
            if (currentInteractable is CarPartSlot slot && slot.IsInstalled && !isHoldingForRemove)
            {
                if (slot.HasInstalledChildSlots()) return;

                isHoldingForRemove = true;
                removeHoldTimer = 0f;
                removeTargetSlot = slot;
                return;
            }
        }

        if (HasCarPart)
        {
            if (currentInteractable is CarPartSlot ts && !ts.IsInstalled && heldPart.PartType == ts.AcceptedPartType)
            { InstallPart(ts); return; }

            // Fallback: direct raycast slot search
            if (playerCamera != null)
            {
                Ray ray = playerCamera.ViewportPointToRay(VIEWPORT_CENTER);
                Debug.DrawRay(ray.origin, ray.direction * interactionRange, Color.yellow, 1f);
                if (Physics.Raycast(ray, out RaycastHit fallbackHit, interactionRange, interactionLayer, QueryTriggerInteraction.Collide))
                {
                    Transform root = fallbackHit.collider.transform.root;
                    CarPartSlot[] allSlots = root.GetComponentsInChildren<CarPartSlot>(true);
                    float closestDist = float.MaxValue;
                    CarPartSlot bestSlot = null;
                    foreach (var s in allSlots)
                    {
                        if (s.IsInstalled || s.AcceptedPartType != heldPart.PartType) continue;
                        float dist = Vector3.Distance(fallbackHit.point, s.transform.position);
                        if (dist < closestDist && dist < 2f)
                        {
                            closestDist = dist;
                            bestSlot = s;
                        }
                    }
                    if (bestSlot != null) { InstallPart(bestSlot); return; }
                }
            }

            DropPart();
        }
    }

    private void HandleRemoveHold()
    {
        if (!isHoldingForRemove) return;
        bool isFHeld = inputActions.Player.Pickup.ReadValue<float>() > 0.5f;

        if (!isFHeld || removeTargetSlot == null || !removeTargetSlot.IsInstalled)
        {
            isHoldingForRemove = false;
            removeHoldTimer = 0f;
            removeTargetSlot = null;
            return;
        }

        if (!(currentInteractable is CarPartSlot lookSlot) || lookSlot != removeTargetSlot)
        {
            isHoldingForRemove = false;
            removeHoldTimer = 0f;
            removeTargetSlot = null;
            return;
        }

        removeHoldTimer += Time.deltaTime;

        if (removeHoldTimer >= removeHoldDuration)
        {
            PickupableCarPart removed = removeTargetSlot.Uninstall();
            if (removed != null) GrabPart(removed);

            isHoldingForRemove = false;
            removeHoldTimer = 0f;
            removeTargetSlot = null;
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
            else if (currentDoor != null)
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
        Vector3 exitPos = playerController.StandPosition + Vector3.up * 0.5f;

        Vector3 dir = exitPos - headPos;
        float dist = dir.magnitude;
        if (dist > 0.01f)
        {
            Debug.DrawRay(headPos, dir.normalized * dist, Color.blue);
            RaycastHit[] hits = Physics.RaycastAll(headPos, dir.normalized, dist, exitObstacleLayer);
            Transform currentCarRoot = playerController.CurrentSeat != null ? playerController.CurrentSeat.root : null;

            foreach (var hit in hits)
            {
                if (!hit.collider.isTrigger && (currentCarRoot == null || hit.collider.transform.root != currentCarRoot))
                {
                    return false;
                }
            }
        }

        return true;
    }

    private void GrabObject(GameObject obj, out Vector3 originalScale)
    {
        obj.SetActive(true);

        Rigidbody rb = obj.GetComponent<Rigidbody>();
        if (rb != null) 
        { 
            rb.isKinematic = false;
            heldObjectOriginalMass = rb.mass;
            rb.mass = 0.0001f;
            rb.useGravity = false;
            rb.linearDamping = 10f;
            rb.angularDamping = 10f;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
        }

        originalScale = obj.transform.localScale;
        obj.transform.SetParent(playerCamera.transform);
        obj.transform.localScale = originalScale * heldPartScale;
        
        SetLayerRecursively(obj, LayerMask.NameToLayer("Ignore Raycast"));
        
        currentHoldDistance = holdDistanceDefault;
    }

    private void DropObject(GameObject obj, Vector3 originalScale)
    {
        obj.transform.SetParent(null);
        obj.transform.localScale = originalScale;
        
        SetLayerRecursively(obj, LayerMask.NameToLayer("Interactable"));
        
        obj.SetActive(true);

        Rigidbody rb = obj.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = false;
            rb.mass = heldObjectOriginalMass;
            rb.useGravity = true;
            rb.linearDamping = 0.05f;
            rb.angularDamping = 0.05f;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.AddForce(playerCamera.transform.forward * 1.5f + Vector3.down * 0.5f, ForceMode.VelocityChange);
            rb.AddTorque(Random.insideUnitSphere * 2f, ForceMode.VelocityChange);
        }
    }

    private void ThrowHeldObject()
    {
        GameObject objToThrow = null;
        Vector3 originalScale = Vector3.one;

        if (HasCarPart)
        {
            objToThrow = heldPart.gameObject;
            originalScale = heldPartOriginalScale;
            ResetCarryState();
        }
        else if (HasFluidContainer)
        {
            objToThrow = heldFluidContainer.gameObject;
            originalScale = heldFluidContainerOriginalScale;
            heldFluidContainer = null;
            isTransferring = false;
        }

        if (objToThrow == null) return;

        objToThrow.transform.SetParent(null);
        objToThrow.transform.localScale = originalScale;
        SetLayerRecursively(objToThrow, LayerMask.NameToLayer("Interactable"));
        objToThrow.SetActive(true);

        Rigidbody rb = objToThrow.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = false;
            rb.mass = heldObjectOriginalMass;
            rb.useGravity = true;
            rb.linearDamping = 0.05f;
            rb.angularDamping = 0.05f;
            rb.linearVelocity = playerCamera.transform.forward * 12f;
            rb.angularVelocity = Random.insideUnitSphere * 5f;
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
        slot.Install(part, heldPartOriginalScale);
        SetLayerRecursively(part.gameObject, LayerMask.NameToLayer("Interactable"));
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

        Rigidbody rb = heldTransform.GetComponent<Rigidbody>();
        if (rb != null && !rb.isKinematic)
        {
            Vector3 dir = finalTarget - rb.position;
            float dist = dir.magnitude;

            if (dist > 3f) 
            {
                rb.position = finalTarget;
                rb.linearVelocity = Vector3.zero;
            }
            else
            {
                rb.linearVelocity = dir * 40f; 
            }

            Quaternion deltaRot = playerCamera.transform.rotation * Quaternion.Inverse(rb.rotation);
            deltaRot.ToAngleAxis(out float angle, out Vector3 axis);
            if (angle > 180f) angle -= 360f;
            rb.angularVelocity = (angle * axis * Mathf.Deg2Rad) * 35f;
        }
        else
        {
            heldTransform.position = finalTarget;
            heldTransform.rotation = playerCamera.transform.rotation;
        }
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
        if (playerController != null && playerController.IsSitting)
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
        if (currentInteractable is CarPartSlot s && s.IsInstalled)
        {
            if (s.HasInstalledChildSlots())
            {
                tooltip.ShowPrompt("Remove attached parts first!");
                return;
            }

            if (isHoldingForRemove && removeTargetSlot == s)
            {
                float progress = Mathf.Clamp01(removeHoldTimer / removeHoldDuration) * 100f;
                tooltip.ShowPrompt($"Removing... {progress:F0}%");
            }
            else
            {
                tooltip.ShowPrompt(removePartPrompt);
            }
            return;
        }
        if (currentSeat != null && !HasCarPart && !HasFluidContainer) { tooltip.ShowPrompt(currentSeat.InteractionPrompt); return; }
        if (currentIgnition != null) { tooltip.ShowPrompt(currentIgnition.InteractionPrompt); return; }
        if (currentDoor != null) 
        { 
            if (currentDoor.Type == HingeDoor.DoorType.FuelCap || currentDoor.Type == HingeDoor.DoorType.GenericDoor)
                tooltip.ShowPrompt("Open/Close [E]");
            else
                tooltip.ShowPrompt($"{dragDoorPrompt} / Open/Close [E]"); 
            return; 
        }

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

    private void SetLayerRecursively(GameObject obj, int newLayer)
    {
        if (obj == null) return;
        obj.layer = newLayer;
        foreach (Transform child in obj.transform)
        {
            if (child == null) continue;
            SetLayerRecursively(child.gameObject, newLayer);
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

    private struct RaycastHitComparer : System.Collections.Generic.IComparer<RaycastHit>
    {
        public int Compare(RaycastHit x, RaycastHit y)
        {
            return x.distance.CompareTo(y.distance);
        }
    }
}
