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
    [SerializeField] private bool invertDrag = false;

    [Header("Bağlantılar")]
    [SerializeField] private CarPartSlot linkedSlot;

    private float currentAngle;
    private float targetAngle;
    private Quaternion initialRotation;
    private Transform rotationTarget;

    private Collider[] doorColliders;
    private Collider[] carBodyColliders;
    
    private static Collider[] overlapResults = new Collider[10];

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
        bool isPickupable = GetComponent<PickupableCarPart>() != null;

        if (!isPickupable)
        {
            // Kapıya Kinematic Rigidbody ekle
            Rigidbody rb = GetComponent<Rigidbody>();
            if (rb == null)
            {
                rb = gameObject.AddComponent<Rigidbody>();
            }
            rb.isKinematic = true;
            rb.useGravity = false;

            // Kapı ve araba gövdesi çarpışmalarını yoksay
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

        // Menteşe ayarı
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

        float newAngle = Mathf.Lerp(currentAngle, targetAngle, smoothSpeed * Time.deltaTime);
        if (Mathf.Abs(newAngle - targetAngle) < 0.01f) newAngle = targetAngle;

        Quaternion newRotation = initialRotation * Quaternion.AngleAxis(newAngle, rotationAxis);
        Quaternion oldRotation = rotationTarget.localRotation;
        rotationTarget.localRotation = newRotation;

        Physics.SyncTransforms();

        if (IsOverlappingExternalObject())
        {
            rotationTarget.localRotation = oldRotation;
            Physics.SyncTransforms();
            targetAngle = currentAngle;
            return;
        }

        currentAngle = newAngle;
    }

    private bool IsOverlappingExternalObject()
    {
        if (doorColliders == null) return false;

        foreach (var col in doorColliders)
        {
            if (col == null || !col.enabled) continue;

            Bounds bounds = col.bounds;
            Vector3 halfExtents = bounds.extents * 0.9f;

            int count = Physics.OverlapBoxNonAlloc(
                bounds.center,
                halfExtents,
                overlapResults,
                Quaternion.identity, 
                ~0, 
                QueryTriggerInteraction.Ignore
            );

            for (int i = 0; i < count; i++)
            {
                Collider overlap = overlapResults[i];
                if (overlap == null) continue;

                if (IsOwnCollider(overlap)) continue;
                if (IsCarBodyCollider(overlap)) continue;

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
        float dragAmount = mouseDelta.x * dragSensitivity;
        if (invertDrag) dragAmount = -dragAmount;

        // Gerçekçi hissiyat: Sınırlara yaklaştıkça kapının ağırlaşması (direnç)
        float resistance = 1f;
        float margin = 15f; // Sınırlara 15 derece kala direnç başlar

        if (dragAmount > 0 && targetAngle > maxAngle - margin)
        {
            resistance = Mathf.Clamp01((maxAngle - targetAngle) / margin);
        }
        else if (dragAmount < 0 && targetAngle < minAngle + margin)
        {
            resistance = Mathf.Clamp01((targetAngle - minAngle) / margin);
        }

        // Tamamen kilitlenmemesi için minimum %10 hız
        resistance = Mathf.Max(resistance, 0.1f);

        targetAngle += dragAmount * resistance;
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
        
        targetAngle = currentAngle;
        rotationTarget.localRotation = initialRotation * Quaternion.AngleAxis(currentAngle, rotationAxis);
    }
}