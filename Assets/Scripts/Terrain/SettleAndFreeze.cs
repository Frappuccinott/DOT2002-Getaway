using UnityEngine;

public class SettleAndFreeze : MonoBehaviour
{
    public float settleTime = 3f;

    float timer;

    void Update()
    {
        timer += Time.deltaTime;
        if (timer >= settleTime)
        {
            foreach (var rb in GetComponentsInChildren<Rigidbody>())
            {
                rb.isKinematic = true;
            }
            Destroy(this);
        }
    }
}
