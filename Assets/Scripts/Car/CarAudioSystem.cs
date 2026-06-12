using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.InputSystem;

public class CarAudioSystem : MonoBehaviour
{
    [Header("--- Mixer ---")]
    [SerializeField] private AudioMixerGroup sfxGroup;

    [Header("--- Motor Sesleri (Loop - FMOD Style) ---")]
    [SerializeField] private AudioClip idleClip;
    [SerializeField] private AudioClip lowOffClip;
    [SerializeField] private AudioClip lowOnClip;
    [SerializeField] private AudioClip medOffClip;
    [SerializeField] private AudioClip medOnClip;
    [SerializeField] private AudioClip highOffClip;
    [SerializeField] private AudioClip highOnClip;
    [SerializeField] private AudioClip maxRpmClip;

    [Header("--- Motor One-Shot ---")]
    [SerializeField] private AudioClip starterCrankClip;
    [SerializeField] private AudioClip engineStartClip;
    [SerializeField] private AudioClip engineStopClip;

    [Header("--- Araç İçi ---")]
    [SerializeField] private AudioClip handbrakeUpClip;
    [SerializeField] private AudioClip handbrakeDownClip;
    [SerializeField] private AudioClip headlightSwitchClip;
    [SerializeField] private AudioClip gearShiftClip;
    [SerializeField] private AudioClip hornClip;

    [Header("--- Çevre Sesleri (Loop) ---")]
    [SerializeField] private AudioClip windLoopClip;
    [SerializeField] private AudioClip tireLoopClip;

    [Header("--- Motor Ses Crossfade Ayarları ---")]
    [SerializeField] private float lowRpmThreshold = 1800f;
    [SerializeField] private float medRpmThreshold = 3500f;
    [SerializeField] private float highRpmThreshold = 5500f;
    [SerializeField] private float maxRpmThreshold = 7000f;
    [SerializeField] private float throttleFadeSpeed = 5f;
    [SerializeField] private float engineFadeSpeed = 5f;

    [Header("--- Çevre Ses Ayarları ---")]
    [SerializeField] private float windMaxVolume = 0.4f;
    [SerializeField] private float windStartSpeed = 20f;
    [SerializeField] private float windFullSpeed = 180f;
    [SerializeField] private float tireMaxVolume = 0.3f;
    [SerializeField] private float tireMinPitch = 0.8f;
    [SerializeField] private float tireMaxPitch = 1.5f;
    [SerializeField] private float tireStartSpeed = 5f;

    [Header("--- One-Shot Ses Ayarları ---")]
    [SerializeField, Range(0f, 1f)] private float oneShotVolume = 0.7f;
    [SerializeField, Range(0f, 1f)] private float hornVolume = 0.9f;

    private CarController carController;
    private CarStartSystem carStartSystem;

    private AudioSource idleSource;
    private AudioSource lowOffSource;
    private AudioSource lowOnSource;
    private AudioSource medOffSource;
    private AudioSource medOnSource;
    private AudioSource highOffSource;
    private AudioSource highOnSource;
    private AudioSource maxRpmSource;
    
    private AudioSource windSource;
    private AudioSource tireSource;
    private AudioSource hornSource;
    private AudioSource oneShotSource;

    private InputAction hornAction;
    private bool wasHornPressed;
    private bool engineAudioActive;

    private void Awake()
    {
        carController = GetComponentInParent<CarController>();
        if (carController == null) carController = GetComponentInChildren<CarController>();

        carStartSystem = GetComponentInParent<CarStartSystem>();
        if (carStartSystem == null) carStartSystem = GetComponentInChildren<CarStartSystem>();

        idleSource = CreateSource("EngineIdle", true, 1f);
        lowOffSource = CreateSource("EngineLowOff", true, 1f);
        lowOnSource = CreateSource("EngineLowOn", true, 1f);
        medOffSource = CreateSource("EngineMedOff", true, 1f);
        medOnSource = CreateSource("EngineMedOn", true, 1f);
        highOffSource = CreateSource("EngineHighOff", true, 1f);
        highOnSource = CreateSource("EngineHighOn", true, 1f);
        maxRpmSource = CreateSource("EngineMaxRPM", true, 1f);
        
        windSource = CreateSource("Wind", true, 0f);
        tireSource = CreateSource("Tire", true, 0f);
        hornSource = CreateSource("Horn", true, 0f);
        oneShotSource = CreateSource("OneShot", false, 1f);

        SetupInputAction();
    }

    private void OnEnable()
    {
        if (carController != null)
        {
            carController.OnHandbrakeToggled += HandleHandbrake;
            carController.OnHeadlightsToggled += HandleHeadlights;
            carController.OnGearShifted += HandleGearShift;
        }

        if (carStartSystem != null)
        {
            carStartSystem.OnStartAttempt += HandleStartAttempt;
            carStartSystem.OnEngineStopped += HandleEngineStopped;
        }

        if (hornAction != null) hornAction.Enable();
    }

