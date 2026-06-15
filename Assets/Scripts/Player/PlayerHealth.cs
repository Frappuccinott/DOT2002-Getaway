using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using TMPro;

public class PlayerHealth : MonoBehaviour
{
    // ==================== CAN AYARLARI ====================

    [Header("=== Can Ayarları ===")]

    [SerializeField, Tooltip("Oyuncunun maksimum canı (oyuna bu değerle başlar)")]
    private float maxHealth = 100f;

    // ==================== UI REFERANSLARI ====================

    [Header("=== UI Referansları ===")]

    [SerializeField, Tooltip("Gösterge tablosundaki sağlık metnini gösteren TextMeshPro UI elementi")]
    private TextMeshProUGUI healthText;

    // ==================== RENK AYARLARI ====================

    [Header("=== UI Renk Ayarları ===")]

    [SerializeField, Tooltip("Can %50 ve üzerindeyken metin rengi")]
    private Color healthyColor = Color.white;

    [SerializeField, Tooltip("Can %50'nin altına düştüğünde metin rengi")]
    private Color criticalColor = Color.red;

    // ==================== SERSEMLETMEn (STUN) AYARLARI ====================

    [Header("=== Sersemletme (Stun) Ayarları ===")]

    [SerializeField, Tooltip("Hasar alındığında sersemletme süresi (saniye)")]
    private float stunDuration = 0.8f;

    // ==================== DURUM DEĞİŞKENLERİ ====================

    /// <summary>
    /// Oyuncunun mevcut canı.
    /// Dışarıdan sadece TakeDamage() ve Heal() ile değiştirilebilir.
    /// </summary>
    private float currentHealth;

    /// <summary>
    /// Oyuncunun ölü olup olmadığını takip eder.
    /// Birden fazla ölüm tetiklenmesini engeller.
    /// </summary>
    private bool isDead = false;

    /// <summary>
    /// Aktif stun Coroutine referansı.
    /// Yeni hasar geldiğinde önceki stun'ı iptal edip süreyi sıfırlamak için kullanılır.
    /// </summary>
    private Coroutine activeStunCoroutine;

    // ==================== PUBLIC ÖZELLİKLER ====================

    /// <summary>
    /// Oyuncunun şu anda sersemletilmiş (stunned) olup olmadığını belirtir.
    ///
    /// Dışarıdaki scriptler (PlayerController vb.) bu değeri OKUYARAK
    /// hareketi engelleyebilir, ancak değeri sadece BU script değiştirebilir.
    ///
    /// Kullanım örneği (PlayerController.cs içinde):
    ///   PlayerHealth health = GetComponent<PlayerHealth>();
    ///   if (health != null && health.isStunned) return; // Hareket etme
    /// </summary>
    public bool isStunned { get; private set; }

    // ================================================================
    //                    UNITY YAŞAM DÖNGÜSÜ
    // ================================================================

    private void Awake()
    {
        // Canı maksimuma ayarla — oyun bu değerle başlar
        currentHealth = maxHealth;

        // Başlangıçta sersemletme kapalı
        isStunned = false;
    }

    private void Start()
    {
        // UI'ı başlangıç değeriyle güncelle
        UpdateHealthUI();
    }

    // ================================================================
    //                   HASAR VE İYİLEŞTİRME
    // ================================================================

    /// <summary>
    /// Oyuncuya hasar verir.
    /// ZombieAI.OnAttackHit() tarafından çağrılır.
    ///
    /// İşlem sırası:
    /// 1. Can düşürülür
    /// 2. UI güncellenir
    /// 3. Oyuncu ölmediyse sersemletme (stun) başlatılır
    /// 4. Can 0 veya altındaysa ölüm tetiklenir
    ///
    /// Kullanım: playerHealth.TakeDamage(25f);
    /// </summary>
    /// <param name="amount">Verilecek hasar miktarı</param>
    public void TakeDamage(float amount)
    {
        // Zaten öldüyse tekrar hasar alma
        if (isDead) return;

        // Negatif veya sıfır hasar kontrolü
        if (amount <= 0f)
        {
            Debug.LogWarning("[PlayerHealth] Negatif veya sıfır hasar verilmeye çalışıldı!");
            return;
        }

        // ===== 1. CANI DÜŞÜR =====
        currentHealth -= amount;
        currentHealth = Mathf.Max(currentHealth, 0f); // 0'ın altına düşmesini engelle

        Debug.Log($"[PlayerHealth] Hasar alındı: {amount} | Kalan can: {currentHealth}/{maxHealth}");

        // ===== 2. UI'I GÜNCELLE =====
        UpdateHealthUI();

        // ===== 3. ÖLÜM KONTROLÜ =====
        if (currentHealth <= 0f)
        {
            Die();
            return; // Öldüyse stun başlatmaya gerek yok
        }

        // ===== 4. SERSEMLETMEYİ BAŞLAT =====
        // Oyuncu hâlâ hayattaysa kısa süreli stun uygula
        // Eğer zaten aktif bir stun varsa, önce onu iptal et (süreyi sıfırla)
        if (activeStunCoroutine != null)
        {
            StopCoroutine(activeStunCoroutine);
        }
        activeStunCoroutine = StartCoroutine(StunRoutine());
    }

