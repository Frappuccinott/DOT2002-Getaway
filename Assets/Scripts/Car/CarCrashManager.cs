using UnityEngine;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Araç Bölgesel ve Kademeli Parçalanma Yöneticisi
/// =================================================
/// 
/// Bu script arabanın ana gövdesinde (Rigidbody olan objede) durur.
/// Çarpışma noktasına ve hızına bağlı olarak parçaları kademeli şekilde koparır.
///
/// Temel Özellikler:
/// 1. Güvenli Bölge (Safe Zone) — Garaj/hangar içinde hasar alınmaz.
/// 2. Oyuncu Çarpışma Yoksayma — "Player" etiketli objelerle temas hasar vermez.
/// 3. Yönsel Kopma — Çarpışma noktasına en yakın parçalar önce kopar.
/// 4. Hıza Duyarlı Kademeli Kopma — Yavaş çarpma = 1 parça, hızlı çarpma = 4+ parça.
/// 5. Gerçekçi Fırlatma — AddExplosionForce + AddTorque ile fiziksel savrulma.
/// 6. Cooldown Sistemi — Fizik motoru spam kopmayı önler.
///
/// Mevcut CarPartSlot / PickupableCarPart / CarAssemblyManager sistemiyle
/// tam uyumlu çalışır — kopan parçalar tekrar takılabilir.
/// </summary>
public class CarCrashManager : MonoBehaviour
{
    // ================================================================
    //                   GÜVENLİ BÖLGE (SAFE ZONE)
    // ================================================================

    [Header("=== Güvenli Bölge (Hangar Koruması) ===")]

    [Tooltip("true iken araç hiçbir çarpışmada hasar almaz.\n" +
        "Garaj/hangar sahnelerinde true yapın, oyun başladığında false yapın.")]
    public bool inSafeZone = true;

    // ================================================================
    //                   ÇARPIŞMA HIZ EŞİKLERİ
    // ================================================================

    [Header("=== Çarpışma Hız Eşikleri ===")]

    [SerializeField, Tooltip("Bu hızın altındaki çarpışmalarda hiçbir parça kopmaz.")]
    private float minCrashSpeed = 10f;

    [SerializeField, Tooltip("Bu hız ve üzerinde maksimum sayıda parça kopar.")]
    private float maxCrashSpeed = 50f;

    // ================================================================
    //                   KOPMA MİKTARI AYARLARI
    // ================================================================

    [Header("=== Kopma Miktarı Ayarları ===")]

    [SerializeField, Tooltip("Minimum hızda (minCrashSpeed) kopacak parça sayısı.")]
    private int minDetachCount = 1;

    [SerializeField, Tooltip("Maksimum hızda (maxCrashSpeed) kopacak parça sayısı.")]
    private int maxDetachCount = 4;

    // ================================================================
    //                   FIRLATMA KUVVETİ AYARLARI
    // ================================================================

    [Header("=== Fırlatma Kuvvet Ayarları ===")]

    [SerializeField, Tooltip("Kopan parçaya uygulanacak temel patlama kuvveti.\n" +
        "80-150 arası gerçekçi saçılma, 500+ uzaya fırlatma.")]
    private float detachForce = 80f;

    [SerializeField, Tooltip("Patlama kuvvetinin etki yarıçapı (metre).")]
    private float explosionRadius = 2f;

    [SerializeField, Tooltip("Patlama kuvvetinin yukarı itme çarpanı.\n" +
        "0 = tamamen yatay, 0.2 = hafif yukarı (önerilen), 1 = uzaya.")]
    private float upwardsModifier = 0.2f;

    [SerializeField, Tooltip("Kopan parçaya uygulanan rastgele tork (dönme) şiddeti.\n" +
        "30-50 arası hafif dönme, 200+ çılgın savrulma.")]
    private float detachTorque = 30f;

    // ================================================================
    //                   COOLDOWN AYARLARI
    // ================================================================

    [Header("=== Cooldown Ayarları ===")]

    [SerializeField, Tooltip("Art arda çarpışmalarda parça kopması arasındaki\n" +
        "minimum bekleme süresi (saniye).\n" +
        "Fizik motorunun aynı frame'de tüm listeyi boşaltmasını engeller.")]
    private float detachCooldown = 0.2f;

    // ================================================================
    //                   PARÇA LİSTESİ
    // ================================================================

    [Header("=== Koparılabilir Parça Slotları ===")]

