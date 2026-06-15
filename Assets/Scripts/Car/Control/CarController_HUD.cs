using UnityEngine;

public partial class CarController
{
    private void RefreshHUDStrings()
    {
        int speed = Mathf.RoundToInt(displaySpeed);
        if (speed != lastCachedSpeed) { cachedSpeedText = $"HIZ: {speed} KM/H"; lastCachedSpeed = speed; }
        if (displayGear != lastCachedGear) { cachedGearText = $"VİTES: {displayGear}"; lastCachedGear = displayGear; }
        int rpm = Mathf.RoundToInt(displayRPM);
        if (rpm != lastCachedRpm) { cachedRpmText = $"RPM: {rpm}"; lastCachedRpm = rpm; }

        float fuelRound = Mathf.Round(currentFuelLiters * 10f) / 10f;
        if (fuelRound != lastCachedFuel) { cachedFuelText = $"Benzin: {currentFuelLiters:F1} L"; lastCachedFuel = fuelRound; }

        bool hasBattery = carStartSystem != null && carStartSystem.HasBattery;
        float battRound = Mathf.Round(currentBatteryPercent * 10f) / 10f;
        if (battRound != lastCachedBattery || hasBattery != lastCachedHasBattery)
        {
            cachedBatteryText = hasBattery ? $"Akü: %{currentBatteryPercent:F1}" : "Akü: TAKILI DEĞİL!";
            lastCachedBattery = battRound;
            lastCachedHasBattery = hasBattery;
        }

        float oilRound = Mathf.Round(currentMotorOilLiters * 10f) / 10f;
        float waterRound = Mathf.Round(currentCoolingWaterLiters * 10f) / 10f;
        if (oilRound != lastCachedOil || waterRound != lastCachedWater)
        {
            cachedOilWaterText = $"Yağ: {currentMotorOilLiters:F1}L | Su: {currentCoolingWaterLiters:F1}L";
            lastCachedOil = oilRound;
            lastCachedWater = waterRound;
        }
    }

    private void OnGUI()
    {
        if (!showHUD || guiStyle == null || guiSmallStyle == null) return;

        RefreshHUDStrings();

        if (IsFlipped)
        {
            GUIStyle warningStyle = new GUIStyle(guiStyle);
            warningStyle.fontSize = 24;
            warningStyle.alignment = TextAnchor.MiddleCenter;
            warningStyle.normal.textColor = Color.red;
            
            GUI.Box(new Rect(Screen.width / 2 - 200, Screen.height / 2 - 50, 400, 100), "");
            GUI.Label(new Rect(Screen.width / 2 - 200, Screen.height / 2 - 50, 400, 100), "ARABA TERS DÖNDÜ!\nDÜZELTMEK İÇİN [B] YE BAS", warningStyle);
        }
        
        // Sağ alttaki panel kullanıcının isteği üzerine silindi.
    }
}
