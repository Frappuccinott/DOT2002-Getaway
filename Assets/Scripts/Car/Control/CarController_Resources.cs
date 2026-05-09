using UnityEngine;

public partial class CarController
{
    private void ConsumeResources(float speedKMH)
    {
        if (carStartSystem == null) return;

        bool isRunning = carStartSystem.IsRunning;
        bool hasBattery = carStartSystem.HasBattery;

        if (isRunning)
        {
            float dt = consumptionMultiplier * Time.fixedDeltaTime;
            float fuelDrainRate = 0.001f + (speedKMH * 0.0002f);
            float oilDrainRate = 0.0001f + (speedKMH * 0.00001f);
            float waterDrainRate = 0.0002f + (speedKMH * 0.00005f);

            carStartSystem.GetTank(FluidType.Gasoline)?.ConsumeFluid(fuelDrainRate * dt);
            carStartSystem.GetTank(FluidType.MotorOil)?.ConsumeFluid(oilDrainRate * dt);
            carStartSystem.GetTank(FluidType.Coolant)?.ConsumeFluid(waterDrainRate * dt);

            if (hasBattery)
            {
                float batteryDrainRate = 0.005f + (areHeadlightsOn ? 0.05f : 0f);
                currentBatteryPercent -= batteryDrainRate * consumptionMultiplier * Time.fixedDeltaTime;
                currentBatteryPercent = Mathf.Max(0f, currentBatteryPercent);
            }
        }

        if (currentBatteryPercent <= 0f || !hasBattery)
        {
            if (areHeadlightsOn)
            {
                areHeadlightsOn = false;
                if (headlights != null)
                {
                    foreach (var light in headlights)
                    {
                        if (light != null)
                        {
                            CarPartSlot slot = light.GetComponentInParent<CarPartSlot>(true);
                            if (slot != null && !slot.IsInstalled) continue;
                            light.SetActive(false);
                        }
                    }
                }
            }
            if (handbrakeLight != null && handbrakeLight.activeSelf)
            {
                handbrakeLight.SetActive(false);
            }
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
            OnGearShifted?.Invoke();
        }
        previousGear = displayGear;

        if (displayGear == "N")
        {
            displayRPM = Mathf.Lerp(displayRPM, 800f + (acceleration != 0 ? 3000f : 0f), Time.fixedDeltaTime * 5f);
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
            displayRPM -= Time.fixedDeltaTime * 2000f;
            displayRPM = Mathf.Max(displayRPM, 800f);
        }
    }

    private void TriggerGearShiftJolt()
    {
        if (displayGear == "R" || displayGear == "N") return;

        if (int.TryParse(displayGear, out int curG) && int.TryParse(previousGear, out int prevG) && curG > prevG)
        {
            float dynamicShiftDelay = displaySpeed > 70f ? gearShiftDelay * 0.2f : gearShiftDelay;
            currentShiftTimer = dynamicShiftDelay;
            displayRPM *= 0.65f;
        }
    }

    private void UpdateWarningLights()
    {
        bool hasPower = currentBatteryPercent > 0f && (carStartSystem != null && carStartSystem.HasBattery);
        bool engineRunning = carStartSystem != null && carStartSystem.IsRunning;
        bool shouldLightsBeOn = hasPower && engineRunning;

        SetWarningLight(fuelWarningLight, currentFuelLiters, fuelWarningThreshold, shouldLightsBeOn);
        SetWarningLight(batteryWarningLight, currentBatteryPercent, batteryWarningThreshold, shouldLightsBeOn);
        SetWarningLight(oilWarningLight, currentMotorOilLiters, oilWarningThreshold, shouldLightsBeOn);
        SetWarningLight(waterWarningLight, currentCoolingWaterLiters, waterWarningThreshold, shouldLightsBeOn);

        if (handbrakeLight != null)
        {
            bool shouldBeOn = engineRunning && hasPower && isHandbrakeEngaged;
            if (handbrakeLight.activeSelf != shouldBeOn) handbrakeLight.SetActive(shouldBeOn);
        }
    }

    private void SetWarningLight(GameObject lightObj, float currentValue, float threshold, bool hasPower)
    {
        if (lightObj == null) return;

        if (!hasPower)
        {
            if (lightObj.activeSelf) lightObj.SetActive(false);
            return;
        }

        if (currentValue <= 0f)
        {
            if (!lightObj.activeSelf) lightObj.SetActive(true);
        }
        else if (currentValue <= threshold)
        {
            bool blinkState = Mathf.PingPong(Time.time * warningBlinkSpeed, 1f) > 0.5f;
            if (lightObj.activeSelf != blinkState) lightObj.SetActive(blinkState);
        }
        else
        {
            if (lightObj.activeSelf) lightObj.SetActive(false);
        }
    }

    private void UpdateBrakeLights()
    {
        if (brakeLights == null || brakeLights.Length == 0) return;

        bool isBraking = (moveInput.y < 0) && (currentBatteryPercent > 0f);
        foreach (var light in brakeLights)
        {
            if (light != null)
            {
                CarPartSlot slot = light.GetComponentInParent<CarPartSlot>(true);
                bool actualBraking = isBraking;
                if (slot != null && !slot.IsInstalled) actualBraking = false;

                if (light.activeSelf != actualBraking) light.SetActive(actualBraking);
            }
        }
    }
}
