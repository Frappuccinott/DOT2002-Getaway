using UnityEngine;

public class HingeDoor : MonoBehaviour
{
    public enum DoorType { CarDoor, Hood, Trunk, FuelCap, HangarDoor, GenericDoor }

    [Header("Kapı Tipi")]
    [SerializeField] private DoorType doorType = DoorType.GenericDoor;

    [Header("Menteşe Ayarları")]
    [SerializeField] private Transform hingePoint;
    [SerializeField] private Vector3 rotationAxis = Vector3.up;

    [Header("Açı Limitleri")]
    [SerializeField] private float minAngle = 0f;
    [SerializeField] private float maxAngle = 70f;

    [Header("Kontrol")]
    [SerializeField] private float dragSensitivity = 0.5f;
    [SerializeField] private float smoothSpeed = 10f;

    [Header("Bağlantılar")]
    [SerializeField] private CarPartSlot linkedSlot;

    private float currentAngle;
    private float targetAngle;
    private Quaternion initialRotation;
    private Transform rotationTarget;

    // Çarpışma algılama için kapının kendi collider'larını ve kök (araba) collider'larını cache'liyoruz.
    private Collider[] doorColliders;
    private Collider[] carBodyColliders;

    public DoorType Type => doorType;
    public bool IsOpen => currentAngle > minAngle + 1f;

    public bool CanOperate
    {
        get
        {
            if (linkedSlot == null) return true;
            return linkedSlot.IsInstalled;
        }
    }

    private void Start()
    {
        // Eğer bu obje yerden alınabilir bir parçaysa (PickupableCarPart varsa),
        // Rigidbody ve IgnoreCollision kurulumunu YAPMA.
        // Bu adımlar sadece arabaya takılı kapılar için gereklidir.
        // Yerdeki parçanın kendi Rigidbody'si (gravity vb.) bozulmasın.
        bool isPickupable = GetComponent<PickupableCarPart>() != null;

        if (!isPickupable)
        {
            // ─── 1) Kapıya Kinematic Rigidbody ekle ───
            // Kapının collider'ları arabanın ana Rigidbody'sine bağlı olduğu için,
            // kapı oyuncunun içine girdiğinde fizik motoru TÜM kuvveti arabanın Rigidbody'sine
            // uyguluyordu ve bu da arabanın takla atmasına neden oluyordu.
            Rigidbody rb = GetComponent<Rigidbody>();
            if (rb == null)
            {
                rb = gameObject.AddComponent<Rigidbody>();
            }
            rb.isKinematic = true;
            rb.useGravity = false;

            // ─── 2) Kapı <-> Araba gövdesi çarpışmalarını yoksay ───
            doorColliders = GetComponentsInChildren<Collider>();
            Collider[] allRootColliders = transform.root.GetComponentsInChildren<Collider>();

            int carColCount = 0;
            foreach (var c in allRootColliders)
            {
                if (!c.transform.IsChildOf(transform)) carColCount++;
            }
            carBodyColliders = new Collider[carColCount];
            int idx = 0;
            foreach (var c in allRootColliders)
            {
                if (!c.transform.IsChildOf(transform))
                {
                    carBodyColliders[idx++] = c;
                }
            }

            foreach (var dCol in doorColliders)
            {
                foreach (var carCol in carBodyColliders)
                {
                    Physics.IgnoreCollision(dCol, carCol, true);
                }
            }
        }

        // ─── 3) Menteşe (hinge) ayarı ───
        if (hingePoint != null)
        {
            Transform originalParent = transform.parent;
            hingePoint.SetParent(originalParent);
            transform.SetParent(hingePoint);
            rotationTarget = hingePoint;
        }
        else
        {
            rotationTarget = transform;
        }

        initialRotation = rotationTarget.localRotation;
        currentAngle = minAngle;
        targetAngle = minAngle;
    }

    private void Update()
    {
        if (Mathf.Approximately(currentAngle, targetAngle)) return;

        // ─── Kapıyı döndürmeden ÖNCE overlap kontrolü yap ───
        // Yeni açıyı hesapla
        float newAngle = Mathf.Lerp(currentAngle, targetAngle, smoothSpeed * Time.deltaTime);
        if (Mathf.Abs(newAngle - targetAngle) < 0.01f) newAngle = targetAngle;

        // Rotasyonu geçici olarak uygula
        Quaternion newRotation = initialRotation * Quaternion.AngleAxis(newAngle, rotationAxis);
        Quaternion oldRotation = rotationTarget.localRotation;
        rotationTarget.localRotation = newRotation;

        // Fizik transform'larını güncelle ki overlap kontrolü doğru çalışsın
        Physics.SyncTransforms();

        // Her bir kapı collider'ı için dış dünya ile çakışma var mı kontrol et
        if (IsOverlappingExternalObject())
        {
            // Çakışma var! Rotasyonu geri al, kapıyı durdur
            rotationTarget.localRotation = oldRotation;
            Physics.SyncTransforms();
            targetAngle = currentAngle;
            return;
        }

        // Çakışma yok, yeni açıyı uygula
        currentAngle = newAngle;
    }

    /// <summary>
    /// Kapının collider'larının araba dışı herhangi bir obje (oyuncu, duvar vs.) ile
    /// çakışıp çakışmadığını kontrol eder.
    /// CharacterController standart OnCollision callback'lerini tetiklemediği için,
    /// proaktif overlap kontrolü yapıyoruz.
    /// </summary>
    private bool IsOverlappingExternalObject()
    {
        if (doorColliders == null) return false;

        foreach (var col in doorColliders)
        {
            if (col == null || !col.enabled) continue;

            Bounds bounds = col.bounds;
            // Küçültülmüş boyut kullanarak hassasiyet sağlıyoruz
            Vector3 halfExtents = bounds.extents * 0.9f;

            Collider[] overlaps = Physics.OverlapBox(
                bounds.center,
                halfExtents,
                col.transform.rotation,
                ~0, // Tüm layer'lar
                QueryTriggerInteraction.Ignore
            );

            foreach (var overlap in overlaps)
            {
                if (overlap == null) continue;

                // Kapının kendi collider'larını atla
                if (IsOwnCollider(overlap)) continue;

                // Arabanın gövde collider'larını atla (zaten IgnoreCollision ile devre dışı ama array kontrolü de yapalım)
                if (IsCarBodyCollider(overlap)) continue;

                // Dış dünya objesi ile çakışma bulundu!
                return true;
            }
        }

        return false;
    }

    private bool IsOwnCollider(Collider col)
    {
        if (doorColliders == null) return false;
        foreach (var dc in doorColliders)
        {
            if (dc == col) return true;
        }
        return false;
    }

    private bool IsCarBodyCollider(Collider col)
    {
        if (carBodyColliders == null) return false;
        foreach (var cc in carBodyColliders)
        {
            if (cc == col) return true;
        }
        return false;
    }

    public void DragDoor(Vector2 mouseDelta)
    {
        targetAngle += mouseDelta.x * dragSensitivity;
        targetAngle = Mathf.Clamp(targetAngle, minAngle, maxAngle);
    }

    public void ToggleOpen()
    {
        float distToMin = Mathf.Abs(currentAngle - minAngle);
        float distToMax = Mathf.Abs(currentAngle - maxAngle);

        targetAngle = (distToMin < distToMax) ? maxAngle : minAngle;
    }

    public void StopDoor()
    {
        if (rotationTarget == null) return;
        
        // Kapıyı çarptığı yerde anında durdur
        targetAngle = currentAngle;
        rotationTarget.localRotation = initialRotation * Quaternion.AngleAxis(currentAngle, rotationAxis);
    }
}
