using UnityEngine;

public partial class CarController
{
    private void AnimateInteriors(bool isShiftPressed)
    {
        if (gearShiftMesh)
        {
            Vector3 targetGearRot = neutralRot;
            switch (displayGear)
            {
                case "R": targetGearRot = reverseRot; break;
                case "1": targetGearRot = gear1Rot; break;
                case "2": targetGearRot = gear2Rot; break;
                case "3": targetGearRot = gear3Rot; break;
                case "4": targetGearRot = gear4Rot; break;
                case "5": targetGearRot = gear5Rot; break;
                case "N": default: targetGearRot = neutralRot; break;
            }
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

        float fuelRatio = currentFuelLiters / maxFuelLiters;
        SmoothNeedle(fuelNeedle, Mathf.Lerp(fuelEmptyAngle, fuelFullAngle, fuelRatio), 2f);

        float effectiveBattery = (carStartSystem != null && carStartSystem.HasBattery) ? currentBatteryPercent : 0f;
        SmoothNeedle(batteryNeedle, Mathf.Lerp(batteryEmptyAngle, batteryFullAngle, effectiveBattery / maxBatteryPercent), 2f);

        float waterRatio = currentCoolingWaterLiters / maxCoolingWaterLiters;
        SmoothNeedle(waterNeedle, Mathf.Lerp(waterEmptyAngle, waterFullAngle, waterRatio), 2f);
    }

    private void SmoothNeedle(Transform needle, float targetAngle, float speed)
    {
        if (needle == null) return;
        float currentZ = needle.localEulerAngles.z;
        float smoothZ = Mathf.LerpAngle(currentZ, targetAngle, Time.deltaTime * speed);
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
        
        float duration = 3.0f; // 3 saniye
        float halfDuration = duration / 2f;
        float elapsed = 0f;

        // Phase 1: İbreleri Max seviyeye taşı
        while (elapsed < halfDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / halfDuration;
            float smoothT = Mathf.SmoothStep(0f, 1f, t);
            SetSweepAngles(smoothT);
            yield return null;
        }

        // Phase 2: İbreleri Min seviyeye taşı (Gerçek pozisyonlarına SmoothNeedle dönünce geçerler)
        elapsed = 0f;
        while (elapsed < halfDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / halfDuration;
            float smoothT = Mathf.SmoothStep(1f, 0f, t);
            SetSweepAngles(smoothT);
            yield return null;
        }

        isSweepingDials = false;
    }

    private void SetSweepAngles(float t)
    {
        if (speedometerNeedle != null)
        {
            float angle = Mathf.Lerp(speedometerCurve.Evaluate(0f), speedometerCurve.Evaluate(220f), t);
            speedometerNeedle.localRotation = Quaternion.Euler(speedometerNeedle.localEulerAngles.x, speedometerNeedle.localEulerAngles.y, angle);
        }
        if (fuelNeedle != null)
        {
            float angle = Mathf.Lerp(fuelEmptyAngle, fuelFullAngle, t);
            fuelNeedle.localRotation = Quaternion.Euler(fuelNeedle.localEulerAngles.x, fuelNeedle.localEulerAngles.y, angle);
        }
        if (batteryNeedle != null)
        {
            float angle = Mathf.Lerp(batteryEmptyAngle, batteryFullAngle, t);
            batteryNeedle.localRotation = Quaternion.Euler(batteryNeedle.localEulerAngles.x, batteryNeedle.localEulerAngles.y, angle);
        }
        if (waterNeedle != null)
        {
            float angle = Mathf.Lerp(waterEmptyAngle, waterFullAngle, t);
            waterNeedle.localRotation = Quaternion.Euler(waterNeedle.localEulerAngles.x, waterNeedle.localEulerAngles.y, angle);
        }
    }
}
