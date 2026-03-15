using UnityEngine;
using System.Linq;

public class CarStartSystem : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private CarAssemblyManager assemblyManager;
    [SerializeField] private CarFluidTank[] fluidTanks; // Inspector'dan atanabilir veya Start() ile otomatik bulunur

    [Header("Minimum Fluid Requirements")]
    [SerializeField] private float minimumFuel = 1f;
    [SerializeField] private float minimumOil = 1f;
    [SerializeField] private float minimumCoolant = 1f;

    private bool isRunning;

    public bool IsRunning => isRunning;
    public bool HasBattery => assemblyManager != null && assemblyManager.IsPartInstalled(CarPartType.Battery);

    private void Start()
    {
        // Eğer Inspector'dan atanmadıysa araç üzerindeki tüm tankları bul
        if (fluidTanks == null || fluidTanks.Length == 0)
        {
            fluidTanks = GetComponentsInChildren<CarFluidTank>(true);
        }
    }

    public CarFluidTank GetTank(FluidType type)
    {
        if (fluidTanks == null) return null;
        return fluidTanks.FirstOrDefault(t => t.AcceptedFluidType == type);
    }

    private void Update()
    {
        if (isRunning)
        {
            CarStartResult result = CheckConditions(false);
            if (result != CarStartResult.Started)
            {
                isRunning = false;
                Debug.LogWarning("[CarStart] ENGINE DIED: A critical part was removed or fluid dropped below minimum.");
            }
        }
    }

    public CarStartResult TryStart()
    {
        return CheckConditions(true);
    }

    private CarStartResult CheckConditions(bool isStartingAttempt)
    {
        bool hasEngine = assemblyManager != null && assemblyManager.IsPartInstalled(CarPartType.Engine);
        bool hasRadiator = assemblyManager != null && assemblyManager.IsPartInstalled(CarPartType.Radiator);

        CarFluidTank fuelTank = GetTank(FluidType.Gasoline);
        CarFluidTank oilTank = GetTank(FluidType.MotorOil);
        CarFluidTank coolantTank = GetTank(FluidType.Coolant);

        float currentFuel = fuelTank != null ? fuelTank.CurrentFluid : 0f;
        float maxFuel = fuelTank != null ? fuelTank.MaxCapacity : 0f;
        float currentOil = oilTank != null ? oilTank.CurrentFluid : 0f;
        float maxOil = oilTank != null ? oilTank.MaxCapacity : 0f;
        float currentCoolant = coolantTank != null ? coolantTank.CurrentFluid : 0f;
        float maxCoolant = coolantTank != null ? coolantTank.MaxCapacity : 0f;

        bool hasEnoughFuel = currentFuel >= minimumFuel;
        bool hasEnoughOil = currentOil >= minimumOil;
        bool hasEnoughCoolant = currentCoolant >= minimumCoolant;

        if (isStartingAttempt)
        {
            Debug.Log("=== [CarStart] IGNITION ATTEMPT ===");
            Debug.Log($"[CarStart] Battery:   {(HasBattery ? "INSTALLED" : "MISSING")}");
            Debug.Log($"[CarStart] Engine:    {(hasEngine ? "INSTALLED" : "MISSING")}");
            Debug.Log($"[CarStart] Radiator:  {(hasRadiator ? "INSTALLED" : "MISSING")}");
            Debug.Log($"[CarStart] Fuel:      {currentFuel:F1}/{maxFuel:F0} L (min {minimumFuel:F0} L) — {(hasEnoughFuel ? "OK" : "LOW")}");
            Debug.Log($"[CarStart] Oil:       {currentOil:F1}/{maxOil:F0} L (min {minimumOil:F0} L) — {(hasEnoughOil ? "OK" : "LOW")}");
            Debug.Log($"[CarStart] Coolant:   {currentCoolant:F1}/{maxCoolant:F0} L (min {minimumCoolant:F0} L) — {(hasEnoughCoolant ? "OK" : "LOW")}");
        }

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
        }
        return CarStartResult.Started;
    }

    public void StopEngine()
    {
        if (!isRunning) return;
        isRunning = false;
        Debug.Log("[CarStart] Engine stopped.");
    }
}

public enum CarStartResult
{
    NoBattery,
    CrankNoStart,
    Started
}
