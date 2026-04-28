using System;
using UnityEngine;
using UnityEngine.InputSystem;

public partial class CarController : MonoBehaviour
{
    private InputAction moveAction;
    private InputAction handbrakeAction;
    private InputAction headlightsAction;

    [Header("--- Işıklar ---")]
    public GameObject handbrakeLight;
    public GameObject[] brakeLights;
    public GameObject[] headlights;
    private bool areHeadlightsOn = false;

    [Header("--- Fiziksel Tekerlekler ---")]
    public WheelCollider frontLeftWC;
    public WheelCollider frontRightWC;
    public WheelCollider rearLeftWC;
    public WheelCollider rearRightWC;

    [Header("--- Görsel Tekerlekler ---")]
    public Transform frontLeftMesh;
    public Transform frontRightMesh;
    public Transform rearLeftMesh;
    public Transform rearRightMesh;

    [Header("--- Araç Ayarları ---")]
    public float motorTorque = 1500f;
    public float brakeTorque = 3000f;
    public float maxSteeringAngle = 35f;
    public float maxSpeedForward = 220f;
    public float maxSpeedReverse = 20f;
    public Transform centerOfMass;

    [Header("--- Direksiyon ---")]
    public Transform steeringWheelMesh;
    public float steeringSmoothness = 5f;
    public float autoCenterSpeed = 10f;
    private float currentSteeringRot = 0f;
    private float currentSteerAngle = 0f;

    [Header("--- El Freni ---")]
    public Transform handbrakeMesh;
    public float handbrakeDuration = 0.5f;
    private bool isHandbrakeEngaged = true;
    private float currentHandbrakeRot = -30f;
    private float targetHandbrakeRot = -30f;

    [Header("--- Pedallar ---")]
    public Transform clutchPedalMesh;
    public Transform brakePedalMesh;
    public Transform gasPedalMesh;
    public float pedalSmoothness = 10f;

    [Header("--- Vites Topuzu ---")]
    public Transform gearShiftMesh;
    public float gearShiftSmoothness = 15f;
    public Vector3 neutralRot = Vector3.zero;
    public Vector3 reverseRot = new Vector3(340f, 24f, 351f);
    public Vector3 gear1Rot = new Vector3(20f, 336f, 351f);
    public Vector3 gear2Rot = new Vector3(-20f, 336f, 351f);
    public Vector3 gear3Rot = new Vector3(20f, 0f, 0f);
    public Vector3 gear4Rot = new Vector3(-20f, 0f, 0f);
    public Vector3 gear5Rot = new Vector3(20f, 24f, 351f);

    [ContextMenu("Vites Rotasyonlarını Otomatik Doldur")]
    private void FillGearRotations()
    {
        gearShiftSmoothness = 15f;
        neutralRot = Vector3.zero;
        reverseRot = new Vector3(340f, 24f, 351f);
        gear1Rot = new Vector3(20f, 336f, 351f);
        gear2Rot = new Vector3(-20f, 336f, 351f);
        gear3Rot = new Vector3(20f, 0f, 0f);
        gear4Rot = new Vector3(-20f, 0f, 0f);
        gear5Rot = new Vector3(20f, 24f, 351f);
    }

    [Header("--- Gövde Salınımı (Ağırlık Transferi) ---")]
    public Transform carBody;
    public float bodyPitchMultiplier = 2f;
    public float bodyRollMultiplier = 1.5f;
    public float bodySmoothness = 5f;
    private float targetBodyPitch = 0f;
    private float targetBodyRoll = 0f;
    private float currentGasRot = 0f;
    private float currentBrakeRot = 0f;
    private float currentClutchRot = 0f;
    public bool isHandsOnWheel = false;

    [Header("--- HUD & Analog Göstergeler ---")]
    public bool showHUD = true;
    
    [Header("Hız Kadranı")]
    public Transform speedometerNeedle;
    public AnimationCurve speedometerCurve = AnimationCurve.Linear(0f, 55f, 220f, 306f);

    [Header("Benzin Kadranı")]
    public Transform fuelNeedle;
    public float fuelEmptyAngle = 384f;
    public float fuelFullAngle = 336f;

    [Header("Akü Kadranı")]
    public Transform batteryNeedle;
    public float batteryEmptyAngle = 139f;
    public float batteryFullAngle = 220f;

    [Header("Hararet (Su) Kadranı")]
    public Transform waterNeedle;
    public float waterEmptyAngle = 151f;
    public float waterFullAngle = 208f;
    
    private float displaySpeed = 0f;

    [Header("--- Araç Sarf Malzemeleri (Tüketim) ---")]
    public float maxBatteryPercent = 100f;
    [HideInInspector] public float currentBatteryPercent;
    public float consumptionMultiplier = 1f;

    private CarStartSystem carStartSystem;

