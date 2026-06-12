using UnityEngine;
using UnityEngine.AI;
using System.Collections;

/// <summary>
/// Zombi AI - State Machine (Durum Makinesi) tabanlı gelişmiş yapay zeka.
/// Durumlar: Idle → Chase → Attack → Dead
///
/// ÖNEMLİ DEĞİŞİKLİKLER (v4):
/// 1. Hasar Toleransı — OnAttackHit() içinde geniş, Inspector'dan ayarlanabilir tolerans.
/// 2. Kayma + Dönme Düzeltmesi — Attack durumunda rotasyon sadece giriş anında yapılır,
///    sonrasında tamamen kilitlenir. Slerp kaldırıldı.
/// 3. Arabayla Ezme — OnCollisionEnter ile "Car" etiketli araç belirli hızın üzerindeyse
///    zombiyi anında öldürür. Ragdoll yerine Animator üzerinden ölüm animasyonu oynatılır,
///    collider kapatılarak arabanın takılmadan geçmesi sağlanır.
/// </summary>
[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(Animator))]
public class ZombieAI : MonoBehaviour
{
    // ==================== DURUM TANIMLARI ====================

    /// <summary>
    /// Zombinin olası durumları.
    /// </summary>
    private enum ZombieState
    {
        Idle,   // Boşta bekleme — hedef algılanana kadar
        Chase,  // Hedefe doğru koşma
        Attack, // Saldırı — çivi gibi sabit, rotasyon kilitli
        Dead    // Ölü — hiçbir işlem yapılmaz
    }

    // ==================== HEDEF REFERANSLARI ====================

    [Header("=== Hedef Referansları ===")]

    [SerializeField, Tooltip("Oyuncunun Transform referansı (Inspector'dan atayın)")]
    private Transform playerTransform;

    [SerializeField, Tooltip("Arabanın Transform referansı (Inspector'dan atayın)")]
    private Transform carTransform;

    [SerializeField, Tooltip("Oyuncu şu anda arabanın içinde mi?")]
    private bool isPlayerInCar = false;

    // ==================== ALGILAMA AYARLARI ====================

    [Header("=== Algılama Ayarları ===")]

    [SerializeField, Tooltip("Zombinin hedefi fark edeceği maksimum mesafe")]
    private float detectionRadius = 25f;

    [SerializeField, Tooltip("Hedef bu mesafenin dışına çıkarsa zombi onu kaybeder")]
    private float loseTargetRadius = 35f;

    // ==================== SALDIRI AYARLARI ====================

    [Header("=== Saldırı Ayarları ===")]

    [SerializeField, Tooltip("Saldırının başlayacağı mesafe (Attack durumuna geçiş eşiği)")]
    private float attackRange = 1.5f;

    [SerializeField, Tooltip("Yakın saldırı (CloseAttack) tetikleneceği mesafe")]
    private float closeAttackRange = 0.8f;

    [SerializeField, Tooltip("Saldırılar arası bekleme süresi (saniye)")]
    private float attackCooldown = 2.0f;

    [SerializeField, Tooltip("Her vuruşta verilecek hasar miktarı")]
    private float attackDamage = 25f;

    [SerializeField, Tooltip("OnAttackHit hasar toleransı — oyuncu bu kadar ekstra uzaklaşsa bile hasar yer")]
    private float damageHitboxTolerance = 2.0f;

    // ==================== HAREKET AYARLARI ====================

    [Header("=== Hareket Ayarları ===")]

    [SerializeField, Tooltip("Hedefi kovalarken koşma hızı")]
    private float chaseSpeed = 3.5f;

    // ==================== ARABA ÇARPMA AYARLARI ====================

    [Header("=== Arabayla Ezme Ayarları ===")]

    [SerializeField, Tooltip("Arabanın zombiyi öldürmesi için gereken minimum çarpma hızı")]
    private float carKillSpeedThreshold = 5f;

    [SerializeField, Tooltip("Araba çarpma kuvvet çarpanı — zombiyi ne kadar sert fırlatır.\n" +
        "Düşük değer = kapota yığılma, yüksek değer = uçma. 1-3 arası ideal.")]
    private float impactMultiplier = 1.5f;

    [SerializeField, Tooltip("Araba çarpmasında ek yukarı kaldırma kuvveti.\n" +
        "Düşük tutun yoksa zombi uzaya fırlar.")]
    private float carHitUpwardForce = 1f;

