using UnityEngine;

public partial class CarController
{
    private void AnimateInteriors(bool isShiftPressed)
    {
        if (gearShiftMesh)
        {
            Vector3 targetGearRot = displayGear switch
            {
                "R" => reverseRot, "1" => gear1Rot, "2" => gear2Rot,
                "3" => gear3Rot, "4" => gear4Rot, "5" => gear5Rot,
                _ => neutralRot
            };
            gearShiftMesh.localRotation = Quaternion.Slerp(gearShiftMesh.localRotation, Quaternion.Euler(targetGearRot), Time.deltaTime * gearShiftSmoothness);
        }

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

    private void UpdateAnalogDials()
    {
        if (isSweepingDials) return;

        SmoothNeedle(speedometerNeedle, speedometerCurve.Evaluate(displaySpeed), 6f);
        SmoothNeedle(fuelNeedle, Mathf.Lerp(fuelEmptyAngle, fuelFullAngle, currentFuelLiters / maxFuelLiters), 2f);

        float effectiveBattery = (carStartSystem != null && carStartSystem.HasBattery) ? currentBatteryPercent : 0f;
        SmoothNeedle(batteryNeedle, Mathf.Lerp(batteryEmptyAngle, batteryFullAngle, effectiveBattery / maxBatteryPercent), 2f);
        SmoothNeedle(waterNeedle, Mathf.Lerp(waterEmptyAngle, waterFullAngle, currentCoolingWaterLiters / maxCoolingWaterLiters), 2f);
    }

    private void SmoothNeedle(Transform needle, float targetAngle, float speed)
    {
        if (needle == null) return;
        float smoothZ = Mathf.LerpAngle(needle.localEulerAngles.z, targetAngle, Time.deltaTime * speed);
        needle.localRotation = Quaternion.Euler(needle.localEulerAngles.x, needle.localEulerAngles.y, smoothZ);
    }

    private bool isSweepingDials = false;

    public void PlayStartupSweep()
    {
        if (!isSweepingDials) StartCoroutine(StartupSweepRoutine());
    }

    private System.Collections.IEnumerator StartupSweepRoutine()
    {
        isSweepingDials = true;
        float halfDuration = 1.5f;
        float elapsed = 0f;

        while (elapsed < halfDuration)
        {
            elapsed += Time.deltaTime;
            SetSweepAngles(Mathf.SmoothStep(0f, 1f, elapsed / halfDuration));
            yield return null;
        }

        elapsed = 0f;
        while (elapsed < halfDuration)
        {
            elapsed += Time.deltaTime;
            SetSweepAngles(Mathf.SmoothStep(1f, 0f, elapsed / halfDuration));
            yield return null;
        }

        isSweepingDials = false;
    }

    private void SetSweepAngles(float t)
    {
        SetNeedleAngle(speedometerNeedle, Mathf.Lerp(speedometerCurve.Evaluate(0f), speedometerCurve.Evaluate(220f), t));
        SetNeedleAngle(fuelNeedle, Mathf.Lerp(fuelEmptyAngle, fuelFullAngle, t));
        SetNeedleAngle(batteryNeedle, Mathf.Lerp(batteryEmptyAngle, batteryFullAngle, t));
        SetNeedleAngle(waterNeedle, Mathf.Lerp(waterEmptyAngle, waterFullAngle, t));
    }

    private void SetNeedleAngle(Transform needle, float angle)
    {
        if (needle != null)
            needle.localRotation = Quaternion.Euler(needle.localEulerAngles.x, needle.localEulerAngles.y, angle);
    }
}