    [SerializeField, Tooltip("Arabaya takılı, çarpışmada kopabilecek parça slotlarının listesi.\n" +
        "Inspector'dan atayın: kaput, kapılar, tamponlar vb.\n" +
        "Boş bırakılırsa Awake'te otomatik taranır.")]
    private List<CarPartSlot> detachableSlots = new List<CarPartSlot>();

    // ================================================================
    //                   ÖZEL DEĞİŞKENLER
    // ================================================================

    /// <summary>Son parça kopma zamanı — cooldown hesaplaması için.</summary>
    private float lastDetachTime = -Mathf.Infinity;

    /// <summary>Arabanın kendi Rigidbody referansı.</summary>
    private Rigidbody carRigidbody;

    // ================================================================
    //                   UNITY YAŞAM DÖNGÜSÜ
    // ================================================================

    private void Awake()
    {
        // Arabanın Rigidbody bileşenini cache'le
        carRigidbody = GetComponent<Rigidbody>();

        if (carRigidbody == null)
        {
            Debug.LogError($"[CarCrashManager] {gameObject.name} üzerinde Rigidbody bulunamadı! " +
                "Bu script, Rigidbody olan ana gövdede bulunmalıdır.");
        }
    }

    private void Start()
    {
        // Inspector'dan slot atanmamışsa arabanın altındaki tüm slotları otomatik bul
        if (detachableSlots == null || detachableSlots.Count == 0)
        {
            AutoPopulateSlots();
        }

        // Listedeki null referansları temizle
        CleanNullSlots();
    }

    // ================================================================
    //                   ÇARPIŞMA ALGILAMA
    // ================================================================

    /// <summary>
    /// Unity fizik çarpışma eventi.
    /// 
    /// Akış:
    /// 1. Güvenli bölge kontrolü (inSafeZone)
    /// 2. "Player" etiketi kontrolü
    /// 3. Cooldown kontrolü
    /// 4. Hız eşiği kontrolü (minCrashSpeed)
    /// 5. Çarpışma noktasını al
    /// 6. Parçaları mesafeye göre sırala (LINQ)
    /// 7. Hız çarpanına göre kaç parça kopacağını hesapla
    /// 8. En yakın N parçayı kopar ve fırlat
    /// </summary>
    private void OnCollisionEnter(Collision collision)
    {
        // ───────── 1) GÜVENLİ BÖLGE KONTROLÜ ─────────
        // Araba garaj/hangardayken hiçbir hasar almamalı.
        if (inSafeZone) return;

        // ───────── 2) OYUNCU ETİKETİ KONTROLÜ ─────────
        // Oyuncu parça takarken oluşan fizik temaslarını yoksay.
        if (collision.gameObject.CompareTag("Player")) return;

        // ───────── 3) ZOMBİ ETİKETİ KONTROLÜ ─────────
        // Araba zombiye çarptığında parçalanmasın — zombi ezme sırasında
        // ne ana gövde ne de ragdoll kemikleri parça kopmasını tetiklemeli.
        // Root objeyi kontrol ederek hem ana collider hem kemik collider'ları yakalarız.
        if (collision.transform.root.CompareTag("Zombie")) return;

        // ───────── 4) COOLDOWN KONTROLÜ ─────────
        // Aynı frame/kısa süre içinde tekrar tekrar kopma olmasını engelle.
        if (Time.time < lastDetachTime + detachCooldown) return;

        // ───────── 5) ÇARPIŞMA HIZINI HESAPLA ─────────
        float impactSpeed = collision.relativeVelocity.magnitude;

        // Minimum eşiğin altındaysa hiçbir şey yapma — hafif sürtünmeler/dokunmalar
        if (impactSpeed < minCrashSpeed) return;

        // ───────── 5) ÇARPIŞMA NOKTASINI AL ─────────
        if (collision.contactCount == 0) return;
        Vector3 impactPoint = collision.GetContact(0).point;

        Debug.Log($"[CarCrashManager] Çarpışma algılandı! " +
            $"Hız: {impactSpeed:F1} m/s | Nokta: {impactPoint}");

        // ───────── 6) PARÇALARI MESAFEYE GÖRE SIRALA (LINQ) ─────────
        // Null ve sökülmüş parçaları filtrele, çarpışma noktasına en yakından
        // en uzağa doğru sırala.
        CleanNullSlots();

        List<CarPartSlot> sortedSlots = detachableSlots
            .Where(slot => slot != null && slot.IsInstalled)
            .OrderBy(slot => Vector3.Distance(slot.transform.position, impactPoint))
            .ToList();

        // Koparacak takılı parça yoksa çık
        if (sortedSlots.Count == 0)
        {
            Debug.Log("[CarCrashManager] Koparacak takılı parça bulunamadı.");
            return;
        }

        // ───────── 7) HIZ ÇARPANINA GÖRE KOPMA MİKTARINI HESAPLA ─────────
        // InverseLerp: hız min-max aralığında nerede? (0..1 arası normalize değer)
        float speedRatio = Mathf.InverseLerp(minCrashSpeed, maxCrashSpeed, impactSpeed);

        // speedRatio'yu kopma sayısına çevir (min..max parça arası Lerp)
        int detachCount = Mathf.RoundToInt(
            Mathf.Lerp(minDetachCount, maxDetachCount, speedRatio)
        );

        // Mevcut takılı parça sayısını aşmayalım
        detachCount = Mathf.Min(detachCount, sortedSlots.Count);

        Debug.Log($"[CarCrashManager] Hız oranı: {speedRatio:F2} | " +
            $"Kopacak parça sayısı: {detachCount}/{sortedSlots.Count}");

        // ───────── 8) EN YAKIN N PARÇAYI KOPAR ─────────
        for (int i = 0; i < detachCount; i++)
        {
            DetachPart(sortedSlots[i], impactPoint, impactSpeed);
        }

        // Cooldown zamanlayıcısını güncelle
        lastDetachTime = Time.time;
    }