    [SerializeField, Tooltip("Araba çarpması sonrası cesedi yok etme süresi (saniye)")]
    private float carKillDestroyDelay = 8.0f;

    // ==================== RAGDOLL REFERANSLARI ====================

    [Header("=== Ragdoll Ayarları ===")]

    [SerializeField, Tooltip("Zombinin kalça (Hips) kemik objesi.\n" +
        "Ragdoll kuvveti bu Rigidbody'ye uygulanır.\n" +
        "Boş bırakılırsa otomatik aranır.")]
    private Rigidbody hipsRigidbody;

    // ==================== CAN SİSTEMİ ====================

    [Header("=== Can Sistemi ===")]

    [SerializeField, Tooltip("Zombinin toplam canı")]
    private float maxHealth = 100f;

    [SerializeField, Tooltip("Ölüm sonrası cesedi yok etme süresi (saniye, 0 = yok etme)")]
    private float destroyDelay = 5.0f;

    // ==================== ÖZEL BİLEŞEN REFERANSLARI ====================

    private NavMeshAgent agent;
    private Animator animator;
    private Rigidbody zombieRigidbody;
    private Collider mainCollider;           // Ana CapsuleCollider — ragdoll'da kapatılır
    private Rigidbody[] ragdollRigidbodies;  // Kemiklerdeki tüm Rigidbody'ler
    private Collider[] ragdollColliders;     // Kemiklerdeki tüm Collider'lar

    // ==================== DURUM DEĞİŞKENLERİ ====================

    private ZombieState currentState = ZombieState.Idle;
    private float currentHealth;
    private bool isDead = false;

    // Saldırı zamanlayıcı — oyun başında hemen saldırabilmesi için -Infinity
    private float lastAttackTime = -Mathf.Infinity;

    // ==================== ANİMATOR HASH'LERİ ====================
    // Performans için string yerine hash kullanılır

    private static readonly int HashSpeed = Animator.StringToHash("Speed");
    private static readonly int HashNormalAttack = Animator.StringToHash("NormalAttack");
    private static readonly int HashCloseAttack = Animator.StringToHash("CloseAttack");
    private static readonly int HashDie = Animator.StringToHash("Die");
    private static readonly int HashIsDead = Animator.StringToHash("IsDead");

    // ================================================================
    //                    UNITY YAŞAM DÖNGÜSÜ
    // ================================================================

    private void Awake()
    {
        // Temel bileşenleri al
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();

        // Ana Rigidbody — yoksa ekle (NavMeshAgent kontrolünde)
        zombieRigidbody = GetComponent<Rigidbody>();
        if (zombieRigidbody == null)
        {
            zombieRigidbody = gameObject.AddComponent<Rigidbody>();
        }
        zombieRigidbody.isKinematic = true;
        zombieRigidbody.useGravity = false;

        // Ana CapsuleCollider referansını al — ragdoll'da bunu kapatacağız
        mainCollider = GetComponent<Collider>();

        // ===== RAGDOLL BİLEŞENLERİNİ CACHE'LE =====
        // Kemiklerdeki tüm Rigidbody ve Collider'ları topla
        // (ana objedeki hariç — sadece alt objelerdekiler)
        Rigidbody[] allRbs = GetComponentsInChildren<Rigidbody>();
        Collider[] allCols = GetComponentsInChildren<Collider>();

        // Ana objedeki bileşenleri filtrele — sadece kemik bileşenleri kalsın
        var rbList = new System.Collections.Generic.List<Rigidbody>();
        var colList = new System.Collections.Generic.List<Collider>();

        foreach (Rigidbody rb in allRbs)
        {
            if (rb != zombieRigidbody) rbList.Add(rb);
        }
        foreach (Collider col in allCols)
        {
            if (col != mainCollider) colList.Add(col);
        }

        ragdollRigidbodies = rbList.ToArray();
        ragdollColliders = colList.ToArray();

        // ===== RAGDOLL'U BAŞLANGIÇTA KAPAT =====
        // Kemik Rigidbody'leri kinematic, Collider'ları kapalı başlar
        // Animator kemikleri kontrol eder
        DisableRagdoll();

        // ===== HIPS RİGİDBODY OTOMATİK BULMA =====
        // Inspector'dan atanmamışsa "Hips" adlı kemik objesini ara
        if (hipsRigidbody == null)
        {
            Transform hipsTransform = FindChildRecursive(transform, "Hips");
            if (hipsTransform != null)
            {
                hipsRigidbody = hipsTransform.GetComponent<Rigidbody>();
            }

            if (hipsRigidbody == null)
            {
                Debug.LogWarning($"[ZombieAI] {gameObject.name} — 'Hips' kemik Rigidbody'si bulunamadı! " +
                    "Inspector'dan manuel atayın veya kemik adını kontrol edin.");
            }
        }
    }

