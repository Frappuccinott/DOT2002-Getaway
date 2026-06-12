using UnityEngine;

public class ZombieSpawnZone : MonoBehaviour
{
    private Transform player;
    private bool hasTriggered = false;

    private void Start()
    {
        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p != null) player = p.transform;
        else
        {
            Camera cam = Camera.main;
            if (cam != null) player = cam.transform;
        }
    }

    private void Update()
    {
        if (hasTriggered || player == null || ZombieManager.Instance == null) return;

        float dist = Vector3.Distance(transform.position, player.position);
        if (dist <= ZombieManager.Instance.triggerDistance)
        {
            if (ZombieManager.Instance.SpawnZombiesAround(transform.position))
            {
                hasTriggered = true;
                enabled = false;
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        ZombieManager manager = ZombieManager.Instance;
#if UNITY_EDITOR
        if (manager == null) manager = FindFirstObjectByType<ZombieManager>();
#endif
        if (manager != null)
        {
            Gizmos.color = new Color(1f, 0f, 0f, 0.3f);
            Gizmos.DrawWireSphere(transform.position, manager.triggerDistance);
            
            Gizmos.color = new Color(0f, 1f, 0f, 0.3f);
            Gizmos.DrawWireSphere(transform.position, manager.spawnRadius);
        }
    }
}
