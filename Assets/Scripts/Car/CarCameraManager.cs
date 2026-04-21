using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Cinemachine; // Cinemachine 3.x versiyonu için (Görselinizden v3 kullandığınızı anlıyorum)

public class CarCameraManager : MonoBehaviour
{
    [Header("Gereksinim: 3 Farklı Cinemachine Kamerası")]
    public CinemachineCamera firstPersonCam;
    public CinemachineCamera thirdPersonCam;
    public CinemachineCamera hoodCam;

    private CarController carController;
    private int currentCamIndex = 0;
    private bool wasDriving = false;
    private InputAction cameraAction;

    private void Awake()
    {
        carController = GetComponentInParent<CarController>();
        if (carController == null) carController = GetComponentInChildren<CarController>();

        PlayerInput playerInput = FindAnyObjectByType<PlayerInput>();
        if (playerInput != null && playerInput.actions != null)
        {
            InputActionMap drivingMap = playerInput.actions.FindActionMap("Driving");
            if (drivingMap != null)
            {
                cameraAction = drivingMap.FindAction("Camera", true);
            }
        }

        if (cameraAction == null)
        {
            InputActionAsset[] allAssets = Resources.FindObjectsOfTypeAll<InputActionAsset>();
            foreach (var asset in allAssets)
            {
                InputActionMap drivingMap = asset.FindActionMap("Driving");
                if (drivingMap != null)
                {
                    cameraAction = drivingMap.FindAction("Camera", true);
                    if (cameraAction != null) break;
                }
            }
        }
    }

    private void OnEnable()
    {
        if (cameraAction != null) cameraAction.Enable();
    }

    private void OnDisable()
    {
        if (cameraAction != null) cameraAction.Disable();
    }

    [Header("Müthiş Hız Hissiyatı (Speed FOV)")]
    public float maxExtraFOV = 20f;
    public float speedForMaxFOV = 120f;
    private float[] baseFOVs = new float[3];

    private void Start()
    {
        if (firstPersonCam) baseFOVs[0] = firstPersonCam.Lens.FieldOfView;
        if (thirdPersonCam) baseFOVs[1] = thirdPersonCam.Lens.FieldOfView;
        if (hoodCam) baseFOVs[2] = hoodCam.Lens.FieldOfView;
    }

    private void Update()
    {
        bool isDriving = (carController != null && carController.isHandsOnWheel);

        if (isDriving)
        {
            if (!wasDriving)
            {
                // Arabaya yeni bindiysek ilk kameraya (FPS) resetleyelim
                currentCamIndex = 0;
                UpdateTargetCamera();
            }

            bool pressed = (cameraAction != null && cameraAction.WasPressedThisFrame()) || (Keyboard.current != null && Keyboard.current.vKey.wasPressedThisFrame);

            if (pressed)
            {
                currentCamIndex++;
                if (currentCamIndex > 2) currentCamIndex = 0;
                UpdateTargetCamera();
            }

            ApplyAdaptiveFOV();
        }
        else if (wasDriving)
        {
            // Arabadan indiğimizde otomatik FPS kameramıza dönelim ve FOV sıfırlansın
            currentCamIndex = 0;
            UpdateTargetCamera();
            ResetAllFOVs();
        }

        wasDriving = isDriving;
    }

    private void UpdateTargetCamera()
    {
        // Öncelik yerine direkt objeleri açıp kapatarak çalışmayı %100 garanti altına alıyoruz.
        if (firstPersonCam) firstPersonCam.gameObject.SetActive(currentCamIndex == 0);
        if (thirdPersonCam) thirdPersonCam.gameObject.SetActive(currentCamIndex == 1);
        if (hoodCam) hoodCam.gameObject.SetActive(currentCamIndex == 2);
    }

    private void ApplyAdaptiveFOV()
    {
        if (carController == null) return;
        
        float currentSpeed = carController.DisplaySpeed;
        float speedRatio = Mathf.Clamp01(currentSpeed / speedForMaxFOV);
        float extraFov = speedRatio * maxExtraFOV;

        if (currentCamIndex == 0 && firstPersonCam != null)
        {
            var lens = firstPersonCam.Lens;
            lens.FieldOfView = Mathf.Lerp(lens.FieldOfView, baseFOVs[0] + extraFov * 0.5f, Time.deltaTime * 5f);
            firstPersonCam.Lens = lens;
        }
        else if (currentCamIndex == 1 && thirdPersonCam != null)
        {
            var lens = thirdPersonCam.Lens;
            lens.FieldOfView = Mathf.Lerp(lens.FieldOfView, baseFOVs[1] + extraFov, Time.deltaTime * 5f);
            thirdPersonCam.Lens = lens;
        }
        else if (currentCamIndex == 2 && hoodCam != null)
        {
            var lens = hoodCam.Lens;
            lens.FieldOfView = Mathf.Lerp(lens.FieldOfView, baseFOVs[2] + extraFov * 0.8f, Time.deltaTime * 5f);
            hoodCam.Lens = lens;
        }
    }

    private void ResetAllFOVs()
    {
        if (firstPersonCam) { var l = firstPersonCam.Lens; l.FieldOfView = baseFOVs[0]; firstPersonCam.Lens = l; }
        if (thirdPersonCam) { var l = thirdPersonCam.Lens; l.FieldOfView = baseFOVs[1]; thirdPersonCam.Lens = l; }
        if (hoodCam) { var l = hoodCam.Lens; l.FieldOfView = baseFOVs[2]; hoodCam.Lens = l; }
    }
}
