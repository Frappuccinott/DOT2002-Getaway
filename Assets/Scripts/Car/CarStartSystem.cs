using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class CarStartSystem : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private CarAssemblyManager assemblyManager;
    [SerializeField] private CarFluidTank[] fluidTanks;

    [Header("Minimum Fluid Requirements")]
    [SerializeField] private float minimumFuel = 1f;
    [SerializeField] private float minimumOil = 1f;
    [SerializeField] private float minimumCoolant = 1f;

    [Header("Kontrol Sıklığı")]
    [SerializeField] private float conditionCheckInterval = 0.5f;

    private bool isRunning;
    public bool cheatEngineInstalled = false;

    private Dictionary<FluidType, CarFluidTank> tankCache;
    private float nextConditionCheck;

    public bool IsRunning => isRunning;
    public bool HasBattery => cheatEngineInstalled || (assemblyManager != null && assemblyManager.IsPartInstalled(CarPartType.Battery));

    public event Action<CarStartResult> OnStartAttempt;
    public event Action OnEngineStopped;

    [Header("--- Unity Events (Gereksinim İçin) ---")]
    public UnityEvent onEngineSuccessfullyStarted;

    private void Start()
    {
        if (fluidTanks == null || fluidTanks.Length == 0)
        {
            fluidTanks = GetComponentsInChildren<CarFluidTank>(true);
        }

        BuildTankCache();
    }

    private void BuildTankCache()
    {
        tankCache = new Dictionary<FluidType, CarFluidTank>();
        if (fluidTanks == null) return;
        foreach (var tank in fluidTanks)
        {
            if (tank != null && !tankCache.ContainsKey(tank.AcceptedFluidType))
                tankCache[tank.AcceptedFluidType] = tank;
        }
    }

    public CarFluidTank GetTank(FluidType type)
    {
        if (tankCache != null && tankCache.TryGetValue(type, out var tank))
            return tank;
        return null;
    }

    private void Update()
    {
        if (!isRunning) return;
        if (Time.time < nextConditionCheck) return;
        nextConditionCheck = Time.time + conditionCheckInterval;

        CarStartResult result = CheckConditions(false);
        if (result != CarStartResult.Started)
        {
            isRunning = false;
            Debug.LogWarning("[CarStart] ENGINE DIED: A critical part was removed or fluid dropped below minimum.");
            OnEngineStopped?.Invoke();
        }
    }

    public CarStartResult TryStart()
    {
        CarStartResult result = CheckConditions(true);
        OnStartAttempt?.Invoke(result);
        return result;
    }

    public void DevQuickStart()
    {
        cheatEngineInstalled = true;
        GetTank(FluidType.Gasoline)?.AddFluid(999f);
        GetTank(FluidType.MotorOil)?.AddFluid(999f);
        GetTank(FluidType.Coolant)?.AddFluid(999f);
    }

    private CarStartResult CheckConditions(bool isStartingAttempt)
    {
        bool hasEngine = cheatEngineInstalled || (assemblyManager != null && assemblyManager.IsPartInstalled(CarPartType.Engine));
        bool hasRadiator = cheatEngineInstalled || (assemblyManager != null && assemblyManager.IsPartInstalled(CarPartType.Radiator));

        CarFluidTank fuelTank = GetTank(FluidType.Gasoline);
        CarFluidTank oilTank = GetTank(FluidType.MotorOil);
        CarFluidTank coolantTank = GetTank(FluidType.Coolant);

        float currentFuel = fuelTank != null ? fuelTank.CurrentFluid : 0f;
        float currentOil = oilTank != null ? oilTank.CurrentFluid : 0f;
        float currentCoolant = coolantTank != null ? coolantTank.CurrentFluid : 0f;

        bool hasEnoughFuel = currentFuel >= minimumFuel;
        bool hasEnoughOil = currentOil >= minimumOil;
        bool hasEnoughCoolant = currentCoolant >= minimumCoolant;

        if (!HasBattery)
        {
            if (isStartingAttempt) Debug.LogWarning("[CarStart] RESULT: No battery — vehicle does not respond at all.");
            return CarStartResult.NoBattery;
        }

        if (!hasEngine)
        {
            if (isStartingAttempt) Debug.LogWarning("[CarStart] RESULT: Cranking... but no engine installed. Car won't start.");
            return CarStartResult.CrankNoStart;
        }

        if (!hasRadiator)
        {
            if (isStartingAttempt) Debug.LogWarning("[CarStart] RESULT: Cranking... but no radiator installed. Car won't start.");
            return CarStartResult.CrankNoStart;
        }

        if (!hasEnoughFuel)
        {
            if (isStartingAttempt) Debug.LogWarning($"[CarStart] RESULT: Cranking... not enough fuel ({currentFuel:F1}/{minimumFuel:F0} L). Car won't start.");
            return CarStartResult.CrankNoStart;
        }

        if (!hasEnoughOil)
        {
            if (isStartingAttempt) Debug.LogWarning($"[CarStart] RESULT: Cranking... not enough oil ({currentOil:F1}/{minimumOil:F0} L). Car won't start.");
            return CarStartResult.CrankNoStart;
        }

        if (!hasEnoughCoolant)
        {
            if (isStartingAttempt) Debug.LogWarning($"[CarStart] RESULT: Cranking... not enough coolant ({currentCoolant:F1}/{minimumCoolant:F0} L). Car won't start.");
            return CarStartResult.CrankNoStart;
        }

        if (isStartingAttempt)
        {
            isRunning = true;
            Debug.Log("[CarStart] RESULT: Car started successfully!");
            onEngineSuccessfullyStarted?.Invoke();
        }
        return CarStartResult.Started;
    }

    public void StopEngine()
    {
        if (!isRunning) return;
        isRunning = false;
        Debug.Log("[CarStart] Engine stopped.");
        OnEngineStopped?.Invoke();
    }
}

public enum CarStartResult
{
    NoBattery,
    CrankNoStart,
    Started
}
