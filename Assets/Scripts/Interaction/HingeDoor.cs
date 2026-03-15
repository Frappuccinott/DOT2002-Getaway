using UnityEngine;

// [RequireComponent(typeof(Collider))]
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

    private void Awake()
    {
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

        currentAngle = Mathf.Lerp(currentAngle, targetAngle, smoothSpeed * Time.deltaTime);
        if (Mathf.Abs(currentAngle - targetAngle) < 0.01f) currentAngle = targetAngle;

        rotationTarget.localRotation = initialRotation * Quaternion.AngleAxis(currentAngle, rotationAxis);
    }

    public void DragDoor(Vector2 mouseDelta)
    {
        targetAngle += mouseDelta.x * dragSensitivity;
        targetAngle = Mathf.Clamp(targetAngle, minAngle, maxAngle);
    }

    public void ToggleOpen()
    {
        float mid = (minAngle + maxAngle) / 2f;
        targetAngle = currentAngle < mid ? maxAngle : minAngle;
    }
}