    // ================================================================
    //                   PARÇA KOPARMA VE FIRLATMA
    // ================================================================

    /// <summary>
    /// Belirli bir CarPartSlot'u koparır, fiziksel olarak serbest bırakır
    /// ve çarpışma noktasından dışarıya doğru fırlatır.
    ///
    /// İşlem Sırası:
    /// 1. CarPartSlot.Uninstall() — mevcut montaj sistemiyle uyumlu söküm
    /// 2. Parent bağlantısını kes
    /// 3. Rigidbody ayarla (yoksa ekle, varsa isKinematic = false)
    /// 4. Collider'ları isTrigger = false yap (yerden düşmesin)
    /// 5. Patlama kuvveti + rastgele tork uygula
    /// 6. Arabanın momentumunu aktar
    /// </summary>
    /// <param name="slot">Koparılacak parça slotu</param>
    /// <param name="impactPoint">Çarpışma temas noktası (dünya koordinatları)</param>
    /// <param name="impactSpeed">Çarpışma hızı (kuvvet hesaplaması için)</param>
    private void DetachPart(CarPartSlot slot, Vector3 impactPoint, float impactSpeed)
    {
        // ── 1) Mevcut montaj sisteminden söküm ──
        // CarPartSlot.Uninstall() çağrısı CarAssemblyManager'ı da bilgilendirir
        PickupableCarPart detachedPart = slot.Uninstall();

        if (detachedPart == null)
        {
            Debug.LogWarning($"[CarCrashManager] {slot.gameObject.name} Uninstall() null döndürdü — " +
                "parçada PickupableCarPart referansı eksik olabilir.");
            return;
        }

        GameObject partObj = detachedPart.gameObject;

        // ── 2) Parent bağlantısını kes — arabadan tamamen ayır ──
        partObj.transform.SetParent(null);

        // Objeyi aktif et (Uninstall zaten yapıyor ama garanti olsun)
        partObj.SetActive(true);

        // ── 3) Rigidbody ayarla — fiziksel serbest bırakma ──
        Rigidbody partRb = partObj.GetComponent<Rigidbody>();
        if (partRb == null)
        {
            // Rigidbody yoksa ekle
            partRb = partObj.AddComponent<Rigidbody>();
            partRb.mass = 2f; // Yeni eklenen parçalara makul bir kütle ver
        }
        partRb.isKinematic = false;
        partRb.useGravity = true;
        partRb.linearDamping = 0.5f; // Sürtünme — çok hızlı kaymasın
        partRb.angularDamping = 0.5f;

        // ── 4) Collider ayarla — isTrigger'ı kapat ki haritadan düşmesin ──
        Collider[] partColliders = partObj.GetComponentsInChildren<Collider>();
        foreach (Collider col in partColliders)
        {
            col.isTrigger = false;
            col.enabled = true;
        }

        // ── 5) Fırlatma kuvveti uygula ──
        // Hızla orantılı kuvvet çarpanı (1x — 3x arası)
        float forceMul = Mathf.Clamp(impactSpeed / minCrashSpeed, 1f, 3f);

        // Patlama kuvveti — çarpışma noktasından dışarıya doğru gerçekçi savrulma
        partRb.AddExplosionForce(
            detachForce * forceMul,   // Kuvvet miktarı (hızla orantılı)
            impactPoint,              // Patlamanın merkezi = çarpışma noktası
            explosionRadius,          // Etki yarıçapı
            upwardsModifier,          // Yukarı itme çarpanı
            ForceMode.Impulse         // Anlık darbe kuvveti
        );

        // Rastgele tork — parçanın havada dönerek savrulması için
        Vector3 randomTorque = Random.insideUnitSphere * detachTorque * forceMul;
        partRb.AddTorque(randomTorque, ForceMode.Impulse);

        // ── 6) Arabanın mevcut hızını parçaya aktar — doğal momentum transferi ──
        if (carRigidbody != null)
        {
            partRb.linearVelocity = carRigidbody.linearVelocity * 0.5f;
        }

        Debug.Log($"[CarCrashManager] KOPTU: {slot.AcceptedPartType} | " +
            $"Kuvvet: {detachForce * forceMul:F0}N | Mesafe: " +
            $"{Vector3.Distance(slot.transform.position, impactPoint):F2}m");
    }

