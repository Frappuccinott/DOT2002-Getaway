using UnityEngine;

public class SettlePhysics : MonoBehaviour
{
    private Rigidbody rb;
    private float stationaryTime = 0f;
    private bool isSettled = false;

    public float timeToSettle = 1.0f;
    public bool destroyRigidbody = true;
    public float forceSettleAfter = 5.0f;
    private float lifetime = 0f;

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        if (rb == null) { Destroy(this); return; }
        
        // As ObjectPlacer already raycasts to the ground perfectly, 
        // there is no need for slow physics settling which kills FPS.
        Settle();
    }

    private void Update()
    {
        if (isSettled || rb == null) return;

        lifetime += Time.deltaTime;

        if (rb.linearVelocity.sqrMagnitude < 0.05f && rb.angularVelocity.sqrMagnitude < 0.05f)
            stationaryTime += Time.deltaTime;
        else
            stationaryTime = 0f;

        if (stationaryTime >= timeToSettle || lifetime >= forceSettleAfter)
            Settle();
    }

    private void Settle()
    {
        isSettled = true;
        if (rb != null)
        {
            if (destroyRigidbody) Destroy(rb);
            else { rb.isKinematic = true; rb.useGravity = false; }
        }
        gameObject.isStatic = true;
        Destroy(this);
    }
}