    private void Start()
    {
        // Canı maksimuma ayarla
        currentHealth = maxHealth;

        // Başlangıç durumu
        currentState = ZombieState.Idle;

        // ===== ANA COLLIDER'I TRIGGER YAP =====
        // CapsuleCollider trigger olarak ayarlanır — araba fiziksel olarak
        // zombinin içinden geçer, "duvara çarpmış gibi" sekmez.
        // Araba algılama OnTriggerEnter üzerinden yapılır.
        if (mainCollider != null)
        {
            mainCollider.isTrigger = true;
        }
    }

    private void Update()
    {
        // Ölüyse hiçbir şey yapma
        if (isDead) return;

        // NavMesh üzerinde değilse hiçbir şey yapma
        if (!agent.isOnNavMesh) return;

        // Mevcut duruma göre davranış belirle
        switch (currentState)
        {
            case ZombieState.Idle:
                State_Idle();
                break;
            case ZombieState.Chase:
                State_Chase();
                break;
            case ZombieState.Attack:
                State_Attack();
                break;
        }

        // Animator hız parametresini güncelle
        UpdateAnimatorSpeed();
    }

    // ================================================================
    //                      DURUM FONKSİYONLARI
    // ================================================================

    /// <summary>
    /// IDLE — Zombi yerinde durur, hedefe olan mesafeyi kontrol eder.
    /// Hedef algılama menzilinde ise Chase durumuna geçer.
    /// </summary>
    private void State_Idle()
    {
        // Hedef algılama menzilinde mi?
        if (IsTargetInDetectionRange())
        {
            TransitionToState(ZombieState.Chase);
        }
    }

    /// <summary>
    /// CHASE — Zombi hedefe doğru koşar.
    /// Hedef saldırı menzilindeyse Attack'a, kaybetme mesafesinin dışındaysa Idle'a geçer.
    /// </summary>
    private void State_Chase()
    {
        Transform target = GetCurrentTarget();

        // Hedef yoksa Idle'a dön
        if (target == null)
        {
            TransitionToState(ZombieState.Idle);
            return;
        }

        float distanceToTarget = Vector3.Distance(transform.position, target.position);

        // Hedef kaybetme menzilinin dışına çıktıysa bırak
        if (distanceToTarget > loseTargetRadius)
        {
            TransitionToState(ZombieState.Idle);
            return;
        }

        // Saldırı menzilindeyse Attack durumuna geç
        if (distanceToTarget <= attackRange)
        {
            TransitionToState(ZombieState.Attack);
            return;
        }

        // Hedefe doğru koş
        agent.speed = chaseSpeed;
        agent.isStopped = false;
        agent.SetDestination(target.position);
    }

    /// <summary>
    /// ATTACK — Zombi çivi gibi sabit durur.
    ///
    /// ÖNEMLİ: Bu durumda rotasyon GÜNCELLENMEz.
    /// Zombi saldırıya girdiği ilk an (OnEnterState_Attack) hedefe dönmüştür.
    /// Animasyon bitene kadar hem pozisyon hem rotasyon tamamen kilitlidir.
    ///
    /// Sadece cooldown kontrolü ve hedef mesafe kontrolü yapılır.
    /// Hasar, Animation Event (OnAttackHit) üzerinden verilir.
    /// </summary>
    private void State_Attack()
    {
        Transform target = GetCurrentTarget();

        // Hedef yoksa Idle'a dön
        if (target == null)
        {
            TransitionToState(ZombieState.Idle);
            return;
        }

        float distanceToTarget = Vector3.Distance(transform.position, target.position);

        // Hedef saldırı menzilinden fazlasıyla çıktıysa tekrar kovalamaya geç
        if (distanceToTarget > attackRange * 1.5f)
        {
            TransitionToState(ZombieState.Chase);
            return;
        }

        // ===== KİLİTLENME (LOCK) =====
        // Agent'ın ivmesini her frame sıfırla — kayma kesinlikle olmasın
        agent.velocity = Vector3.zero;

        // ===== ROTASYON GÜNCELLENMEZ =====
        // Zombi sadece OnEnterState'de hedefe döndü.
        // Saldırı animasyonu boyunca rotasyonu tamamen kilitli kalır.
        // Slerp, LookAt veya herhangi bir dönüş YAPILMAZ.

        // Cooldown kontrolü — saldırı bekleme süresi doldu mu?
        if (Time.time >= lastAttackTime + attackCooldown)
        {
            // Yeni saldırı başlamadan önce hedefe tekrar dön
            // (cooldown bitti = yeni saldırı serisi başlıyor)
            LockRotationToTarget(target);

            PerformAttack(distanceToTarget);
            lastAttackTime = Time.time;
        }
    }

