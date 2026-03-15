using UnityEngine;
using UnityEngine.InputSystem;

public class CarController : MonoBehaviour
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
    public float batteryEmptyAngle = 180f;
    public float batteryFullAngle = 0f;

    [Header("Hararet (Su) Kadranı")]
    public Transform waterNeedle; // Hararet (Su) ibresi
    public float waterEmptyAngle = 180f; // 0 Litre Su (Aşırı Sıcak)
    public float waterFullAngle = 0f;    // 5 Litre Su (Soğuk/Normal)
    
    private float displaySpeed = 0f;

    [Header("--- Araç Sarf Malzemeleri (Tüketim) ---")]
    public float maxBatteryPercent = 100f;
    [HideInInspector] public float currentBatteryPercent;
    public float consumptionMultiplier = 1f;

    private CarStartSystem carStartSystem;

    // Gerçek Tanklardan Dinamik Okuma
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

    private Vector2 moveInput;
    private Rigidbody rb;
    private GUIStyle guiStyle;
    private GUIStyle guiSmallStyle;

    private void Awake()
    {
        PlayerInput playerInput = FindAnyObjectByType<PlayerInput>();
        if (playerInput != null && playerInput.actions != null)
        {
            InputActionMap drivingMap = playerInput.actions.FindActionMap("Driving");
            if (drivingMap != null)
            {
                moveAction = drivingMap.FindAction("move");
                handbrakeAction = drivingMap.FindAction("handbrake");
                headlightsAction = drivingMap.FindAction("headlights");
            }
        }
        else
        {
            InputActionAsset[] allAssets = Resources.FindObjectsOfTypeAll<InputActionAsset>();
            foreach (var asset in allAssets)
            {
                InputActionMap drivingMap = asset.FindActionMap("Driving");
                if (drivingMap != null)
                {
                    moveAction = drivingMap.FindAction("move");
                    handbrakeAction = drivingMap.FindAction("handbrake");
                    headlightsAction = drivingMap.FindAction("headlights");
                    break;
                }
            }
        }
    }

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        if (centerOfMass != null && rb != null)
        {
            rb.centerOfMass = centerOfMass.localPosition;
        }
        
        if (handbrakeLight != null)
        {
            handbrakeLight.SetActive(isHandbrakeEngaged);
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

    private void OnEnable()
    {
        if (moveAction != null) moveAction.Enable();
        if (handbrakeAction != null)
        {
            handbrakeAction.Enable();
            handbrakeAction.performed += ToggleHandbrake;
        }
        if (headlightsAction != null)
        {
            headlightsAction.Enable();
            headlightsAction.performed += ToggleHeadlights;
        }
    }

    private void OnDisable()
    {
        if (moveAction != null) moveAction.Disable();
        if (handbrakeAction != null)
        {
            handbrakeAction.Disable();
            handbrakeAction.performed -= ToggleHandbrake;
        }
        if (headlightsAction != null)
        {
            headlightsAction.Disable();
            headlightsAction.performed -= ToggleHeadlights;
        }
    }

    private void ToggleHandbrake(InputAction.CallbackContext context)
    {
        if (!isHandsOnWheel) return;

        isHandbrakeEngaged = !isHandbrakeEngaged;
        targetHandbrakeRot = isHandbrakeEngaged ? -30f : 10f;
        if (handbrakeLight != null && currentBatteryPercent > 0f) handbrakeLight.SetActive(isHandbrakeEngaged);
    }

    private void ToggleHeadlights(InputAction.CallbackContext context)
    {
        if (!isHandsOnWheel) return;

        if (currentBatteryPercent <= 0f) return; // Akü bittiyse far anahtarı çalışmaz

        areHeadlightsOn = !areHeadlightsOn;
        if (headlights != null)
        {
            foreach (var light in headlights) { if (light != null) light.SetActive(areHeadlightsOn); }
        }
    }

    private void Update()
    {
        bool isShiftPressed = Keyboard.current != null && Keyboard.current.shiftKey.isPressed;

        if (isHandsOnWheel)
        {
            if (moveAction != null) moveInput = moveAction.ReadValue<Vector2>();

            // Engine starting logic
            if (carStartSystem != null && !carStartSystem.IsRunning)
            {
                if (isShiftPressed && moveInput.y > 0.5f)
                {
                    carStartSystem.TryStart();
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

    private void UpdateBrakeLights()
    {
        if (brakeLights != null && brakeLights.Length > 0)
        {
            bool isBraking = (moveInput.y < 0) && (currentBatteryPercent > 0f); // Fren lambaları için elektrik şart
            foreach (var light in brakeLights)
            {
                if (light != null && light.activeSelf != isBraking) light.SetActive(isBraking);
            }
        }
    }

    private void UpdateWarningLights()
    {
        // Temel Kural: Akü takılı değilse veya %0 ise TÜM ışıklar (ve göstergeler) yanmayı bırakır (karanlığa gömülür).
        bool hasPower = currentBatteryPercent > 0f && (carStartSystem != null && carStartSystem.HasBattery);

        SetWarningLight(fuelWarningLight, currentFuelLiters, fuelWarningThreshold, hasPower);
        SetWarningLight(batteryWarningLight, currentBatteryPercent, batteryWarningThreshold, hasPower);
        SetWarningLight(oilWarningLight, currentMotorOilLiters, oilWarningThreshold, hasPower);
        SetWarningLight(waterWarningLight, currentCoolingWaterLiters, waterWarningThreshold, hasPower);
    }

    private void SetWarningLight(GameObject lightObj, float currentValue, float threshold, bool hasPower)
    {
        if (lightObj == null) return;

        // Akü bittiyse ışık tamamen kapanır.
        if (!hasPower)
        {
            if (lightObj.activeSelf) lightObj.SetActive(false);
            return;
        }

        if (currentValue <= 0f)
        {
            // Değer TAMAMEN 0 olduysa: Işık SÜREKLİ YANIK kalır (Kritik hata / Motor Durdu vb.)
            if (!lightObj.activeSelf) lightObj.SetActive(true);
        }
        else if (currentValue <= threshold)
        {
            // Değer threshold sınırından az ama 0'dan büyükse: YANIP SÖNER (Uyarı)
            bool blinkState = Mathf.PingPong(Time.time * warningBlinkSpeed, 1f) > 0.5f;
            if (lightObj.activeSelf != blinkState) lightObj.SetActive(blinkState);
        }
        else
        {
            // Değer güvenliyse: Işık KAPALI kalır
            if (lightObj.activeSelf) lightObj.SetActive(false);
        }
    }

    private void ConsumeResources(float speedKMH)
    {
        float fuelDrainRate = 0.001f + (speedKMH * 0.0002f); 
        float oilDrainRate = 0.0001f + (speedKMH * 0.00001f);
        float waterDrainRate = 0.0002f + (speedKMH * 0.00005f);

        if (carStartSystem != null)
        {
            float deltaMutliplier = consumptionMultiplier * Time.deltaTime;
            carStartSystem.GetTank(FluidType.Gasoline)?.ConsumeFluid(fuelDrainRate * deltaMutliplier);
            carStartSystem.GetTank(FluidType.MotorOil)?.ConsumeFluid(oilDrainRate * deltaMutliplier);
            carStartSystem.GetTank(FluidType.Coolant)?.ConsumeFluid(waterDrainRate * deltaMutliplier);
        }
        
        if (carStartSystem != null && carStartSystem.HasBattery)
        {
            float batteryDrainRate = 0.005f + (areHeadlightsOn ? 0.05f : 0f);
            currentBatteryPercent -= batteryDrainRate * consumptionMultiplier * Time.deltaTime;
            currentBatteryPercent = Mathf.Max(0f, currentBatteryPercent);
        }

        // Akü sıfırlandığı an, açık unutulmuş/açık kalmış farları ve el freni lambasını da anında söndürürüz
        if (currentBatteryPercent <= 0f || (carStartSystem != null && !carStartSystem.HasBattery))
        {
            if (areHeadlightsOn)
            {
                areHeadlightsOn = false;
                if (headlights != null)
                {
                    foreach (var light in headlights) { if (light != null) light.SetActive(false); }
                }
            }
            if (handbrakeLight != null && handbrakeLight.activeSelf)
            {
                handbrakeLight.SetActive(false);
            }
        }
    }

    private void FixedUpdate()
    {
        ApplyPhysics();
    }

    private void ApplyPhysics()
    {
        float speedKMH = rb.linearVelocity.magnitude * 3.6f;
        float forwardDot = Vector3.Dot(transform.forward, rb.linearVelocity);

        if (currentShiftTimer > 0) currentShiftTimer -= Time.deltaTime;

        float targetSteerAngle = moveInput.x * maxSteeringAngle;
        
        if (Mathf.Abs(moveInput.x) < 0.1f && speedKMH > 1f)
            currentSteerAngle = Mathf.MoveTowards(currentSteerAngle, 0f, Time.deltaTime * autoCenterSpeed);
        else
            currentSteerAngle = Mathf.Lerp(currentSteerAngle, targetSteerAngle, Time.deltaTime * steeringSmoothness);
        
        frontLeftWC.steerAngle = currentSteerAngle;
        frontRightWC.steerAngle = currentSteerAngle;

        float acceleration = moveInput.y;
        bool isMovingForward = forwardDot > -0.5f;
        
        if (isHandbrakeEngaged)
        {
            rearLeftWC.motorTorque = 0f;
            rearRightWC.motorTorque = 0f;
            rearLeftWC.brakeTorque = brakeTorque;
            rearRightWC.brakeTorque = brakeTorque;
        }
        else
        {
            if (isMovingForward && acceleration > 0 && speedKMH >= maxSpeedForward)
            {
                rearLeftWC.motorTorque = 0f;
                rearRightWC.motorTorque = 0f;
            }
            else if (!isMovingForward && acceleration < 0 && speedKMH >= maxSpeedReverse)
            {
                rearLeftWC.motorTorque = 0f;
                rearRightWC.motorTorque = 0f;
            }
            else if (currentShiftTimer > 0)
            {
                rearLeftWC.motorTorque = 0f;
                rearRightWC.motorTorque = 0f;
            }
            else if (currentFuelLiters <= 0f || currentBatteryPercent <= 0f || (carStartSystem != null && !carStartSystem.IsRunning))
            {
                rearLeftWC.motorTorque = 0f;
                rearRightWC.motorTorque = 0f;
                rearLeftWC.brakeTorque = brakeTorque * 0.5f;
                rearRightWC.brakeTorque = brakeTorque * 0.5f;
                displayRPM = Mathf.Lerp(displayRPM, 0f, Time.deltaTime * 2f);
            }
            else
            {
                rearLeftWC.motorTorque = acceleration * motorTorque;
                rearRightWC.motorTorque = acceleration * motorTorque;
            }

            if (currentFuelLiters > 0f && currentBatteryPercent > 0f)
            {
                rearLeftWC.brakeTorque = 0f;
                rearRightWC.brakeTorque = 0f;
            }
        }

        displaySpeed = speedKMH;
        CalculateHUDData(acceleration, isMovingForward, speedKMH);
        UpdateAnalogDials();
        ConsumeResources(speedKMH);
        ApplyWeightTransfer(acceleration, targetSteerAngle);
    }

    private void UpdateAnalogDials()
    {
        if (speedometerNeedle != null)
        {
            float targetNeedleAngle = speedometerCurve.Evaluate(displaySpeed);
            float currentNeedleAngle = speedometerNeedle.localEulerAngles.z;
            float smoothZ = Mathf.LerpAngle(currentNeedleAngle, targetNeedleAngle, Time.deltaTime * 6f);
            speedometerNeedle.localRotation = Quaternion.Euler(speedometerNeedle.localEulerAngles.x, speedometerNeedle.localEulerAngles.y, smoothZ);
        }

        if (fuelNeedle != null)
        {
            float fillRatio = currentFuelLiters / maxFuelLiters;
            float targetFuelAngle = Mathf.Lerp(fuelEmptyAngle, fuelFullAngle, fillRatio);
            float currentFuelZ = fuelNeedle.localEulerAngles.z;
            float smoothZ = Mathf.LerpAngle(currentFuelZ, targetFuelAngle, Time.deltaTime * 2f);
            fuelNeedle.localRotation = Quaternion.Euler(fuelNeedle.localEulerAngles.x, fuelNeedle.localEulerAngles.y, smoothZ);
        }

        if (batteryNeedle != null)
        {
            float effectiveBattery = (carStartSystem != null && carStartSystem.HasBattery) ? currentBatteryPercent : 0f;
            float fillRatio = effectiveBattery / maxBatteryPercent;
            float targetBatteryAngle = Mathf.Lerp(batteryEmptyAngle, batteryFullAngle, fillRatio);
            float currentBatZ = batteryNeedle.localEulerAngles.z;
            float smoothZ = Mathf.LerpAngle(currentBatZ, targetBatteryAngle, Time.deltaTime * 2f);
            batteryNeedle.localRotation = Quaternion.Euler(batteryNeedle.localEulerAngles.x, batteryNeedle.localEulerAngles.y, smoothZ);
        }

        // 4. Hararet (Su) İbresini Güncelle
        if (waterNeedle != null)
        {
            float fillRatio = currentCoolingWaterLiters / maxCoolingWaterLiters;
            // Su azaldıkça ibre "Sıcak" tarafa (Empty/EmptyAngle) gidecek. Su tam doluyken "Soğuk" tarafa (FullAngle) gidecek.
            float targetWaterAngle = Mathf.Lerp(waterEmptyAngle, waterFullAngle, fillRatio);
            float currentWaterZ = waterNeedle.localEulerAngles.z;
            float smoothZ = Mathf.LerpAngle(currentWaterZ, targetWaterAngle, Time.deltaTime * 2f);
            waterNeedle.localRotation = Quaternion.Euler(waterNeedle.localEulerAngles.x, waterNeedle.localEulerAngles.y, smoothZ);
        }
    }

    private void CalculateHUDData(float acceleration, bool isMovingForward, float speedKMH)
    {
        if (speedKMH < 1f)
        {
            displayGear = (acceleration == 0) ? "N" : (acceleration > 0 ? "1" : "R");
            currentGearInt = 1;
        }
        else if (!isMovingForward)
        {
            displayGear = "R";
        }
        else
        {
            if (currentGearInt <= 1)
            {
                if (speedKMH > 35f) currentGearInt = 2;
                else currentGearInt = 1;
            }
            else if (currentGearInt == 2)
            {
                if (speedKMH > 75f) currentGearInt = 3;
                else if (speedKMH < 30f) currentGearInt = 1;
            }
            else if (currentGearInt == 3)
            {
                if (speedKMH > 120f) currentGearInt = 4;
                else if (speedKMH < 70f) currentGearInt = 2;
            }
            else if (currentGearInt == 4)
            {
                if (speedKMH > 165f) currentGearInt = 5;
                else if (speedKMH < 115f) currentGearInt = 3;
            }
            else if (currentGearInt >= 5)
            {
                if (speedKMH < 160f) currentGearInt = 4;
                else currentGearInt = 5;
            }
            displayGear = currentGearInt.ToString();
        }

        if (displayGear != previousGear && displayGear != "N")
        {
            TriggerGearShiftJolt();
        }
        previousGear = displayGear;

        if (displayGear == "N") 
        {
            displayRPM = Mathf.Lerp(displayRPM, 800f + (acceleration != 0 ? 3000f : 0f), Time.deltaTime * 5f);
        }
        else if (displayGear == "R")
        {
            displayRPM = 1000f + (speedKMH / maxSpeedReverse) * 4000f;
        }
        else
        {
            float minSpeedForGear = 0f;
            float maxSpeedForGear = 40f;

            switch (displayGear)
            {
                case "1": minSpeedForGear = 0f; maxSpeedForGear = 40f; break;
                case "2": minSpeedForGear = 40f; maxSpeedForGear = 80f; break;
                case "3": minSpeedForGear = 80f; maxSpeedForGear = 130f; break;
                case "4": minSpeedForGear = 130f; maxSpeedForGear = 180f; break;
                case "5": minSpeedForGear = 180f; maxSpeedForGear = maxSpeedForward; break;
            }

            float ratio = Mathf.Clamp01((speedKMH - minSpeedForGear) / (maxSpeedForGear - minSpeedForGear));
            displayRPM = 1000f + (ratio * 5500f);
        }
        
        if (acceleration == 0 && displayGear != "N")
        {
            displayRPM -= Time.deltaTime * 2000f;
            displayRPM = Mathf.Max(displayRPM, 800f);
        }
    }

    private void ApplyWeightTransfer(float acceleration, float steerAngle)
    {
        if (carBody != null)
        {
            float speedKMH = rb.linearVelocity.magnitude * 3.6f;

            if (currentShiftTimer > 0)
            {
                targetBodyPitch = gearShiftJoltForce;
            }
            else
            {
                if (acceleration != 0)
                {
                    targetBodyPitch = Mathf.Clamp(-acceleration * bodyPitchMultiplier, -bodyPitchMultiplier, bodyPitchMultiplier);
                }
                else 
                {
                    if (speedKMH > 5f && moveInput.y < 0) 
                        targetBodyPitch = Mathf.Clamp(brakeTorque * 0.001f * bodyPitchMultiplier, -bodyPitchMultiplier, bodyPitchMultiplier);
                    else 
                        targetBodyPitch = 0f;
                }
            }

            targetBodyRoll = Mathf.Clamp(-currentSteerAngle / maxSteeringAngle * bodyRollMultiplier * (speedKMH / 50f), -bodyRollMultiplier, bodyRollMultiplier);

            Quaternion targetRotation = Quaternion.Euler(targetBodyPitch, 0f, targetBodyRoll);
            float smoothness = (currentShiftTimer > 0) ? bodySmoothness * 3f : bodySmoothness;
            carBody.localRotation = Quaternion.Slerp(carBody.localRotation, targetRotation, Time.deltaTime * smoothness);
        }
    }

    private void TriggerGearShiftJolt()
    {
        if (displayGear != "R" && displayGear != "N")
        {
            bool isUpshift = false;
            if (int.TryParse(displayGear, out int curG) && int.TryParse(previousGear, out int prevG))
            {
                isUpshift = curG > prevG;
            }

            if (isUpshift)
            {
                float dynamicShiftDelay = displaySpeed > 70f ? gearShiftDelay * 0.2f : gearShiftDelay;
                currentShiftTimer = dynamicShiftDelay; 
                displayRPM *= 0.65f;
            }
        }
    }

    private void UpdateWheelVisuals()
    {
        UpdateSingleWheel(frontLeftWC, frontLeftMesh);
        UpdateSingleWheel(frontRightWC, frontRightMesh);
        UpdateSingleWheel(rearLeftWC, rearLeftMesh);
        UpdateSingleWheel(rearRightWC, rearRightMesh);
    }

    private void UpdateSingleWheel(WheelCollider wc, Transform mesh)
    {
        if (!mesh) return;
        wc.GetWorldPose(out Vector3 position, out Quaternion rotation);
        mesh.SetPositionAndRotation(position, rotation);
    }

    private void AnimateInteriors(bool isShiftPressed)
    {
        if (handbrakeMesh)
        {
            float speed = 40f / handbrakeDuration;
            currentHandbrakeRot = Mathf.MoveTowards(currentHandbrakeRot, targetHandbrakeRot, speed * Time.deltaTime);
            handbrakeMesh.localEulerAngles = new Vector3(currentHandbrakeRot, handbrakeMesh.localEulerAngles.y, handbrakeMesh.localEulerAngles.z);
        }

        if (steeringWheelMesh)
        {
            float targetSteering = moveInput.x * 450f;
            currentSteeringRot = Mathf.Lerp(currentSteeringRot, targetSteering, Time.deltaTime * steeringSmoothness);
            steeringWheelMesh.localEulerAngles = new Vector3(steeringWheelMesh.localEulerAngles.x, currentSteeringRot, steeringWheelMesh.localEulerAngles.z);
        }

        if (gasPedalMesh)
        {
            float targetGas = (moveInput.y > 0) ? -30f : 0f;
            currentGasRot = Mathf.Lerp(currentGasRot, targetGas, Time.deltaTime * pedalSmoothness);
            gasPedalMesh.localEulerAngles = new Vector3(gasPedalMesh.localEulerAngles.x, gasPedalMesh.localEulerAngles.y, currentGasRot);
        }

        if (brakePedalMesh)
        {
            float targetBrake = (moveInput.y < 0) ? -30f : 0f;
            currentBrakeRot = Mathf.Lerp(currentBrakeRot, targetBrake, Time.deltaTime * pedalSmoothness);
            brakePedalMesh.localEulerAngles = new Vector3(brakePedalMesh.localEulerAngles.x, brakePedalMesh.localEulerAngles.y, currentBrakeRot);
        }

        if (clutchPedalMesh)
        {
            float targetClutch = isShiftPressed ? -30f : 0f;
            currentClutchRot = Mathf.Lerp(currentClutchRot, targetClutch, Time.deltaTime * pedalSmoothness);
            clutchPedalMesh.localEulerAngles = new Vector3(clutchPedalMesh.localEulerAngles.x, clutchPedalMesh.localEulerAngles.y, currentClutchRot);
        }
    }

    private void OnGUI()
    {
        if (!showHUD || guiStyle == null || guiSmallStyle == null) return;

        int width = 280;
        int height = 250;
        int x = Screen.width - width - 20;
        int y = Screen.height - height - 20;

        GUI.Box(new Rect(x, y, width, height), "ARAÇ BİLGİ EKRANI");

        Color speedColor = displaySpeed > 180f ? Color.red : (displaySpeed > 100f ? Color.yellow : Color.green);
        guiStyle.normal.textColor = speedColor;
        GUI.Label(new Rect(x + 15, y + 30, 250, 30), $"HIZ: {Mathf.RoundToInt(displaySpeed)} KM/H", guiStyle);

        guiStyle.normal.textColor = Color.white;
        GUI.Label(new Rect(x + 15, y + 60, 250, 30), $"VİTES: {displayGear}", guiStyle);

        guiStyle.normal.textColor = Color.cyan;
        GUI.Label(new Rect(x + 15, y + 90, 250, 30), $"RPM: {Mathf.RoundToInt(displayRPM)}", guiStyle);

        if (isHandbrakeEngaged)
        {
            guiStyle.normal.textColor = Color.red;
            GUI.Label(new Rect(x + 15, y + 120, 250, 30), "EL FRENİ ÇEKİLİ (!)", guiStyle);
        }

        guiSmallStyle.normal.textColor = currentFuelLiters < 5f ? Color.red : Color.yellow;
        GUI.Label(new Rect(x + 15, y + 150, 250, 25), $"Benzin: {currentFuelLiters:F1} L", guiSmallStyle);

        bool hasBattery = carStartSystem != null && carStartSystem.HasBattery;
        string batteryText = hasBattery ? $"Akü: %{currentBatteryPercent:F1}" : "Akü: TAKILI DEĞİL!";
        guiSmallStyle.normal.textColor = (!hasBattery || currentBatteryPercent < 20f) ? Color.red : Color.green;
        GUI.Label(new Rect(x + 15, y + 175, 250, 25), batteryText, guiSmallStyle);

        guiSmallStyle.normal.textColor = Color.gray;
        GUI.Label(new Rect(x + 15, y + 200, 250, 25), $"Yağ: {currentMotorOilLiters:F1}L | Su: {currentCoolingWaterLiters:F1}L", guiSmallStyle);
    }
}
