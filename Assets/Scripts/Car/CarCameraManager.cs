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

    private CarController carController;
    private int currentCamIndex = 0;
    private bool wasDriving = false;
    private InputAction cameraAction;

    private CinemachineCamera[] cameras;
    private float[] baseFOVs;
    private float[] fovScales;

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

            bool pressed = (cameraAction != null && cameraAction.WasPressedThisFrame()) || (Keyboard.current != null && Keyboard.current.vKey.wasPressedThisFrame);

            if (pressed)
            {
                currentCamIndex++;
                if (currentCamIndex >= cameras.Length) currentCamIndex = 0;
                UpdateTargetCamera();
            }

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
