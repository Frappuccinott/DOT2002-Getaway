using UnityEngine;
using UnityEngine.AI;

public class ZombieManager : MonoBehaviour
{
    public static ZombieManager Instance { get; private set; }

    [Header("Zombie Settings")]
    public GameObject zombiePrefab;
    [Range(1, 10)] public int minZombiesPerStructure = 2;
    [Range(1, 20)] public int maxZombiesPerStructure = 5;
    [Range(5f, 50f)] public float spawnRadius = 15f;
    public int poolSize = 50;

    [Header("Trigger Settings")]
    [Range(10f, 200f)] public float triggerDistance = 60f;

    private GameObject spawnPoolParent;
    private System.Collections.Generic.List<GameObject> zombiePool = new System.Collections.Generic.List<GameObject>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        spawnPoolParent = new GameObject("ZombieSpawnPool");
        spawnPoolParent.SetActive(false);
    }

    private System.Collections.IEnumerator Start()
    {
        if (zombiePrefab != null)
        {
            for (int i = 0; i < poolSize; i++)
            {
                GameObject z = Instantiate(zombiePrefab, spawnPoolParent.transform);
                z.SetActive(false);
                zombiePool.Add(z);
                if (i % 2 == 0) yield return null;
            }
        }
    }

    public GameObject GetZombieFromPool()
    {
        foreach (var z in zombiePool)
        {
            if (z != null && !z.activeInHierarchy)
            {
                return z;
            }
        }
        
        GameObject newZ = Instantiate(zombiePrefab, spawnPoolParent.transform);
        newZ.SetActive(false);
        zombiePool.Add(newZ);
        return newZ;
    }

    public bool SpawnZombiesAround(Vector3 center)
    {
        if (zombiePrefab == null) 
        {
            Debug.LogWarning("[ZombieManager] Zombie prefab is null!");
            return false;
        }

        if (!NavMesh.SamplePosition(center, out NavMeshHit centerHit, 50f, NavMesh.AllAreas))
        {
            Debug.LogWarning($"[ZombieManager] Could not find NavMesh near center {center}!");
            return false;
        }

        int count = Random.Range(Mathf.Max(1, minZombiesPerStructure), Mathf.Max(1, maxZombiesPerStructure) + 1);
        Debug.Log($"[ZombieManager] Attempting to spawn {count} zombies around {center}.");
        for (int i = 0; i < count; i++)
        {
            Vector2 randomCircle = Random.insideUnitCircle * spawnRadius;
            Vector3 spawnPos = center + new Vector3(randomCircle.x, 50f, randomCircle.y);
            
            if (Physics.Raycast(spawnPos, Vector3.down, out RaycastHit hit, 100f))
            {
                spawnPos.y = hit.point.y;
            }
            else
            {
                spawnPos.y = center.y;
            }

            if (NavMesh.SamplePosition(spawnPos, out NavMeshHit navHit, 20f, NavMesh.AllAreas))
            {
                if (spawnPoolParent == null)
                {
                    spawnPoolParent = new GameObject("ZombieSpawnPool");
                    spawnPoolParent.SetActive(false);
                }

                GameObject z = GetZombieFromPool();
                
                NavMeshAgent agent = z.GetComponent<NavMeshAgent>();
                if (agent != null) agent.enabled = false;
                
                z.transform.SetParent(null);
                
                if (agent != null)
                {
                    agent.Warp(navHit.position);
                    agent.enabled = true;
                }
                z.SetActive(true);
            }
            else
            {
                Debug.LogWarning($"[ZombieManager] Could not find NavMesh near {spawnPos} for Zombie spawning!");
            }
        }
        
        return true;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0f, 0f, 0.3f);
        Gizmos.DrawWireSphere(transform.position, triggerDistance);
        
        Gizmos.color = new Color(0f, 1f, 0f, 0.3f);
        Gizmos.DrawWireSphere(transform.position, spawnRadius);
    }
}
