using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// FPS kamera bakış kontrolcüsü.
/// Mouse ile yatay/dikey bakış açısını yönetir.
/// Yatay döndürme player objesine, dikey döndürme kameraya uygulanır.
/// </summary>
public class PlayerLook : MonoBehaviour
{
    #region Settings

    [Header("Bakış Ayarları")]
    [Tooltip("Mouse hassasiyeti")]
    [SerializeField] private float mouseSensitivity = 2f;

    [Tooltip("Yukarı/aşağı bakma açı limiti (derece)")]
    [SerializeField] private float maxLookAngle = 80f;

    [Tooltip("Bakış için kullanılacak kamera (boş bırakılırsa child'dan otomatik bulunur)")]
    [SerializeField] private Transform cameraTransform;

    [Header("Zoom Ayarları")]
    [Tooltip("Zoom aktifken hedeflenen FOV değeri")]
    [SerializeField] private float zoomFOV = 40f;
    [Tooltip("Zoom geçiş hızı")]
    [SerializeField] private float zoomSpeed = 10f;

    #endregion

    // Input referansı
    private PlayerInputActions inputActions;

    // Dahili durum
    private float verticalRotation = 0f;
    private bool isSnappingToSeat = false;
    private Quaternion targetPlayerRotation;
    private Quaternion targetCameraRotation;
    private Camera playerCamera;
    private float defaultFOV;
    private bool isZooming = false;

    private void Awake()
    {
        inputActions = new PlayerInputActions();

        // Kamera referansı atanmadıysa child'dan bul
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
            if (playerCamera != null)
            {
                defaultFOV = playerCamera.fieldOfView;
            }
        }
    }

    private void OnEnable()
    {
        inputActions.Player.Enable();

        inputActions.Player.Zoom.started += OnZoomStarted;
        inputActions.Player.Zoom.canceled += OnZoomCanceled;

        // İmleci kilitle ve gizle
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void OnDisable()
    {
        inputActions.Player.Zoom.started -= OnZoomStarted;
        inputActions.Player.Zoom.canceled -= OnZoomCanceled;

        inputActions.Player.Disable();

        // İmleci serbest bırak
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    private void OnDestroy()
    {
        inputActions?.Dispose();
    }

    private void LateUpdate()
    {
        HandleLook();
        HandleZoom();
    }

    private void HandleZoom()
    {
        if (playerCamera == null) return;
        
        float targetFOV = isZooming ? zoomFOV : defaultFOV;
        playerCamera.fieldOfView = Mathf.Lerp(playerCamera.fieldOfView, targetFOV, zoomSpeed * Time.deltaTime);
    }

    private void OnZoomStarted(InputAction.CallbackContext context)
    {
        isZooming = true;
    }

    private void OnZoomCanceled(InputAction.CallbackContext context)
    {
        isZooming = false;
    }

    public void SnapToSeatLook(Transform seatPoint)
    {
        Vector3 flatForward = seatPoint.forward;
        flatForward.y = 0f;
        if (flatForward.sqrMagnitude > 0.001f)
            targetPlayerRotation = Quaternion.LookRotation(flatForward);
        else
            targetPlayerRotation = transform.rotation;

        targetCameraRotation = Quaternion.Euler(0f, 0f, 0f); // İleri bak
        isSnappingToSeat = true;
    }

    public void StopSnapping()
    {
        isSnappingToSeat = false;
    }

    /// <summary>
    /// Mouse girdisine göre bakış açısını günceller veya oturma eylemi sırasında pürüzsüz kamera geçişi yapar.
    /// </summary>
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
                isSnappingToSeat = false; // Geçiş bitti, serbest bırak
            }
            return;
        }

        // Look action'dan mouse delta'sını al
        Vector2 lookInput = inputActions.Player.Look.ReadValue<Vector2>();

        // Yatay döndürme (player objesini Y ekseninde döndür)
        float horizontalRotation = lookInput.x * mouseSensitivity;
        transform.Rotate(Vector3.up * horizontalRotation);

        // Dikey döndürme (kamerayı X ekseninde döndür, sınırlı)
        verticalRotation -= lookInput.y * mouseSensitivity;
        verticalRotation = Mathf.Clamp(verticalRotation, -maxLookAngle, maxLookAngle);
        cameraTransform.localRotation = Quaternion.Euler(verticalRotation, 0f, 0f);
    }
}