    // ================================================================
    //                     DURUM GEÇİŞ SİSTEMİ
    // ================================================================

    /// <summary>
    /// Durumlar arası güvenli geçiş yapar.
    /// Önce eski durumdan çıkış, sonra yeni duruma giriş işlemleri uygulanır.
    /// </summary>
    private void TransitionToState(ZombieState newState)
    {
        // Aynı duruma tekrar geçişi engelle
        if (currentState == newState) return;

        // Eski durumdan çıkış işlemleri
        OnExitState(currentState);

        currentState = newState;

        // Yeni duruma giriş işlemleri
        OnEnterState(newState);
    }

    /// <summary>
    /// Bir durumdan çıkarken yapılacak temizlik işlemleri.
    /// </summary>
    private void OnExitState(ZombieState state)
    {
        if (!agent.isOnNavMesh) return;

        switch (state)
        {
            case ZombieState.Attack:
                // Saldırıdan çıkınca agent'ı tekrar harekete hazırla
                agent.isStopped = false;
                break;
        }
    }

    /// <summary>
    /// Bir duruma girerken yapılacak başlangıç ayarları.
    /// </summary>
    private void OnEnterState(ZombieState newState)
    {
        if (!agent.isOnNavMesh) return;

        switch (newState)
        {
            case ZombieState.Idle:
                // Idle — tamamen dur
                agent.isStopped = true;
                agent.velocity = Vector3.zero;
                break;

            case ZombieState.Chase:
                // Chase — harekete başla
                agent.isStopped = false;
                agent.speed = chaseSpeed;
                break;

            case ZombieState.Attack:
                // ===== KRİTİK: KAYMA + DÖNME KESİN ÇÖZÜMü =====

                // 1) Agent tamamen durdurulur — yol bilgisi sıfırlanır
                agent.isStopped = true;
                agent.velocity = Vector3.zero;
                agent.ResetPath();

                // 2) Hedefe anlık dönüş — sadece BU ANDA, bir kerelik
                //    Bundan sonra State_Attack içinde rotasyon GÜNCELLENMEZ
                Transform target = GetCurrentTarget();
                if (target != null)
                {
                    LockRotationToTarget(target);
                }
                break;
        }
    }

    /// <summary>
    /// Zombiyi hedefe doğru anında döndürür (sadece Y ekseninde).
    /// Bu fonksiyon Slerp KULLANMAZ — anlık, kesin dönüştür.
    /// Saldırı durumuna girişte ve yeni saldırı cooldown'u bittiğinde çağrılır.
    /// </summary>
    private void LockRotationToTarget(Transform target)
    {
        Vector3 direction = (target.position - transform.position);
        direction.y = 0f; // Sadece yatay eksende dön
        if (direction.sqrMagnitude > 0.001f)
        {
            transform.rotation = Quaternion.LookRotation(direction.normalized);
        }
    }

    // ================================================================
    //                      SALDIRI SİSTEMİ
    // ================================================================

    /// <summary>
    /// Mesafeye göre uygun saldırı animasyonunu tetikler.
    /// closeAttackRange içindeyse CloseAttack, değilse NormalAttack.
    /// </summary>
    private void PerformAttack(float distanceToTarget)
    {
        if (distanceToTarget <= closeAttackRange)
        {
            // Yakın mesafe saldırısı
            animator.SetTrigger(HashCloseAttack);
        }
        else
        {
            // Normal mesafe saldırısı
            animator.SetTrigger(HashNormalAttack);
        }
    }

