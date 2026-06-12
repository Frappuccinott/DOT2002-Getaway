using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    [Header("Hareket Ayarları")]
    [SerializeField] private float walkSpeed = 5f;
    [SerializeField] private float runSpeed = 9f;
    [SerializeField] private float crouchSpeed = 2.5f;
    [SerializeField] private float moveSmoothTime = 0.1f;

    [Header("Taşıma Ayarları")]
    private float carrySpeedMultiplier = 1f;

    [Header("Zıplama Ayarları")]
    [SerializeField] private float jumpForce = 7f;
    [SerializeField] private float gravity = 20f;

    [Header("Eğilme Ayarları")]
    [SerializeField] private float standHeight = 2f;
    [SerializeField] private float crouchHeight = 1f;
    [SerializeField] private float crouchTransitionSpeed = 8f;

    [Header("Oturma Ayarları")]
    [SerializeField] private float sitHeight = 1.2f;
    [SerializeField] private float sitLerpSpeed = 10f;

    private CharacterController controller;
    private PlayerInputActions inputActions;
    private Collider[] allColliders;
    private Rigidbody rb;
    private PlayerLook playerLook;

    // Sersemletme (stun) kontrolü için PlayerHealth referansı
    private PlayerHealth playerHealth;

    private Vector3 velocity;
    private Vector2 currentMoveInput;
    private Vector2 smoothMoveVelocity;
    private Vector2 smoothedMoveInput;
    private bool isSprinting;
    private bool isCrouching;
    private float targetHeight;

    private bool isSitting;
    private bool isStandingUp;
    private Transform currentSeat;
    private Vector3 standPosition;
    private Vector3 localStandOffset;

    private Vector3 defaultCenter;
    private float defaultHeight;

    public bool IsGrounded => controller.isGrounded;
    public bool IsSprinting => isSprinting && !isCrouching && currentMoveInput.sqrMagnitude > 0.01f;
    public bool IsCrouching => isCrouching;
    public bool IsSitting => isSitting;
    public Transform CurrentSeat => currentSeat;
    public Vector3 StandPosition => currentSeat != null ? currentSeat.TransformPoint(localStandOffset) : standPosition;
    public bool IsMoving => currentMoveInput.sqrMagnitude > 0.01f && !isSitting;

    public float CrouchRatio
    {
        get
        {
            if (defaultHeight <= crouchHeight) return 0f;
            return 1f - (controller.height - crouchHeight) / (defaultHeight - crouchHeight);
        }
    }

    public float CurrentSpeed
    {
        get
        {
            float baseSpeed;
            if (isCrouching) baseSpeed = crouchSpeed;
            else if (IsSprinting) baseSpeed = runSpeed;
            else baseSpeed = walkSpeed;

            return baseSpeed * carrySpeedMultiplier;
        }
    }

    public void SetCarrySpeedMultiplier(float multiplier)
    {
        carrySpeedMultiplier = Mathf.Clamp(multiplier, 0.1f, 1f);
    }

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
        allColliders = GetComponents<Collider>();
        rb = GetComponent<Rigidbody>();
        playerLook = GetComponent<PlayerLook>();
        playerHealth = GetComponent<PlayerHealth>();

        defaultHeight = controller.height;
        standHeight = defaultHeight;
        targetHeight = standHeight;

        if (rb != null) rb.isKinematic = true;
    }

    private void OnEnable()
    {
        inputActions = PlayerInputProvider.Actions;
        if (inputActions == null) return;
        inputActions.Player.Enable();

        inputActions.Player.Jump.performed += OnJump;
    }

    private void OnDisable()
    {
        if (inputActions == null) return;
        inputActions.Player.Jump.performed -= OnJump;
    }

    private void Update()
    {
        if (isSitting)
        {
            HandleSitLerp();
            return;
        }
        else if (isStandingUp)
        {
            HandleStandUpLerp();
            return;
        }

        // ===== SERSEMLETMEn (STUN) KONTROLÜ =====
        // Oyuncu sersemletilmişse hareket inputunu sıfırla,
        // sadece yerçekimi uygulanmaya devam etsin.
        if (playerHealth != null && playerHealth.isStunned)
        {
            currentMoveInput = Vector2.zero;
            smoothedMoveInput = Vector2.zero;
            HandleGravity();
            controller.Move(new Vector3(0f, velocity.y, 0f) * Time.deltaTime);
            return;
        }

        if (inputActions != null)
        {
            isSprinting = inputActions.Player.Sprint.IsPressed();
            
            bool crouchInput = inputActions.Player.Crouch.IsPressed();
            if (crouchInput && !isCrouching)
            {
                isCrouching = true;
                targetHeight = crouchHeight;
            }
            else if (!crouchInput && isCrouching)
            {
                isCrouching = false;
                targetHeight = standHeight;
            }
        }

        HandleMovement();
        HandleGravity();
        HandleCrouch();
    }

    private void HandleSitLerp()
    {
        if (currentSeat == null) return;

        transform.localPosition = Vector3.Lerp(transform.localPosition, Vector3.zero, sitLerpSpeed * Time.deltaTime);

        float currentH = controller.height;
        float newH = Mathf.Lerp(currentH, sitHeight, crouchTransitionSpeed * Time.deltaTime);
        float hDiff = defaultHeight - newH;
        controller.height = newH;
        controller.center = defaultCenter - new Vector3(0f, hDiff / 2f, 0f);
    }

    public void Sit(Transform seatPoint)
    {
        if (isSitting) return;

        isSitting = true;
        currentSeat = seatPoint;
        
        localStandOffset = seatPoint.InverseTransformPoint(transform.position);
        standPosition = transform.position;

        controller.enabled = false;

        foreach (var col in allColliders) col.enabled = false;

        if (rb != null) rb.isKinematic = true;

        transform.SetParent(seatPoint);

        if (playerLook != null) playerLook.SnapToSeatLook(seatPoint);
    }

    public void StandUp()
    {
        if (!isSitting) return;

        isSitting = false;
        isStandingUp = true;
        
        standPosition = currentSeat.TransformPoint(localStandOffset);
        
        currentSeat = null;
        
        transform.SetParent(null);
        
        if (playerLook != null) playerLook.StopSnapping();
    }

    private void HandleStandUpLerp()
    {
        transform.position = Vector3.Lerp(transform.position, standPosition, sitLerpSpeed * Time.deltaTime);

        float currentH = controller.height;
        float newH = Mathf.Lerp(currentH, standHeight, crouchTransitionSpeed * Time.deltaTime);
        float hDiff = defaultHeight - newH;
        controller.height = newH;
        controller.center = defaultCenter - new Vector3(0f, hDiff / 2f, 0f);

        if (Vector3.Distance(transform.position, standPosition) < 0.05f && Mathf.Abs(newH - standHeight) < 0.05f)
        {
            isStandingUp = false;
            
            foreach (var col in allColliders) col.enabled = true;
            
            controller.enabled = true;
        }
    }

    private void HandleMovement()
    {
        currentMoveInput = inputActions.Player.Move.ReadValue<Vector2>();

        smoothedMoveInput = Vector2.SmoothDamp(
            smoothedMoveInput,
            currentMoveInput,
            ref smoothMoveVelocity,
            moveSmoothTime
        );

        float speed = CurrentSpeed;
        Vector3 moveDirection = transform.right * smoothedMoveInput.x + transform.forward * smoothedMoveInput.y;
        moveDirection *= speed;

        moveDirection.y = velocity.y;
        velocity = moveDirection;

        controller.Move(velocity * Time.deltaTime);
    }

    private void HandleGravity()
    {
        if (controller.isGrounded && velocity.y < 0f)
        {
            velocity.y = -2f;
        }
        else
        {
            velocity.y -= gravity * Time.deltaTime;
        }
    }

    private void HandleCrouch()
    {
        float currentHeight = controller.height;
        float newHeight = Mathf.Lerp(currentHeight, targetHeight, crouchTransitionSpeed * Time.deltaTime);

        float heightDifference = defaultHeight - newHeight;
        controller.height = newHeight;
        controller.center = defaultCenter - new Vector3(0f, heightDifference / 2f, 0f);
    }

    private void OnJump(InputAction.CallbackContext context)
    {
        if (controller.isGrounded && !isCrouching && !isSitting)
        {
            velocity.y = jumpForce;
        }
    }

    private void OnControllerColliderHit(ControllerColliderHit hit)
    {
        HingeDoor door = hit.collider.GetComponentInParent<HingeDoor>();
        if (door != null)
        {
            door.StopDoor();
        }

        Rigidbody body = hit.collider.attachedRigidbody;
        if (body != null && !body.isKinematic)
        {
            PickupableCarPart part = hit.collider.GetComponentInParent<PickupableCarPart>();
            FluidContainer fluid = hit.collider.GetComponentInParent<FluidContainer>();
            
            if (part != null || fluid != null)
            {
                if (hit.moveDirection.y < -0.3f) return;

                Vector3 pushDir = new Vector3(hit.moveDirection.x, 0, hit.moveDirection.z);
                body.linearVelocity = pushDir * 3f; 
            }
        }
    }
}
