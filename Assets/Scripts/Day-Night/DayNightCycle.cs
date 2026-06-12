using UnityEngine;
using TMPro; // TextMeshPro için eklendi

public class DayNightCycle : MonoBehaviour
{
    [Header("Zaman Ayarları")]
    public float dayDurationInSeconds = 360f; // 6 dakika = 360 saniye (3 dk gündüz, 3 dk gece)

    [Header("UI Ayarları")]
    public TextMeshProUGUI timeText; // Inspector'dan atanacak TextMeshPro objesi

    [Header("Skybox Materyalleri (6 Sided)")]
    public Material daySkybox;
    public Material nightSkybox;

    [Header("Işık Kaynakları")]
    public Light sunLight;
    public Light moonLight;

    public float maxMoonIntensity = 0.5f;
    private float defaultSunIntensity;

    // Editördeki orijinal resimlerin bozulmaması için geçici materyaller
    private Material dayInst;
    private Material nightInst;

    private Color dayOriginalTint;
    private Color nightOriginalTint;
    private bool isNight = false;

    private float currentRotation; // Zamanı doğru hesaplayabilmek için güneşin açısını takip ediyoruz

    void Start()
    {
        if (sunLight != null) 
        {
            defaultSunIntensity = sunLight.intensity;
            currentRotation = sunLight.transform.eulerAngles.x; // Güneşin başlangıç açısını al
        }
        if (moonLight != null) moonLight.intensity = 0f;

        // Orijinal 6-Sided materyallerinin kopyasını çıkarıyoruz ki renkleri kalıcı bozulmasın
        if (daySkybox != null)
        {
            dayInst = new Material(daySkybox);
            dayOriginalTint = dayInst.GetColor("_Tint");
        }
        if (nightSkybox != null)
        {
            nightInst = new Material(nightSkybox);
            nightOriginalTint = nightInst.GetColor("_Tint");
        }

        // Oyuna gündüz ile başla
        RenderSettings.skybox = dayInst;
    }

    void Update()
    {
        // Boşluklar doldurulmadıysa kodun çökmesini engelle
        if (sunLight == null || dayInst == null || nightInst == null) return;

        // 1. Zamanı Akıt (Güneşi Döndür)
        float rotationSpeed = 360f / dayDurationInSeconds;
        sunLight.transform.Rotate(Vector3.right, rotationSpeed * Time.deltaTime);

        // Açı takibi ve UI Güncellemesi
        currentRotation += rotationSpeed * Time.deltaTime;
        currentRotation %= 360f;

        if (timeText != null)
        {
            // Açıya göre saati hesapla: X=0 -> 06:00, X=90 -> 12:00, X=180 -> 18:00, X=270 -> 00:00
            float timeInHours = ((currentRotation / 360f) * 24f + 6f) % 24f;
            int hours = Mathf.FloorToInt(timeInHours);
            timeText.text = string.Format("{0:00}.00", hours);
        }

        // Ay yönünü senkronize et
        if (moonLight != null)
        {
            moonLight.transform.rotation = sunLight.transform.rotation * Quaternion.Euler(180, 0, 0);
        }

        // 2. Güneşin Açısını Bul (Y ekseninin yönü)
        // Y ekseni 0'dan küçükse (aşağı bakıyorsa) GÜNDÜZ, 0'dan büyükse GECE
        float sunHeight = sunLight.transform.forward.y;

        // 3. Geçiş ve Karartma Mantığı
        if (sunHeight > 0)
        {
            // --- GECE ---
            if (!isNight)
            {
                RenderSettings.skybox = nightInst; // Gökyüzünü değiştir
                isNight = true;
            }

            // Güneş battıktan hemen sonra yıldızlı geceyi yavaşça aydınlat
            float fade = Mathf.Clamp01(sunHeight * 5f);
            nightInst.SetColor("_Tint", Color.Lerp(Color.black, nightOriginalTint, fade));

            // Işıkları yönet
            sunLight.intensity = Mathf.Lerp(sunLight.intensity, 0f, Time.deltaTime * 2f);
            if (moonLight != null) moonLight.intensity = Mathf.Lerp(moonLight.intensity, maxMoonIntensity, Time.deltaTime * 2f);
        }
        else
        {
            // --- GÜNDÜZ ---
            if (isNight)
            {
                RenderSettings.skybox = dayInst; // Gökyüzünü değiştir
                isNight = false;
            }

            // Güneş batarken (ufka inerken) gündüz gökyüzünü yavaşça siyaha karart
            float fade = Mathf.Clamp01((-sunHeight) * 5f);
            dayInst.SetColor("_Tint", Color.Lerp(Color.black, dayOriginalTint, fade));

            // Işıkları yönet
            sunLight.intensity = Mathf.Lerp(sunLight.intensity, defaultSunIntensity, Time.deltaTime * 2f);
            if (moonLight != null) moonLight.intensity = Mathf.Lerp(moonLight.intensity, 0f, Time.deltaTime * 2f);
        }
    }
}
