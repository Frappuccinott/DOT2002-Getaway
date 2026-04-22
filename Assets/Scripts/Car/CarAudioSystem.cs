using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.InputSystem;

public class CarAudioSystem : MonoBehaviour
{
    [Header("--- Mixer ---")]
    [SerializeField] private AudioMixerGroup sfxGroup;

    [Header("--- Motor Sesleri (Loop) ---")]
    [SerializeField] private AudioClip engineIdleClip;
    [SerializeField] private AudioClip engineLoadClip;

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

    [Header("--- Motor Ses Ayarları ---")]
    [SerializeField] private float idleMaxVolume = 0.6f;
    [SerializeField] private float idleMinVolume = 0.05f;
    [SerializeField] private float idleMinPitch = 0.95f;
    [SerializeField] private float idleMaxPitch = 1.15f;
    [SerializeField] private float loadMaxVolume = 0.8f;
    [SerializeField] private float loadMinPitch = 0.6f;
    [SerializeField] private float loadMaxPitch = 1.4f;
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

    private AudioSource engineIdleSource;
    private AudioSource engineLoadSource;
    private AudioSource windSource;
    private AudioSource tireSource;
    private AudioSource hornSource;
    private AudioSource oneShotSource;

    private InputAction hornAction;
    private bool wasHornPressed;
    private float targetIdleVolume;
    private float targetLoadVolume;
    private bool engineAudioActive;

    private void Awake()
    {
        carController = GetComponentInParent<CarController>();
        if (carController == null) carController = GetComponentInChildren<CarController>();

        carStartSystem = GetComponentInParent<CarStartSystem>();
        if (carStartSystem == null) carStartSystem = GetComponentInChildren<CarStartSystem>();

        engineIdleSource = CreateSource("EngineIdle", true, 1f);
        engineLoadSource = CreateSource("EngineLoad", true, 1f);
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
            float rpmNorm = Mathf.Clamp01((rpm - 800f) / 5700f);

            targetIdleVolume = Mathf.Lerp(idleMaxVolume, idleMinVolume, rpmNorm);
            targetLoadVolume = Mathf.Lerp(0f, loadMaxVolume, rpmNorm);

            engineIdleSource.pitch = Mathf.Lerp(idleMinPitch, idleMaxPitch, rpmNorm);
            engineLoadSource.pitch = Mathf.Lerp(loadMinPitch, loadMaxPitch, rpmNorm);
        }
        else
        {
            targetIdleVolume = 0f;
            targetLoadVolume = 0f;
        }

        engineIdleSource.volume = Mathf.MoveTowards(engineIdleSource.volume, targetIdleVolume, engineFadeSpeed * Time.deltaTime);
        engineLoadSource.volume = Mathf.MoveTowards(engineLoadSource.volume, targetLoadVolume, engineFadeSpeed * Time.deltaTime);

        if (!isRunning && engineAudioActive && engineIdleSource.volume < 0.01f)
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
            bool isRunning = carStartSystem != null && carStartSystem.IsRunning;
            if (speed > tireStartSpeed && isRunning)
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
        if (hornAction == null || hornClip == null || !carController.isHandsOnWheel) return;

        bool isHornPressed = hornAction.ReadValue<float>() > 0.5f;

        if (isHornPressed && !wasHornPressed)
        {
            hornSource.clip = hornClip;
            hornSource.volume = hornVolume;
            hornSource.Play();
        }
        else if (!isHornPressed && wasHornPressed)
        {
            hornSource.Stop();
        }

        wasHornPressed = isHornPressed;
    }

    private void HandleStartAttempt(CarStartResult result)
    {
        if (result == CarStartResult.Started)
        {
            PlayOneShot(engineStartClip);
        }
        else if (result == CarStartResult.CrankNoStart)
        {
            PlayOneShot(starterCrankClip);
        }
    }

    private void HandleEngineStopped()
    {
        PlayOneShot(engineStopClip);
    }

    private void HandleHandbrake(bool engaged)
    {
        PlayOneShot(engaged ? handbrakeUpClip : handbrakeDownClip);
    }

    private void HandleHeadlights(bool on)
    {
        PlayOneShot(headlightSwitchClip);
    }

    private void HandleGearShift()
    {
        PlayOneShot(gearShiftClip);
    }

    private void StartEngineLoops()
    {
        if (engineIdleClip != null && !engineIdleSource.isPlaying)
        {
            engineIdleSource.clip = engineIdleClip;
            engineIdleSource.volume = 0f;
            engineIdleSource.Play();
        }

        if (engineLoadClip != null && !engineLoadSource.isPlaying)
        {
            engineLoadSource.clip = engineLoadClip;
            engineLoadSource.volume = 0f;
            engineLoadSource.Play();
        }
    }

    private void StopEngineLoops()
    {
        engineIdleSource.Stop();
        engineLoadSource.Stop();
    }

    private void PlayOneShot(AudioClip clip)
    {
        if (clip != null && oneShotSource != null)
            oneShotSource.PlayOneShot(clip, oneShotVolume);
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