    /// <summary>
    /// Oyuncuyu iyileştirir (can ekler).
    /// Can maksimumun üzerine çıkmaz.
    ///
    /// Kullanım: playerHealth.Heal(30f);
    /// </summary>
    /// <param name="amount">İyileştirilecek can miktarı</param>
    public void Heal(float amount)
    {
        if (isDead) return;
        if (amount <= 0f) return;

        currentHealth += amount;
        currentHealth = Mathf.Min(currentHealth, maxHealth); // Maksimumun üzerine çıkmasın

        Debug.Log($"[PlayerHealth] İyileştirildi: +{amount} | Can: {currentHealth}/{maxHealth}");

        // UI'ı güncelle
        UpdateHealthUI();
    }

    // ================================================================
    //                  SERSEMLETMEn (STUN) SİSTEMİ
    // ================================================================

    /// <summary>
    /// Sersemletme (Stun) Coroutine'i.
    ///
    /// Hasar alındığında tetiklenir:
    /// 1. isStunned = true yapılır → dışarıdaki scriptler hareketi durdurur
    /// 2. stunDuration kadar beklenir
    /// 3. isStunned = false yapılır → oyuncu tekrar hareket edebilir
    ///
    /// NOT: Sersemletme sırasında yeni hasar alınırsa, önceki Coroutine
    /// iptal edilir ve süre baştan başlar (TakeDamage içinde yönetilir).
    /// </summary>
    private IEnumerator StunRoutine()
    {
        // Sersemletme başlasın
        isStunned = true;

        Debug.Log($"[PlayerHealth] Oyuncu sersemletildi! Süre: {stunDuration} saniye");

        // Belirtilen süre kadar bekle
        yield return new WaitForSeconds(stunDuration);

        // Sersemletme bitsin — oyuncu tekrar hareket edebilir
        isStunned = false;
        activeStunCoroutine = null;

        Debug.Log("[PlayerHealth] Sersemletme bitti, oyuncu tekrar hareket edebilir.");
    }

    // ================================================================
    //                       UI GÜNCELLEME
    // ================================================================

    /// <summary>
    /// Sağlık değerini sayı olarak UI'da günceller.
    ///
    /// Format: "100", "75", "0" (sadece sayı)
    ///
    /// Renk kuralları:
    ///   - Can >= 50 → healthyColor (varsayılan: beyaz)
    ///   - Can <  50 → criticalColor (varsayılan: kırmızı)
    /// </summary>
    private void UpdateHealthUI()
    {
        if (healthText == null) return;

        // Mevcut canı tam sayıya yuvarla ve sadece sayı olarak göster
        int healthValue = Mathf.RoundToInt(currentHealth);

        // Sadece sayı göster — örnek: "100", "75", "0"
        healthText.text = healthValue.ToString();

        // Renk kontrolü — 50'nin altında kırmızı, 50 ve üstünde normal
        if (currentHealth < 50f)
        {
            healthText.color = criticalColor;
        }
        else
        {
            healthText.color = healthyColor;
        }
    }

    // ================================================================
    //                        ÖLÜM SİSTEMİ
    // ================================================================

    /// <summary>
    /// Oyuncunun ölüm işlemlerini gerçekleştirir.
    /// Mevcut sahneyi anında yeniden yükleyerek oyunu sıfırdan başlatır.
    /// </summary>
    private void Die()
    {
        if (isDead) return;

        isDead = true;

        // Aktif stun varsa temizle
        isStunned = false;
        if (activeStunCoroutine != null)
        {
            StopCoroutine(activeStunCoroutine);
            activeStunCoroutine = null;
        }

        // Sahneyi direkt yeniden yüklemek yerine GameManager'ın ölüm sürecini başlat
        if (GameManager.Instance != null)
        {
            GameManager.Instance.PlayerDied();
        }
        else
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
    }

    // ================================================================
    //              DIŞARIDAN ERİŞİLEBİLİR YARDIMCILAR
    // ================================================================

    /// <summary>
    /// Mevcut canı döndürür.
    /// </summary>
    public float GetCurrentHealth() => currentHealth;

    /// <summary>
    /// Maksimum canı döndürür.
    /// </summary>
    public float GetMaxHealth() => maxHealth;

    /// <summary>
    /// Oyuncunun hayatta olup olmadığını döndürür.
    /// </summary>
    public bool IsAlive() => !isDead;

    /// <summary>
    /// Can yüzdesini döndürür (0–1 arası, UI için değil hesaplama için).
    /// </summary>
    public float GetHealthPercent() => currentHealth / Mathf.Max(maxHealth, 0.01f);
}