    private void OnDisable()
    {
        if (carController != null)
        {
            carController.OnHandbrakeToggled -= HandleHandbrake;
            carController.OnHeadlightsToggled -= HandleHeadlights;
            carController.OnGearShifted -= HandleGearShift;
        }

        if (carStartSystem != null)
        {
            carStartSystem.OnStartAttempt -= HandleStartAttempt;
            carStartSystem.OnEngineStopped -= HandleEngineStopped;
        }

        if (hornAction != null) hornAction.Disable();
    }

    private void Update()
    {
        if (carController == null) return;

        UpdateEngineAudio();
        UpdateEnvironmentAudio();
        UpdateHorn();
    }

    private void UpdateEngineAudio()
    {
        bool isRunning = carStartSystem != null && carStartSystem.IsRunning;

        if (isRunning && !engineAudioActive)
        {
            StartEngineLoops();
            engineAudioActive = true;
        }

        if (isRunning)
        {
            float rpm = carController.DisplayRPM;
            float throttle = Mathf.Clamp01(carController.ThrottleInput);

            float idleWeight = 0f, lowWeight = 0f, medWeight = 0f, highWeight = 0f, maxWeight = 0f;

            if (rpm < lowRpmThreshold)
            {
                float t = Mathf.InverseLerp(800f, lowRpmThreshold, rpm);
                idleWeight = 1f - t;
                lowWeight = t;
            }
            else if (rpm < medRpmThreshold)
            {
                float t = Mathf.InverseLerp(lowRpmThreshold, medRpmThreshold, rpm);
                lowWeight = 1f - t;
                medWeight = t;
            }
            else if (rpm < highRpmThreshold)
            {
                float t = Mathf.InverseLerp(medRpmThreshold, highRpmThreshold, rpm);
                medWeight = 1f - t;
                highWeight = t;
            }
            else if (rpm < maxRpmThreshold)
            {
                float t = Mathf.InverseLerp(highRpmThreshold, maxRpmThreshold, rpm);
                highWeight = 1f - t;
                maxWeight = t;
            }
            else
            {
                maxWeight = 1f;
            }

            float onWeight = throttle;
            float offWeight = 1f - throttle;

            float dt = Time.deltaTime;
            
            idleSource.volume = Mathf.MoveTowards(idleSource.volume, idleWeight, engineFadeSpeed * dt);
            
            lowOnSource.volume = Mathf.MoveTowards(lowOnSource.volume, lowWeight * onWeight, throttleFadeSpeed * dt);
            lowOffSource.volume = Mathf.MoveTowards(lowOffSource.volume, lowWeight * offWeight, throttleFadeSpeed * dt);
            
            medOnSource.volume = Mathf.MoveTowards(medOnSource.volume, medWeight * onWeight, throttleFadeSpeed * dt);
            medOffSource.volume = Mathf.MoveTowards(medOffSource.volume, medWeight * offWeight, throttleFadeSpeed * dt);
            
            highOnSource.volume = Mathf.MoveTowards(highOnSource.volume, highWeight * onWeight, throttleFadeSpeed * dt);
            highOffSource.volume = Mathf.MoveTowards(highOffSource.volume, highWeight * offWeight, throttleFadeSpeed * dt);
            
            maxRpmSource.volume = Mathf.MoveTowards(maxRpmSource.volume, maxWeight, engineFadeSpeed * dt);

            float rpmNorm = Mathf.Clamp01((rpm - 800f) / maxRpmThreshold);
            float basePitch = Mathf.Lerp(0.8f, 1.4f, rpmNorm);
            
            idleSource.pitch = basePitch;
            lowOnSource.pitch = basePitch;
            lowOffSource.pitch = basePitch;
            medOnSource.pitch = basePitch;
            medOffSource.pitch = basePitch;
            highOnSource.pitch = basePitch;
            highOffSource.pitch = basePitch;
            maxRpmSource.pitch = basePitch;
        }
        else
        {
            float dt = Time.deltaTime;
            idleSource.volume = Mathf.MoveTowards(idleSource.volume, 0f, engineFadeSpeed * dt);
            lowOnSource.volume = Mathf.MoveTowards(lowOnSource.volume, 0f, engineFadeSpeed * dt);
            lowOffSource.volume = Mathf.MoveTowards(lowOffSource.volume, 0f, engineFadeSpeed * dt);
            medOnSource.volume = Mathf.MoveTowards(medOnSource.volume, 0f, engineFadeSpeed * dt);
            medOffSource.volume = Mathf.MoveTowards(medOffSource.volume, 0f, engineFadeSpeed * dt);
            highOnSource.volume = Mathf.MoveTowards(highOnSource.volume, 0f, engineFadeSpeed * dt);
            highOffSource.volume = Mathf.MoveTowards(highOffSource.volume, 0f, engineFadeSpeed * dt);
            maxRpmSource.volume = Mathf.MoveTowards(maxRpmSource.volume, 0f, engineFadeSpeed * dt);
        }

        if (!isRunning && engineAudioActive && idleSource.volume < 0.01f && lowOnSource.volume < 0.01f)
        {
            StopEngineLoops();
            engineAudioActive = false;
        }
    }

