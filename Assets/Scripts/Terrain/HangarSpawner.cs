using UnityEngine;

public class HangarSpawner : MonoBehaviour
{
    [Header("Hangar Prefab")]
    public GameObject hangarPrefab;

    [Header("Spawn Position")]
    public float distanceFromRoad = 25f;
    public float spawnZ = 10f;
    public bool placeOnRightSide = true;

    [Header("Yükseklik Ayarı")]
    public float heightOffset = 0.5f;

    [Header("Güvenlik & Temizlik")]
    public float clearanceRadius = 60f;

    [Header("Oyuncu ve Araba Işınlama")]
    public Vector3 playerLocalOffset = new Vector3(-2f, 1f, -2f);
    public Vector3 carLocalOffset = new Vector3(0f, 0.5f, 0f);

    [Header("Debug")]
    [SerializeField] private bool showGizmos = true;

    RoadGenerator roadGen;
    GameObject spawnedHangar;
    bool hasSpawned = false;

    public Vector3 GetExpectedHangarPosition()
    {
        RoadGenerator rg = GetComponent<RoadGenerator>();
        if (rg == null) return Vector3.zero;

        float roadCenterX = rg.GetRoadCenterX(spawnZ);
        float halfRoad = rg.roadWidth * 0.5f;
        float side = placeOnRightSide ? 1f : -1f;
        float worldX = roadCenterX + side * (halfRoad + distanceFromRoad);
        return new Vector3(worldX, 0f, spawnZ);
    }

    public void SpawnHangar()
    {
        if (hasSpawned) return;
        if (hangarPrefab == null) return;

        roadGen = GetComponent<RoadGenerator>();
        if (roadGen == null) return;

        float worldZ = spawnZ;
        float roadCenterX = roadGen.GetRoadCenterX(worldZ);
        float halfRoad = roadGen.roadWidth * 0.5f;
        float side = placeOnRightSide ? 1f : -1f;
        float worldX = roadCenterX + side * (halfRoad + distanceFromRoad);

        float terrainY = GetTerrainHeight(worldX, worldZ);
        Vector3 spawnPos = new Vector3(worldX, terrainY + heightOffset, worldZ);

        Vector3 toRoad = new Vector3(roadCenterX - worldX, 0f, 0f).normalized;
        Quaternion rotation = Quaternion.LookRotation(toRoad, Vector3.up);

        spawnedHangar = Instantiate(hangarPrefab, spawnPos, rotation);
        spawnedHangar.name = "Hangar_Start";

        TeleportPlayerAndCarToHangar();
        hasSpawned = true;
    }

    private void TeleportPlayerAndCarToHangar()
    {
        if (spawnedHangar == null) return;

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            CharacterController cc = player.GetComponent<CharacterController>();
            if (cc != null) cc.enabled = false;
            player.transform.position = spawnedHangar.transform.position + spawnedHangar.transform.TransformDirection(playerLocalOffset);
            player.transform.rotation = spawnedHangar.transform.rotation;
            if (cc != null) cc.enabled = true;
        }

        GameObject car = GameObject.FindGameObjectWithTag("Car");
        if (car != null)
        {
            Rigidbody carRb = car.GetComponent<Rigidbody>();
            if (carRb != null)
            {
                carRb.linearVelocity = Vector3.zero;
                carRb.angularVelocity = Vector3.zero;
            }
            car.transform.position = spawnedHangar.transform.position + spawnedHangar.transform.TransformDirection(carLocalOffset);
            car.transform.rotation = spawnedHangar.transform.rotation;
        }
    }

    float GetTerrainHeight(float worldX, float worldZ)
    {
        Vector3 worldPos = new Vector3(worldX, 0f, worldZ);

        foreach (Terrain t in Terrain.activeTerrains)
        {
            Vector3 tPos = t.transform.position;
            Vector3 tSize = t.terrainData.size;
            if (worldX >= tPos.x && worldX <= tPos.x + tSize.x &&
                worldZ >= tPos.z && worldZ <= tPos.z + tSize.z)
            {
                return t.SampleHeight(worldPos) + tPos.y;
            }
        }

        float bestDist = float.MaxValue;
        float bestHeight = 0f;
        foreach (Terrain t in Terrain.activeTerrains)
        {
            Vector3 tCenter = t.transform.position + t.terrainData.size * 0.5f;
            float dist = Vector2.Distance(new Vector2(worldX, worldZ), new Vector2(tCenter.x, tCenter.z));
            if (dist < bestDist)
            {
                bestDist = dist;
                bestHeight = t.SampleHeight(worldPos) + t.transform.position.y;
            }
        }

        return bestDist < float.MaxValue ? bestHeight : 0f;
    }

    [ContextMenu("Force Respawn Hangar")]
    public void ForceRespawn()
    {
        if (spawnedHangar != null)
            SafeDestroy(spawnedHangar);
        hasSpawned = false;
        SpawnHangar();
    }

    void OnDrawGizmos()
    {
        if (!showGizmos) return;
        RoadGenerator rg = GetComponent<RoadGenerator>();
        if (rg == null) return;

        float worldZ = spawnZ;
        float roadCenterX = rg.GetRoadCenterX(worldZ);
        float halfRoad = rg.roadWidth * 0.5f;
        float side = placeOnRightSide ? 1f : -1f;
        float worldX = roadCenterX + side * (halfRoad + distanceFromRoad);
        Vector3 pos = new Vector3(worldX, 5f, worldZ);

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireCube(pos, new Vector3(10f, 6f, 15f));
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(pos, new Vector3(roadCenterX, 5f, worldZ));
    }

    private void SafeDestroy(GameObject obj)
    {
#if UNITY_EDITOR
        if (UnityEditor.Selection.activeGameObject != null)
        {
            if (UnityEditor.Selection.activeGameObject == obj || UnityEditor.Selection.activeGameObject.transform.IsChildOf(obj.transform))
                UnityEditor.Selection.activeGameObject = null;
        }
#endif
        if (Application.isPlaying) Destroy(obj);
        else DestroyImmediate(obj);
    }
}