    /// <summary>
    /// ANİMASYON EVENT — Saldırı animasyonunun vuruş anında çağrılır.
    /// Bu fonksiyon, animasyon dosyasına eklenen Animation Event tarafından tetiklenir.
    /// Hedefe gerçek hasarı burada uygular.
    ///
    /// HASAR TOLERANSI: attackRange + damageHitboxTolerance kadar geniş bir
    /// mesafe kontrolü yapılır. Böylece oyuncu saldırı animasyonu sırasında
    /// 1-2 adım geri çıksa bile hasardan kaçamaz.
    ///
    /// Kullanım: Animator penceresinde saldırı animasyonunun vuruş karesine
    /// bir Animation Event ekleyin ve fonksiyon olarak "OnAttackHit" seçin.
    /// </summary>
    public void OnAttackHit()
    {
        if (isDead) return;

        Transform target = GetCurrentTarget();
        if (target == null) return;

        float distanceToTarget = Vector3.Distance(transform.position, target.position);

        // ===== GENİŞ HASAR TOLERANSI =====
        // attackRange + damageHitboxTolerance = toplam hasar menzili
        // Varsayılan: 1.5 + 2.0 = 3.5 birim — oyuncu 1-2 adım geri çıksa bile hasar yer
        float totalDamageRange = attackRange + damageHitboxTolerance;

        if (distanceToTarget <= totalDamageRange)
        {
            // Oyuncu arabadaysa arabaya, değilse oyuncuya hasar ver
            if (isPlayerInCar)
            {
                // Arabaya hasar (ileride CarHealth bileşeni eklenebilir)
                // CarHealth carHealth = target.GetComponent<CarHealth>();
                // if (carHealth != null) carHealth.TakeDamage(attackDamage);
                Debug.Log($"[ZombieAI] {gameObject.name} arabaya {attackDamage} hasar verdi!");
            }
            else
            {
                // Oyuncuya hasar — PlayerHealth bileşenini bul ve hasar ver
                PlayerHealth playerHealth = target.GetComponent<PlayerHealth>();
                if (playerHealth != null)
                {
                    playerHealth.TakeDamage(attackDamage);
                }
                else
                {
                    Debug.LogWarning($"[ZombieAI] Hedefte PlayerHealth bileşeni bulunamadı: {target.name}");
                }
            }

            Debug.Log($"[ZombieAI] {gameObject.name} hedefe vurdu! Hasar: {attackDamage} | Mesafe: {distanceToTarget:F2} | Tolerans: {totalDamageRange:F2}");
        }
    }

    // ================================================================
    //                 ARABA ÇARPMA / EZME MEKANİĞİ
    // ================================================================

    /// <summary>
    /// Unity Trigger sistemi ile araba algılama.
    /// 
    /// NEDEN OnTriggerEnter?
    /// OnCollisionEnter kullanıldığında araba zombiye çarptığında
    /// "duvara çarpmış gibi" sekiyordu. CapsuleCollider'ı isTrigger yaparak
    /// araba fiziksel olarak zombinin içinden geçer — sekme olmaz.
    /// Ragdoll aktifleştiğinde arabanın kütlesi (~1500kg) zombi kemiklerini
    /// (~70kg) rahatça ezip geçer.
    ///
    /// Gereksinimler:
    /// - Arabanın root GameObject'ine "Car" Tag'ı atanmış olmalı.
    /// - Zombinin CapsuleCollider'ı isTrigger = true olmalı (Start'ta ayarlanır).
    /// - Arabada Rigidbody olmalı (hız bilgisi için).
    /// </summary>
    private void OnTriggerEnter(Collider other)
    {
        // Zaten ölüyse işlem yapma — birden fazla çalışmayı engelle
        if (isDead) return;

        // Çarpan objenin root'u "Car" etiketine sahip mi?
        // (Arabanın alt parçaları da trigger'a girebilir, root'tan kontrol et)
        if (!other.transform.root.CompareTag("Car")) return;

        // Arabanın Rigidbody'sinden hız bilgisini al
        Rigidbody carRb = other.transform.root.GetComponent<Rigidbody>();
        if (carRb == null)
        {
            // Alt objelerde de ara
            carRb = other.GetComponentInParent<Rigidbody>();
        }

        float impactSpeed = carRb != null ? carRb.linearVelocity.magnitude : 0f;

        Debug.Log($"[ZombieAI] {gameObject.name} araba trigger algılandı! " +
            $"Araba hızı: {impactSpeed:F2} | Eşik: {carKillSpeedThreshold:F2}");

        // Araba hızı eşik değerini aşıyorsa → Ragdoll ölümü
        if (impactSpeed >= carKillSpeedThreshold)
        {
            DieByCarImpact(other, carRb);
        }
    }