    private void UpdateEnvironmentAudio()
    {
        float speed = carController.DisplaySpeed;

        if (windLoopClip != null)
        {
            if (speed > windStartSpeed)
            {
                if (!windSource.isPlaying) { windSource.clip = windLoopClip; windSource.Play(); }
                float windNorm = Mathf.Clamp01((speed - windStartSpeed) / (windFullSpeed - windStartSpeed));
                windSource.volume = Mathf.Lerp(0f, windMaxVolume, windNorm);
            }
            else if (windSource.isPlaying)
            {
                windSource.Stop();
            }
        }

        if (tireLoopClip != null)
        {
            if (speed > tireStartSpeed)
            {
                if (!tireSource.isPlaying) { tireSource.clip = tireLoopClip; tireSource.Play(); }
                float tireNorm = Mathf.Clamp01((speed - tireStartSpeed) / 100f);
                tireSource.volume = Mathf.Lerp(0f, tireMaxVolume, tireNorm);
                tireSource.pitch = Mathf.Lerp(tireMinPitch, tireMaxPitch, tireNorm);
            }
            else if (tireSource.isPlaying)
            {
                tireSource.Stop();
            }
        }
    }

    private void UpdateHorn()
    {
        if (hornAction == null || hornClip == null) return;

        bool isHornPressed = hornAction.ReadValue<float>() > 0.5f && carController.isHandsOnWheel;

        if (isHornPressed && !wasHornPressed)
        {
            hornSource.clip = hornClip;
            hornSource.volume = hornVolume;
            hornSource.Play();
            Debug.Log("[CarAudioSystem] Horn started playing.");
        }
        else if (!isHornPressed && wasHornPressed)
        {
            hornSource.Stop();
            Debug.Log("[CarAudioSystem] Horn stopped playing.");
        }

        wasHornPressed = isHornPressed;
    }

    private void HandleStartAttempt(CarStartResult result)
    {
        Debug.Log($"[CarAudioSystem] Start attempt result: {result}");
        if (result == CarStartResult.Started)
        {
            PlayOneShot(engineStartClip);
        }
        else if (result == CarStartResult.CrankNoStart)
        {
            PlayOneShot(starterCrankClip);
        }
        else if (result == CarStartResult.NoBattery)
        {
            Debug.Log("[CarAudioSystem] No battery, no sound played for start attempt.");
        }
    }

    private void HandleEngineStopped()
    {
        Debug.Log("[CarAudioSystem] Engine stopped.");
        PlayOneShot(engineStopClip);
    }

    private void HandleHandbrake(bool engaged)
    {
        Debug.Log($"[CarAudioSystem] Handbrake toggled: {(engaged ? "Engaged" : "Released")}");
        PlayOneShot(engaged ? handbrakeUpClip : handbrakeDownClip);
    }

    private void HandleHeadlights(bool on)
    {
        Debug.Log($"[CarAudioSystem] Headlights toggled: {(on ? "On" : "Off")}");
        PlayOneShot(headlightSwitchClip);
    }

    private void HandleGearShift()
    {
        Debug.Log("[CarAudioSystem] Gear shifted.");
        PlayOneShot(gearShiftClip);
    }

    private void StartEngineLoops()
    {
        Debug.Log("[CarAudioSystem] Starting engine audio loops.");
        PlayLoop(idleSource, idleClip);
        PlayLoop(lowOffSource, lowOffClip);
        PlayLoop(lowOnSource, lowOnClip);
        PlayLoop(medOffSource, medOffClip);
        PlayLoop(medOnSource, medOnClip);
        PlayLoop(highOffSource, highOffClip);
        PlayLoop(highOnSource, highOnClip);
        PlayLoop(maxRpmSource, maxRpmClip);
    }

    private void PlayLoop(AudioSource src, AudioClip clip)
    {
        if (clip != null && !src.isPlaying)
        {
            src.clip = clip;
            src.volume = 0f;
            src.Play();
        }
    }

    private void StopEngineLoops()
    {
        Debug.Log("[CarAudioSystem] Stopping engine audio loops.");
        idleSource.Stop();
        lowOffSource.Stop();
        lowOnSource.Stop();
        medOffSource.Stop();
        medOnSource.Stop();
        highOffSource.Stop();
        highOnSource.Stop();
        maxRpmSource.Stop();
    }

    private void PlayOneShot(AudioClip clip)
    {
        if (clip != null && oneShotSource != null)
        {
            oneShotSource.PlayOneShot(clip, oneShotVolume);
        }
        else if (clip == null)
        {
            Debug.LogWarning("[CarAudioSystem] Cannot play one-shot sound: AudioClip is missing (null)!");
        }
    }

    private AudioSource CreateSource(string label, bool loop, float spatialBlend)
    {
        GameObject child = new GameObject($"Audio_{label}");
        child.transform.SetParent(transform);
        child.transform.localPosition = Vector3.zero;

        AudioSource source = child.AddComponent<AudioSource>();
        source.loop = loop;
        source.playOnAwake = false;
        source.spatialBlend = spatialBlend;
        source.volume = 0f;
        source.outputAudioMixerGroup = sfxGroup;
        return source;
    }

    private void SetupInputAction()
    {
        hornAction = InputHelper.FindDrivingAction("horn");
    }
}
