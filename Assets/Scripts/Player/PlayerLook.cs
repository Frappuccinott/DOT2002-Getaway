using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Cinemachine;

public class PlayerLook : MonoBehaviour
{
    [Header("Bakış Ayarları")]
    [SerializeField] private float mouseSensitivity = 2f;
    [SerializeField] private float maxLookAngle = 80f;
    [SerializeField] private Transform cameraTransform;

    [Header("Zoom Ayarları")]
    [SerializeField] private float zoomFOV = 40f;
    [SerializeField] private float zoomSpeed = 10f;

    private PlayerInputActions inputActions;

    private float verticalRotation = 0f;
    private bool isSnappingToSeat = false;
    private Quaternion targetPlayerRotation;
    private Quaternion targetCameraRotation;
    private Camera playerCamera;
    private CinemachineCamera cinemachineCam;
    private float defaultFOV;
    private bool isZooming = false;
    private PlayerController playerController;

    private void Awake()
    {
        playerController = GetComponent<PlayerController>();

        if (cameraTransform == null)
        {
            Camera cam = GetComponentInChildren<Camera>();
            if (cam != null)
            {
                cameraTransform = cam.transform;
            }
            else
            {
                Debug.LogError("[PlayerLook] Kamera bulunamadı! Lütfen Inspector'dan atayın veya child olarak ekleyin.");
            }
        }

        if (cameraTransform != null)
        {
            playerCamera = cameraTransform.GetComponent<Camera>();
            cinemachineCam = cameraTransform.GetComponent<CinemachineCamera>();

            if (playerCamera != null)
            {
                defaultFOV = playerCamera.fieldOfView;
            }
            else if (cinemachineCam != null)
            {
                defaultFOV = cinemachineCam.Lens.FieldOfView;
            }
        }
    }

    private void OnEnable()
    {
        inputActions = PlayerInputProvider.Actions;
        if (inputActions == null) return;
        inputActions.Player.Enable();

        inputActions.Player.Zoom.started += OnZoomStarted;
        inputActions.Player.Zoom.canceled += OnZoomCanceled;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void OnDisable()
    {
        if (inputActions == null) return;
        inputActions.Player.Zoom.started -= OnZoomStarted;
        inputActions.Player.Zoom.canceled -= OnZoomCanceled;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }



    private void LateUpdate()
    {
        HandleLook();
        HandleZoom();
        HandleCameraHeight();
    }

    private void HandleZoom()
    {
        float targetFOV = isZooming ? zoomFOV : defaultFOV;
        
        if (playerCamera != null)
        {
            playerCamera.fieldOfView = Mathf.Lerp(playerCamera.fieldOfView, targetFOV, zoomSpeed * Time.deltaTime);
        }
        else if (cinemachineCam != null)
        {
            var lens = cinemachineCam.Lens;
            lens.FieldOfView = Mathf.Lerp(lens.FieldOfView, targetFOV, zoomSpeed * Time.deltaTime);
            cinemachineCam.Lens = lens;
        }
    }

    private void OnZoomStarted(InputAction.CallbackContext context)
    {
        isZooming = true;
    }

    private void OnZoomCanceled(InputAction.CallbackContext context)
    {
        isZooming = false;
    }

    private float defaultCameraY;
    
    private void Start()
    {
        if (cameraTransform != null) defaultCameraY = cameraTransform.localPosition.y;
    }

    private void HandleCameraHeight()
    {
        if (playerController != null && cameraTransform != null)
        {
            // Eğer koltuğa oturuyorsak kamerayı indirmemize gerek yok
            if (isSnappingToSeat || playerController.IsSitting) return;

            // Karakter 2m'den 1m'ye düştüğü için kamera tam 1 birim aşağı inmeli.
            float targetY = defaultCameraY - (playerController.CrouchRatio * 1.0f);
            
            Vector3 localPos = cameraTransform.localPosition;
            localPos.y = Mathf.Lerp(localPos.y, targetY, 10f * Time.deltaTime);
            cameraTransform.localPosition = localPos;
        }
    }

    public void SnapToSeatLook(Transform seatPoint)
    {
        Vector3 flatForward = seatPoint.forward;
        flatForward.y = 0f;
        if (flatForward.sqrMagnitude > 0.001f)
            targetPlayerRotation = Quaternion.LookRotation(flatForward);
        else
            targetPlayerRotation = transform.rotation;

        targetCameraRotation = Quaternion.Euler(0f, 0f, 0f);
        isSnappingToSeat = true;
    }

    public void StopSnapping()
    {
        isSnappingToSeat = false;

        if (cameraTransform != null)
        {
            cameraTransform.localRotation = Quaternion.Euler(verticalRotation, 0f, 0f);
        }
    }
    private void HandleLook()
    {
        if (cameraTransform == null) return;

        if (isSnappingToSeat)
        {
            transform.rotation = Quaternion.Slerp(transform.rotation, targetPlayerRotation, 10f * Time.deltaTime);
            cameraTransform.localRotation = Quaternion.Slerp(cameraTransform.localRotation, targetCameraRotation, 10f * Time.deltaTime);
            verticalRotation = Mathf.Lerp(verticalRotation, 0f, 10f * Time.deltaTime);

            if (Quaternion.Angle(transform.rotation, targetPlayerRotation) < 1f &&
                Quaternion.Angle(cameraTransform.localRotation, targetCameraRotation) < 1f)
            {
                isSnappingToSeat = false;
            }
            return;
        }

        Vector2 lookInput = inputActions.Player.Look.ReadValue<Vector2>();
        float horizontalRotation = lookInput.x * mouseSensitivity;

        if (playerController != null && playerController.IsSitting)
        {
            // Arabada otururken: Karakterin gövdesini döndürme, sadece kafayı (kamerayı) olduğu yerde sağa/sola ve yukarı/aşağı çevir.
            verticalRotation -= lookInput.y * mouseSensitivity;
            verticalRotation = Mathf.Clamp(verticalRotation, -maxLookAngle, maxLookAngle);
            
            float targetY = cameraTransform.localEulerAngles.y + horizontalRotation;
            cameraTransform.localRotation = Quaternion.Euler(verticalRotation, targetY, 0f);
        }
        else
        {
            // Ayaktayken: Klasik FPS (Gövde sağa-sola, kafa aşağı-yukarı)
            transform.Rotate(Vector3.up * horizontalRotation);
            
            verticalRotation -= lookInput.y * mouseSensitivity;
            verticalRotation = Mathf.Clamp(verticalRotation, -maxLookAngle, maxLookAngle);
            cameraTransform.localRotation = Quaternion.Euler(verticalRotation, 0f, 0f);
        }
    }
}