    /// <summary>
    /// Arabayla Ezilme Ölümü — Trigger + Ragdoll sistemi.
    ///
    /// OnTriggerEnter'dan çağrılır — araba fiziksel olarak zombinin içinden geçer,
    /// sekme/takılma olmaz. Ragdoll kemiklerine arabanın hızıyla orantılı
    /// kontrollü bir kuvvet uygulanır.
    ///
    /// AŞAMA 1: Animator/Agent kapatılır, CapsuleCollider devre dışı
    /// AŞAMA 2: Ragdoll aktif — kemik Rigidbody'leri fizik motoruna teslim
    /// AŞAMA 3: Hips'e arabanın hız yönünde makul bir AddForce
    /// </summary>
    /// <param name="carCollider">Trigger'a giren arabanın Collider'ı</param>
    /// <param name="carRb">Arabanın Rigidbody'si (hız bilgisi için)</param>
    private void DieByCarImpact(Collider carCollider, Rigidbody carRb)
    {
        // ===== TEKRAR ÇALIŞMA KORUMASI =====
        if (isDead) return;

        float carSpeed = carRb != null ? carRb.linearVelocity.magnitude : 0f;
        Debug.Log($"[ZombieAI] {gameObject.name} araba tarafından ezildi! " +
            $"Ragdoll aktif ediliyor. (Araba hızı: {carSpeed:F2})");

        // ===== 1) DURUM GÜNCELLEMESİ =====
        isDead = true;
        currentHealth = 0f;
        currentState = ZombieState.Dead;

        // ===== 2) ANİMATÖRÜ KAPAT =====
        // Ragdoll sırasında Animator çalışırsa kemikleri override eder → kapatılmalı
        animator.enabled = false;

        // ===== 3) NAVMESHAGENT'I DEVRE DIŞI BIRAK =====
        if (agent.isOnNavMesh)
        {
            agent.isStopped = true;
            agent.velocity = Vector3.zero;
        }
        agent.enabled = false;

        // ===== 4) ANA COLLIDER'I KAPAT =====
        // CapsuleCollider(trigger) kapatılır — ragdoll kemik collider'ları devralır
        if (mainCollider != null)
        {
            mainCollider.enabled = false;
        }

        // ===== 5) ANA RİGİDBODY'Yİ KİNEMATİK YAP =====
        // Ana Rigidbody artık gereksiz — ragdoll kemikleri fizik yapacak
        zombieRigidbody.isKinematic = true;
        zombieRigidbody.useGravity = false;

        // ===== 6) RAGDOLL'U AKTİF ET =====
        // Tüm kemik Rigidbody'leri isKinematic = false
        // Tüm kemik Collider'ları aktif
        EnableRagdoll();

        // ===== 7) KALÇAYA (HIPS) KUVVET UYGULA =====
        // GTA tarzı: zombi kapota yığılır veya kısa mesafe sürüklenir
        // Arabanın hız yönünde, orantılı ama aşırıya kaçmayan bir itme
        if (hipsRigidbody != null && carRb != null)
        {
            // Arabanın mevcut hız vektörü — yön ve büyüklük bilgisi
            Vector3 carVelocity = carRb.linearVelocity;
            Vector3 impactDirection = carVelocity.normalized;
            float impactMagnitude = carVelocity.magnitude;

            // Kuvvet: hız × çarpan — fazla uçurmaması için clamp'li (maks 30N)
            float clampedForce = Mathf.Min(impactMagnitude * impactMultiplier, 30f);
            Vector3 force = impactDirection * clampedForce;

            // Hafif yukarı kaldırma — zombi yere yapışmasın ama uzaya da fırlamasın
            force += Vector3.up * carHitUpwardForce;

            hipsRigidbody.AddForce(force, ForceMode.Impulse);

            Debug.Log($"[ZombieAI] Ragdoll kuvvet: {force.magnitude:F1}N | " +
                $"Yön: {impactDirection} | Araba hızı: {impactMagnitude:F1}");
        }
        else
        {
            Debug.LogWarning($"[ZombieAI] {gameObject.name} — hipsRigidbody veya carRb null, " +
                "ragdoll kuvveti uygulanamadı!");
        }

        // ===== 8) GECİKMELİ YOK ETME =====
        if (carKillDestroyDelay > 0f)
        {
            Destroy(gameObject, carKillDestroyDelay);
        }
    }

