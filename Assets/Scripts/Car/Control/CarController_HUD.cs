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

        int width = 280;
        int height = 250;
        int x = Screen.width - width - 20;
        int y = Screen.height - height - 20;

        if (IsFlipped)
        {
            GUIStyle warningStyle = new GUIStyle(guiStyle);
            warningStyle.fontSize = 24;
            warningStyle.alignment = TextAnchor.MiddleCenter;
            warningStyle.normal.textColor = Color.red;
            
            GUI.Box(new Rect(Screen.width / 2 - 200, Screen.height / 2 - 50, 400, 100), "");
            GUI.Label(new Rect(Screen.width / 2 - 200, Screen.height / 2 - 50, 400, 100), "ARABA TERS DÖNDÜ!\nDÜZELTMEK İÇİN [B] YE BAS", warningStyle);
        }

        GUI.Box(new Rect(x, y, width, height), "ARAÇ BİLGİ EKRANI");

        Color speedColor = displaySpeed > 180f ? Color.red : (displaySpeed > 100f ? Color.yellow : Color.green);
        guiStyle.normal.textColor = speedColor;
        GUI.Label(new Rect(x + 15, y + 30, 250, 30), cachedSpeedText, guiStyle);

        guiStyle.normal.textColor = Color.white;
        GUI.Label(new Rect(x + 15, y + 60, 250, 30), cachedGearText, guiStyle);

        guiStyle.normal.textColor = Color.cyan;
        GUI.Label(new Rect(x + 15, y + 90, 250, 30), cachedRpmText, guiStyle);

        if (isHandbrakeEngaged)
        {
            guiStyle.normal.textColor = Color.red;
            GUI.Label(new Rect(x + 15, y + 120, 250, 30), "EL FRENİ ÇEKİLİ (!)", guiStyle);
        }

        guiSmallStyle.normal.textColor = currentFuelLiters < 5f ? Color.red : Color.yellow;
        GUI.Label(new Rect(x + 15, y + 150, 250, 25), cachedFuelText, guiSmallStyle);

        bool hasBattery = carStartSystem != null && carStartSystem.HasBattery;
        guiSmallStyle.normal.textColor = (!hasBattery || currentBatteryPercent < 20f) ? Color.red : Color.green;
        GUI.Label(new Rect(x + 15, y + 175, 250, 25), cachedBatteryText, guiSmallStyle);

        guiSmallStyle.normal.textColor = Color.gray;
        GUI.Label(new Rect(x + 15, y + 200, 250, 25), cachedOilWaterText, guiSmallStyle);
    }
}
