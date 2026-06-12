using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Cinemachine;

public class CarCameraManager : MonoBehaviour
{
    [Header("Cinemachine Kameraları")]
    public CinemachineCamera firstPersonCam;
    public CinemachineCamera thirdPersonCam;
    public CinemachineCamera hoodCam;

    [Header("Müthiş Hız Hissiyatı (Speed FOV)")]
    public float maxExtraFOV = 20f;
    public float speedForMaxFOV = 120f;
    [SerializeField] private float fovLerpSpeed = 5f;
    [SerializeField] private float fpsFovScale = 0.5f;
    [SerializeField] private float hoodFovScale = 0.8f;

    [Header("Scroll & TPS Zoom")]
    [SerializeField] private float scrollZoomSpeed = 0.01f;
    [SerializeField] private float minTpsDistance = 2.5f;
    [SerializeField] private float maxTpsDistance = 12f;

    private CarController carController;
    private int currentCamIndex = 0;
    private bool wasDriving = false;
    private InputAction cameraAction;

    private CinemachineCamera[] cameras;
    private float[] baseFOVs;
    private float[] fovScales;

    // TPS Zoom references
    private CinemachineThirdPersonFollow tpsFollow;
    private CinemachineOrbitalFollow orbitalFollow;
    private float currentTpsDistance = 5f;

    private void Awake()
    {
        carController = GetComponentInParent<CarController>();
        if (carController == null) carController = GetComponentInChildren<CarController>();

        cameraAction = InputHelper.FindDrivingAction("Camera", true);
    }

    private void OnEnable()
    {
        if (cameraAction != null) cameraAction.Enable();
    }

    private void OnDisable()
    {
        if (cameraAction != null) cameraAction.Disable();
    }

    private void Start()
    {
        cameras = new[] { firstPersonCam, thirdPersonCam, hoodCam };
        baseFOVs = new float[cameras.Length];
        fovScales = new[] { fpsFovScale, 1f, hoodFovScale };

        for (int i = 0; i < cameras.Length; i++)
        {
            if (cameras[i] != null)
                baseFOVs[i] = cameras[i].Lens.FieldOfView;
        }

        // Initialize Zoom Components
        if (thirdPersonCam != null)
        {
            tpsFollow = thirdPersonCam.GetComponent<CinemachineThirdPersonFollow>();
            orbitalFollow = thirdPersonCam.GetComponent<CinemachineOrbitalFollow>();

            if (tpsFollow != null) currentTpsDistance = tpsFollow.CameraDistance;
            else if (orbitalFollow != null) currentTpsDistance = orbitalFollow.Radius;
        }
    }

    private void Update()
    {
        bool isDriving = (carController != null && carController.isHandsOnWheel);

        if (isDriving)
        {
            if (!wasDriving)
            {
                currentCamIndex = 0;
                UpdateTargetCamera();
            }

            bool pressed = (cameraAction != null && cameraAction.WasPressedThisFrame());

            if (pressed)
            {
                currentCamIndex++;
                if (currentCamIndex >= cameras.Length) currentCamIndex = 0;
                UpdateTargetCamera();
            }

            HandleMouseScrollZoom();
            ApplyAdaptiveFOV();
        }
        else if (wasDriving)
        {
            currentCamIndex = 0;
            UpdateTargetCamera();
            ResetAllFOVs();
        }

        wasDriving = isDriving;
    }

    private void HandleMouseScrollZoom()
    {
        if (Mouse.current == null) return;
        
        float scroll = Mouse.current.scroll.ReadValue().y;
        if (Mathf.Abs(scroll) < 0.1f) return;

        // FPS (0) -> Scroll out (scroll < 0) -> Switch to TPS (1)
        if (currentCamIndex == 0 && scroll < 0f)
        {
            currentCamIndex = 1;
            currentTpsDistance = minTpsDistance;
            ApplyDistanceToTps();
            UpdateTargetCamera();
        }
        // TPS (1) -> Zoom logic
        else if (currentCamIndex == 1)
        {
            // Input System scroll delta is usually 120 or -120 per notch.
            currentTpsDistance -= scroll * scrollZoomSpeed;

            if (currentTpsDistance <= minTpsDistance)
            {
                currentTpsDistance = minTpsDistance;
                // Switch back to FPS
                currentCamIndex = 0;
                UpdateTargetCamera();
            }
            else if (currentTpsDistance > maxTpsDistance)
            {
                currentTpsDistance = maxTpsDistance;
            }

            ApplyDistanceToTps();
        }
    }

    private void ApplyDistanceToTps()
    {
        if (tpsFollow != null) tpsFollow.CameraDistance = currentTpsDistance;
        else if (orbitalFollow != null) orbitalFollow.Radius = currentTpsDistance;
    }

    private void UpdateTargetCamera()
    {
        for (int i = 0; i < cameras.Length; i++)
        {
            if (cameras[i] != null)
                cameras[i].gameObject.SetActive(i == currentCamIndex);
        }
    }

    private void ApplyAdaptiveFOV()
    {
        if (carController == null) return;
        if (currentCamIndex < 0 || currentCamIndex >= cameras.Length) return;

        CinemachineCamera cam = cameras[currentCamIndex];
        if (cam == null) return;

        float speedRatio = Mathf.Clamp01(carController.DisplaySpeed / speedForMaxFOV);
        float extraFov = speedRatio * maxExtraFOV * fovScales[currentCamIndex];

        var lens = cam.Lens;
        lens.FieldOfView = Mathf.Lerp(lens.FieldOfView, baseFOVs[currentCamIndex] + extraFov, Time.deltaTime * fovLerpSpeed);
        cam.Lens = lens;
    }

    private void ResetAllFOVs()
    {
        for (int i = 0; i < cameras.Length; i++)
        {
            if (cameras[i] == null) continue;
            var lens = cameras[i].Lens;
            lens.FieldOfView = baseFOVs[i];
            cameras[i].Lens = lens;
        }
    }
}