    // ================================================================
    //                    RAGDOLL SİSTEMİ
    // ================================================================

    /// <summary>
    /// Ragdoll'u aktif eder — kemiklerdeki tüm Rigidbody'leri fizik motoruna
    /// teslim eder ve Collider'ları açar.
    /// Zombi artık "bez bebek" gibi davranır.
    /// </summary>
    private void EnableRagdoll()
    {
        // Tüm kemik Rigidbody'lerini fizik motoruna teslim et
        foreach (Rigidbody rb in ragdollRigidbodies)
        {
            if (rb == null) continue;
            rb.isKinematic = false;
            rb.useGravity = true;
        }

        // Tüm kemik Collider'larını aç — zeminle ve objelerle etkileşsin
        foreach (Collider col in ragdollColliders)
        {
            if (col == null) continue;
            col.enabled = true;
        }
    }

    /// <summary>
    /// Ragdoll'u devre dışı bırakır — kemik Rigidbody'leri kinematic,
    /// kemik Collider'ları kapalı. Animator kemikleri kontrol eder.
    /// Awake'te çağrılır.
    /// </summary>
    private void DisableRagdoll()
    {
        foreach (Rigidbody rb in ragdollRigidbodies)
        {
            if (rb == null) continue;
            rb.isKinematic = true;
            rb.useGravity = false;
        }

        foreach (Collider col in ragdollColliders)
        {
            if (col == null) continue;
            col.enabled = false;
        }
    }

    /// <summary>
    /// Transform hiyerarşisinde belirli isimdeki child objeyi rekürsif arar.
    /// Ragdoll Hips kemik objesini bulmak için kullanılır.
    /// </summary>
    private Transform FindChildRecursive(Transform parent, string childName)
    {
        foreach (Transform child in parent)
        {
            // Adı tam eşleşme veya "Hips" içeriyorsa kabul et
            if (child.name.Contains(childName))
                return child;

            Transform found = FindChildRecursive(child, childName);
            if (found != null) return found;
        }
        return null;
    }

    // ================================================================
    //                    YARDIMCI FONKSİYONLAR
    // ================================================================

    /// <summary>
    /// Oyuncunun mevcut durumuna göre doğru hedefi döndürür.
    /// isPlayerInCar true ise araba, değilse oyuncu hedef alınır.
    /// </summary>
    private Transform GetCurrentTarget()
    {
        if (isPlayerInCar && carTransform != null)
        {
            return carTransform;
        }
        return playerTransform;
    }

    /// <summary>
    /// Hedefin algılama menzilinde olup olmadığını kontrol eder.
    /// </summary>
    private bool IsTargetInDetectionRange()
    {
        Transform target = GetCurrentTarget();
        if (target == null) return false;
        return Vector3.Distance(transform.position, target.position) <= detectionRadius;
    }

    /// <summary>
    /// Animator'daki Speed parametresini günceller.
    /// Agent'ın gerçek hızını 0–1 aralığına normalize eder.
    /// Idle ve Attack durumlarında hız her zaman 0 olur.
    /// </summary>
    private void UpdateAnimatorSpeed()
    {
        // NavMesh üzerinde değilse hızı sıfırla
        if (!agent.isOnNavMesh)
        {
            animator.SetFloat(HashSpeed, 0f);
            return;
        }

        float targetSpeed = 0f;

        // Saldırı ve Idle durumunda hız her zaman 0
        if (currentState == ZombieState.Attack || currentState == ZombieState.Idle)
        {
            targetSpeed = 0f;
        }
        else
        {
            // Agent'ın gerçek hızını al ve normalize et
            float currentVelocity = agent.velocity.magnitude;
            targetSpeed = Mathf.Clamp01(currentVelocity / Mathf.Max(chaseSpeed, 0.01f));
        }

        // Yumuşak geçiş için Lerp uygula
        float currentAnimSpeed = animator.GetFloat(HashSpeed);
        float smoothedSpeed = Mathf.Lerp(currentAnimSpeed, targetSpeed, Time.deltaTime * 8f);
        animator.SetFloat(HashSpeed, smoothedSpeed);
    }