    public float currentFuelLiters => carStartSystem?.GetTank(FluidType.Gasoline)?.CurrentFluid ?? 0f;
    public float maxFuelLiters => carStartSystem?.GetTank(FluidType.Gasoline)?.MaxCapacity ?? 40f;

    public float currentMotorOilLiters => carStartSystem?.GetTank(FluidType.MotorOil)?.CurrentFluid ?? 0f;
    public float maxMotorOilLiters => carStartSystem?.GetTank(FluidType.MotorOil)?.MaxCapacity ?? 10f;

    public float currentCoolingWaterLiters => carStartSystem?.GetTank(FluidType.Coolant)?.CurrentFluid ?? 0f;
    public float maxCoolingWaterLiters => carStartSystem?.GetTank(FluidType.Coolant)?.MaxCapacity ?? 5f;

    [Header("--- Uyarı Işıkları (Gösterge Paneli) ---")]
    public GameObject fuelWarningLight;
    public GameObject batteryWarningLight;
    public GameObject oilWarningLight;
    public GameObject waterWarningLight;

    public float fuelWarningThreshold = 5f;
    public float batteryWarningThreshold = 20f;
    public float oilWarningThreshold = 1f;
    public float waterWarningThreshold = 0.5f;
    public float warningBlinkSpeed = 4f;

    [Header("--- Vites ve Devir Hissiyatı ---")]
    public float gearShiftDelay = 0.3f;
    public float gearShiftJoltForce = 4f;
    private float currentShiftTimer = 0f;
    private string displayGear = "N";
    private string previousGear = "N";
    private int currentGearInt = 1;
    private float displayRPM = 0f;

    public float DisplayRPM => displayRPM;
    public float DisplaySpeed => displaySpeed;
    public bool IsHandbrakeEngaged => isHandbrakeEngaged;
    public bool AreHeadlightsOn => areHeadlightsOn;
    public string DisplayGear => displayGear;

    public event Action<bool> OnHandbrakeToggled;
    public event Action<bool> OnHeadlightsToggled;
    public event Action OnGearShifted;

    private Vector2 moveInput;
    private Rigidbody rb;
    private GUIStyle guiStyle;
    private GUIStyle guiSmallStyle;

    private string cachedSpeedText;
    private string cachedGearText;
    private string cachedRpmText;
    private string cachedFuelText;
    private string cachedBatteryText;
    private string cachedOilWaterText;
    private int lastCachedSpeed = -1;
    private string lastCachedGear;
    private int lastCachedRpm = -1;
    private float lastCachedFuel = -1f;
    private float lastCachedBattery = -1f;
    private bool lastCachedHasBattery;
    private float lastCachedOil = -1f;
    private float lastCachedWater = -1f;

    private void Awake()
    {
        moveAction = InputHelper.FindDrivingAction("move");
        handbrakeAction = InputHelper.FindDrivingAction("handbrake");
        headlightsAction = InputHelper.FindDrivingAction("headlights");
    }

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        if (centerOfMass != null && rb != null)
        {
            rb.centerOfMass = centerOfMass.localPosition;
        }
        
        carStartSystem = GetComponentInParent<CarStartSystem>();
        if (carStartSystem == null) carStartSystem = GetComponentInChildren<CarStartSystem>();

        currentBatteryPercent = maxBatteryPercent;

        guiStyle = new GUIStyle();
        guiStyle.fontSize = 20;
        guiStyle.fontStyle = FontStyle.Bold;

        guiSmallStyle = new GUIStyle(guiStyle);
        guiSmallStyle.fontSize = 16;
        guiSmallStyle.fontStyle = FontStyle.Normal;
    }


    private void Update()
    {
        // DEV CHEAT: Cursor kilitli olduğu için F12 tuşuna atandı!
        if (Keyboard.current != null && Keyboard.current.f12Key.wasPressedThisFrame)
        {
            if (carStartSystem != null) carStartSystem.DevQuickStart();
            currentBatteryPercent = maxBatteryPercent;
            Debug.Log("[DEV CHEAT] Araba parçaları ve depolar fullendi! Artık arabaya binip çalıştırabilirsiniz.");
        }

        bool isShiftPressed = false;

        if (isHandsOnWheel)
        {
            isShiftPressed = Keyboard.current != null && Keyboard.current.shiftKey.isPressed;
            if (moveAction != null) moveInput = moveAction.ReadValue<Vector2>();

            if (carStartSystem != null)
            {
                if (!carStartSystem.IsRunning)
                {
                    if (isShiftPressed && moveInput.y > 0.5f)
                    {
                        carStartSystem.TryStart();
                    }
                }
                else
                {
                    if (isShiftPressed && moveInput.y < -0.5f)
                    {
                        carStartSystem.StopEngine();
                    }
                }
            }
        }
        else
        {
            moveInput = Vector2.zero;
        }

        AnimateInteriors(isShiftPressed);
        UpdateWheelVisuals();
        UpdateBrakeLights();
        UpdateWarningLights();
    }

}