    // ================================================================
    //                   YARDIMCI FONKSİYONLAR
    // ================================================================

    /// <summary>
    /// Inspector'dan slot atanmamışsa, arabanın hiyerarşisindeki tüm
    /// CarPartSlot bileşenlerini otomatik olarak bulur ve listeye ekler.
    /// </summary>
    private void AutoPopulateSlots()
    {
        CarPartSlot[] foundSlots = GetComponentsInChildren<CarPartSlot>(true);
        detachableSlots = new List<CarPartSlot>(foundSlots);

        Debug.Log($"[CarCrashManager] Otomatik tarama: " +
            $"{detachableSlots.Count} adet koparılabilir slot bulundu.");
    }

    /// <summary>
    /// Listeden null veya yok edilmiş referansları temizler.
    /// Her kopma öncesi çağrılarak güvenli iterasyon sağlanır.
    /// </summary>
    private void CleanNullSlots()
    {
        detachableSlots.RemoveAll(slot => slot == null);
    }

    // ================================================================
    //                   DIŞARIDAN ERİŞİM API'Sİ
    // ================================================================

    /// <summary>
    /// Runtime'da yeni bir slot ekler.
    /// Örneğin oyuncu yeni parça taktığında çağrılabilir.
    /// </summary>
    public void RegisterSlot(CarPartSlot slot)
    {
        if (slot != null && !detachableSlots.Contains(slot))
        {
            detachableSlots.Add(slot);
        }
    }

    /// <summary>
    /// Bir slotu listeden çıkarır.
    /// Kalıcı olarak yok edilen parçalar için kullanılır.
    /// </summary>
    public void UnregisterSlot(CarPartSlot slot)
    {
        detachableSlots.Remove(slot);
    }

    /// <summary>
    /// Şu anda takılı olan parça sayısını döndürür.
    /// UI veya debug amaçlı kullanılabilir.
    /// </summary>
    public int GetInstalledPartCount()
    {
        int count = 0;
        foreach (var slot in detachableSlots)
        {
            if (slot != null && slot.IsInstalled) count++;
        }
        return count;
    }

    // ================================================================
    //                   GIZMOS (EDİTÖR GÖRSELLERİ)
    // ================================================================

#if UNITY_EDITOR
    /// <summary>
    /// Editörde koparılabilir parça slotlarını görsel olarak çizer.
    /// Yeşil küre = takılı parça, Kırmızı küre = sökülmüş/eksik parça.
    /// Sarı çizgi = slot'tan araba merkezine bağlantı.
    /// </summary>
    private void OnDrawGizmosSelected()
    {
        if (detachableSlots == null) return;

        foreach (var slot in detachableSlots)
        {
            if (slot == null) continue;

            // Takılıysa yeşil, sökülmüşse kırmızı
            Gizmos.color = slot.IsInstalled
                ? new Color(0f, 1f, 0f, 0.5f)
                : new Color(1f, 0f, 0f, 0.5f);

            Gizmos.DrawWireSphere(slot.transform.position, 0.2f);

            // Slot → araba merkezi çizgisi
            Gizmos.color = new Color(1f, 1f, 0f, 0.2f);
            Gizmos.DrawLine(transform.position, slot.transform.position);
        }
    }
#endif
}