    // ================================================================
    //              DIŞARIDAN ERİŞİLEBİLİR FONKSİYONLAR
    // ================================================================

    /// <summary>
    /// Zombiye hasar verir.
    /// Canı sıfırın altına düşerse ölüm tetiklenir.
    /// Kullanım: zombieAI.TakeDamage(25f);
    /// </summary>
    public void TakeDamage(float damage)
    {
        if (isDead) return;

        currentHealth -= damage;

        if (currentHealth <= 0f)
        {
            currentHealth = 0f;
            Die();
        }
    }

    /// <summary>
    /// Zombinin ölüm işlemlerini gerçekleştirir.
    /// NavMeshAgent durdurulur, collider'lar kapatılır, ölüm animasyonu tetiklenir.
    /// </summary>
    private void Die()
    {
        if (isDead) return;

        isDead = true;
        currentState = ZombieState.Dead;

        // Ölüm animasyonu
        animator.SetBool(HashIsDead, true);
        animator.SetTrigger(HashDie);

        // NavMeshAgent'ı tamamen durdur
        if (agent.isOnNavMesh)
        {
            agent.isStopped = true;
            agent.velocity = Vector3.zero;
        }
        agent.enabled = false;

        // Ana collider'ı kapat
        if (mainCollider != null)
        {
            mainCollider.enabled = false;
        }

        // Belirli bir süre sonra GameObject'i yok et
        if (destroyDelay > 0f)
        {
            Destroy(gameObject, destroyDelay);
        }
    }

    /// <summary>
    /// Oyuncunun araba durumunu dışarıdan güncellemek için.
    /// Kullanım: zombieAI.SetPlayerInCar(true);
    /// </summary>
    public void SetPlayerInCar(bool inCar)
    {
        isPlayerInCar = inCar;
    }

    /// <summary>
    /// Oyuncu referansını runtime'da atamak için.
    /// </summary>
    public void SetPlayerTransform(Transform player)
    {
        playerTransform = player;
    }

    /// <summary>
    /// Araba referansını runtime'da atamak için.
    /// </summary>
    public void SetCarTransform(Transform car)
    {
        carTransform = car;
    }

    /// <summary>
    /// Zombinin mevcut sağlık durumunu döndürür.
    /// </summary>
    public float GetCurrentHealth() => currentHealth;

    /// <summary>
    /// Zombinin hayatta olup olmadığını döndürür.
    /// </summary>
    public bool IsAlive() => !isDead;

    // ================================================================
    //              GIZMOS (EDITOR GÖRSEL YARDIMCILARI)
    // ================================================================

#if UNITY_EDITOR
    /// <summary>
    /// Editörde algılama ve saldırı menzillerini görsel olarak çizer.
    /// Sadece zombi seçiliyken görünür.
    /// </summary>
    private void OnDrawGizmosSelected()
    {
        // Algılama menzili — Yeşil
        Gizmos.color = new Color(0f, 1f, 0f, 0.15f);
        Gizmos.DrawWireSphere(transform.position, detectionRadius);

        // Hedef kaybetme menzili — Sarı
        Gizmos.color = new Color(1f, 1f, 0f, 0.1f);
        Gizmos.DrawWireSphere(transform.position, loseTargetRadius);

        // Saldırı menzili — Kırmızı
        Gizmos.color = new Color(1f, 0f, 0f, 0.25f);
        Gizmos.DrawWireSphere(transform.position, attackRange);

        // Hasar tolerans menzili — Turuncu (gerçek hasar alanı)
        Gizmos.color = new Color(1f, 0.5f, 0f, 0.15f);
        Gizmos.DrawWireSphere(transform.position, attackRange + damageHitboxTolerance);

        // Yakın saldırı menzili — Koyu Kırmızı
        Gizmos.color = new Color(0.8f, 0f, 0f, 0.35f);
        Gizmos.DrawWireSphere(transform.position, closeAttackRange);
    }
#endif
}
