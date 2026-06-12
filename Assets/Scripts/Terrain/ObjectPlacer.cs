using UnityEngine;
using System.Collections.Generic;

public class ObjectPlacer : MonoBehaviour
{
    [Header("Placement Settings")]
    public float minDistanceForGround = 15f;
    public float minDistanceForRocks = 15f;
    public float minDistanceForTrees = 4f;
    public float roadAvoidanceThreshold = 0.25f;

    [Header("Structure Settings")]
    public float minDistanceBetweenStructures = 80f;
    public float minDistanceFromOtherObjects = 15f;

    [Header("Structure Loot")]
    public StructureLootTable structureLootTable;

    int globalSeed;
    HangarSpawner hangarSpawner;

    public void Initialize(int seed)
    {
        globalSeed = seed;
        hangarSpawner = GetComponent<HangarSpawner>();
    }

    public System.Collections.IEnumerator PlaceChunkObjectsCoroutine(TerrainChunk chunk, int chunkSize,
        BiomeDefinition[] biomes, BiomeDefinition[,] biomeMap, float[,] roadMask, RoadGenerator roadGen)
    {
        if (chunk.objectsParent != null)
            SafeDestroy(chunk.objectsParent);

        chunk.objectsParent = new GameObject($"Objects_{chunk.coord.x}_{chunk.coord.y}");
        chunk.objectsParent.transform.SetParent(chunk.gameObject.transform);
        chunk.objectsParent.transform.localPosition = Vector3.zero;

        int chunkSeed = globalSeed + chunk.coord.x * 73856093 + chunk.coord.y * 19349663;
        System.Random rng = new System.Random(chunkSeed);

        int biomeResX = biomeMap.GetLength(1);
        int biomeResZ = biomeMap.GetLength(0);
        int maskRes = roadMask.GetLength(0);

        float chunkWorldX = chunk.coord.x * chunkSize;
        float chunkWorldZ = chunk.coord.y * chunkSize;

        List<Vector3> allPlacedPositions = new List<Vector3>();

        foreach (BiomeDefinition biome in biomes)
        {
            List<Vector2> positions = GetBiomePositions(biome, biomeMap, biomeResX, biomeResZ);
            if (positions.Count == 0) continue;

            yield return StartCoroutine(PlaceCategoryCoroutine(biome.groundObjects, biome.groundObjectCount, positions,
                roadMask, maskRes, rng, biome, "Ground", chunk, chunkSize, chunkWorldX, chunkWorldZ, allPlacedPositions));
            yield return StartCoroutine(PlaceCategoryCoroutine(biome.rockObjects, biome.rockObjectCount, positions,
                roadMask, maskRes, rng, biome, "Rock", chunk, chunkSize, chunkWorldX, chunkWorldZ, allPlacedPositions));
            yield return StartCoroutine(PlaceCategoryCoroutine(biome.treeObjects, biome.treeObjectCount, positions,
                roadMask, maskRes, rng, biome, "Tree", chunk, chunkSize, chunkWorldX, chunkWorldZ, allPlacedPositions));
        }

        chunk.terrain.Flush();
        yield return StartCoroutine(PlaceChunkStructuresCoroutine(chunk, chunkSize, biomes, biomeMap, roadGen, rng, chunkWorldX, chunkWorldZ, allPlacedPositions));
    }

    List<Vector2> GetBiomePositions(BiomeDefinition biome, BiomeDefinition[,] biomeMap, int resX, int resZ)
    {
        List<Vector2> positions = new List<Vector2>();
        for (int z = 0; z < resZ; z++)
            for (int x = 0; x < resX; x++)
                if (biomeMap[z, x] == biome)
                    positions.Add(new Vector2((float)x / (resX - 1), (float)z / (resZ - 1)));
        return positions;
    }

    System.Collections.IEnumerator PlaceCategoryCoroutine(GameObject[] prefabs, int count, List<Vector2> biomePositions,
        float[,] roadMask, int maskRes, System.Random rng, BiomeDefinition biome, string category,
        TerrainChunk chunk, int chunkSize, float chunkWorldX, float chunkWorldZ,
        List<Vector3> allPlacedPositions)
    {
        if (prefabs == null || prefabs.Length == 0 || count <= 0) yield break;

        List<GameObject> validPrefabs = new List<GameObject>();
        foreach (var p in prefabs)
            if (p != null) validPrefabs.Add(p);
        if (validPrefabs.Count == 0) yield break;

        Transform parent = new GameObject($"{biome.biomeName}_{category}").transform;
        parent.SetParent(chunk.objectsParent.transform);

        List<Vector3> placed = new List<Vector3>();
        int attempts = 0;
        int maxAttempts = count * 30;

        while (placed.Count < count && attempts < maxAttempts)
        {
            attempts++;

            int posIndex = rng.Next(biomePositions.Count);
            Vector2 basePos = biomePositions[posIndex];

            float normX = Mathf.Clamp01(basePos.x + (float)(rng.NextDouble() * 2.0 - 1.0) * 0.03f);
            float normZ = Mathf.Clamp01(basePos.y + (float)(rng.NextDouble() * 2.0 - 1.0) * 0.03f);

            int mx = Mathf.Clamp(Mathf.FloorToInt(normX * (maskRes - 1)), 0, maskRes - 1);
            int mz = Mathf.Clamp(Mathf.FloorToInt(normZ * (maskRes - 1)), 0, maskRes - 1);
            if (roadMask[mz, mx] > roadAvoidanceThreshold) continue;

            float localX = normX * chunkSize;
            float localZ = normZ * chunkSize;
            float worldX = chunkWorldX + localX;
            float worldZ = chunkWorldZ + localZ;

            Vector3 worldSamplePoint = new Vector3(worldX, 0, worldZ);
            float terrainY = chunk.terrain.SampleHeight(worldSamplePoint);
            float worldY = terrainY + chunk.gameObject.transform.position.y;
            Vector3 worldPos = new Vector3(worldX, worldY, worldZ);

            if (hangarSpawner != null)
            {
                Vector3 hPos = hangarSpawner.GetExpectedHangarPosition();
                float distToHangar = Vector2.Distance(new Vector2(worldX, worldZ), new Vector2(hPos.x, hPos.z));
                if (distToHangar < hangarSpawner.clearanceRadius) continue;
            }

            float minDist = minDistanceForGround;
            if (category == "Rock") minDist = minDistanceForRocks;
            else if (category == "Tree") minDist = minDistanceForTrees;

            bool tooClose = false;
            foreach (Vector3 p in placed)
            {
                if (Vector3.Distance(p, worldPos) < minDist)
                { tooClose = true; break; }
            }
            if (tooClose) continue;

            GameObject prefab = validPrefabs[rng.Next(validPrefabs.Count)];
            Vector3 spawnPos = new Vector3(worldX, worldY, worldZ);

            if (Physics.Raycast(new Vector3(worldX, worldY + 50f, worldZ), Vector3.down, out RaycastHit hit, 200f))
                spawnPos.y = hit.point.y;

            GameObject obj = Instantiate(prefab, spawnPos, Quaternion.identity, parent);

            if (biome.randomRotation)
                obj.transform.rotation = Quaternion.Euler(0, (float)rng.NextDouble() * 360f, 0);

            if (obj.GetComponent<Rigidbody>() != null)
                obj.AddComponent<SettlePhysics>();

            foreach (Rigidbody rb in obj.GetComponentsInChildren<Rigidbody>())
            {
                if (rb.gameObject.GetComponent<SettlePhysics>() == null)
                    rb.gameObject.AddComponent<SettlePhysics>();
            }

            float scaleMult = biome.scaleRange.x + (float)rng.NextDouble() * (biome.scaleRange.y - biome.scaleRange.x);
            obj.transform.localScale = prefab.transform.localScale * scaleMult;

            placed.Add(worldPos);
            allPlacedPositions.Add(worldPos);
            if (placed.Count % 5 == 0) yield return null;
        }
    }

    System.Collections.IEnumerator PlaceChunkStructuresCoroutine(TerrainChunk chunk, int chunkSize,
        BiomeDefinition[] biomes, BiomeDefinition[,] biomeMap, RoadGenerator roadGen, System.Random rng,
        float chunkWorldX, float chunkWorldZ, List<Vector3> allPlacedPositions)
    {
        Transform structureParent = new GameObject("Structures").transform;
        structureParent.SetParent(chunk.objectsParent.transform);

        List<Vector3> structurePositions = new List<Vector3>();
        int biomeResX = biomeMap.GetLength(1);
        int biomeResZ = biomeMap.GetLength(0);

        foreach (BiomeDefinition biome in biomes)
        {
            if (biome.structureObjects == null || biome.structureObjects.Length == 0 || biome.structureCount <= 0)
                continue;

            List<GameObject> validStructurePrefabs = new List<GameObject>();
            foreach (var p in biome.structureObjects)
                if (p != null) validStructurePrefabs.Add(p);
            if (validStructurePrefabs.Count == 0) continue;

            int placed = 0;
            int attempts = 0;
            int maxAttempts = biome.structureCount * 40;

            while (placed < biome.structureCount && attempts < maxAttempts)
            {
                attempts++;

                float normX = (float)rng.NextDouble();
                float normZ = (float)rng.NextDouble();
                float worldX = chunkWorldX + normX * chunkSize;
                float worldZ = chunkWorldZ + normZ * chunkSize;

                int bx = Mathf.Clamp(Mathf.FloorToInt(normX * biomeResX), 0, biomeResX - 1);
                int bz = Mathf.Clamp(Mathf.FloorToInt(normZ * biomeResZ), 0, biomeResZ - 1);
                if (biomeMap[bz, bx] != biome) continue;

                float distFromRoad = roadGen.GetDistanceFromRoad(worldX, worldZ);
                if (distFromRoad < biome.structureMinRoadDistance) continue;
                if (distFromRoad > biome.structureMaxRoadDistance) continue;

                Vector3 worldSamplePoint = new Vector3(worldX, 0, worldZ);
                float terrainY = chunk.terrain.SampleHeight(worldSamplePoint);
                float worldY = terrainY + chunk.gameObject.transform.position.y;
                Vector3 worldPos = new Vector3(worldX, worldY, worldZ);

                if (hangarSpawner != null)
                {
                    Vector3 hPos = hangarSpawner.GetExpectedHangarPosition();
                    float distToHangar = Vector2.Distance(new Vector2(worldX, worldZ), new Vector2(hPos.x, hPos.z));
                    if (distToHangar < hangarSpawner.clearanceRadius) continue;
                }

                bool tooClose = false;
                foreach (Vector3 p in structurePositions)
                {
                    if (Vector3.Distance(p, worldPos) < minDistanceBetweenStructures)
                    { tooClose = true; break; }
                }
                if (tooClose) continue;

                bool overlapsOther = false;
                foreach (Vector3 p in allPlacedPositions)
                {
                    if (Vector3.Distance(p, worldPos) < minDistanceFromOtherObjects)
                    { overlapsOther = true; break; }
                }
                if (overlapsOther) continue;

                GameObject prefab = validStructurePrefabs[rng.Next(validStructurePrefabs.Count)];
                Vector3 spawnPos = worldPos;
                if (Physics.Raycast(new Vector3(worldX, worldY + 50f, worldZ), Vector3.down, out RaycastHit hit, 200f))
                    spawnPos.y = hit.point.y;

                GameObject obj = Instantiate(prefab, spawnPos,
                    Quaternion.Euler(0, (float)rng.NextDouble() * 360f, 0), structureParent);

                obj.isStatic = true;
                obj.AddComponent<ZombieSpawnZone>();

                if (obj.GetComponent<Rigidbody>() != null)
                    obj.AddComponent<SettlePhysics>();

                foreach (Rigidbody rb in obj.GetComponentsInChildren<Rigidbody>())
                {
                    if (rb.gameObject.GetComponent<SettlePhysics>() == null)
                        rb.gameObject.AddComponent<SettlePhysics>();
                }

                if (structureLootTable != null)
                    StructureLootSpawner.SpawnLoot(obj, structureLootTable, rng, chunk.terrain);

                structurePositions.Add(worldPos);
                allPlacedPositions.Add(worldPos);
                placed++;
                if (placed % 2 == 0) yield return null;
            }
        }
    }

    public void PlaceChunkObjects(TerrainChunk chunk, int chunkSize,
        BiomeDefinition[] biomes, BiomeDefinition[,] biomeMap, float[,] roadMask, RoadGenerator roadGen)
    {
        if (chunk.objectsParent != null) SafeDestroy(chunk.objectsParent);
        chunk.objectsParent = new GameObject($"Objects_{chunk.coord.x}_{chunk.coord.y}");
        chunk.objectsParent.transform.SetParent(chunk.gameObject.transform);
        chunk.objectsParent.transform.localPosition = Vector3.zero;

        int chunkSeed = globalSeed + chunk.coord.x * 73856093 + chunk.coord.y * 19349663;
        System.Random rng = new System.Random(chunkSeed);

        int biomeResX = biomeMap.GetLength(1);
        int biomeResZ = biomeMap.GetLength(0);
        int maskRes = roadMask.GetLength(0);

        float chunkWorldX = chunk.coord.x * chunkSize;
        float chunkWorldZ = chunk.coord.y * chunkSize;

        List<Vector3> allPlacedPositions = new List<Vector3>();

        foreach (BiomeDefinition biome in biomes)
        {
            List<Vector2> positions = GetBiomePositions(biome, biomeMap, biomeResX, biomeResZ);
            if (positions.Count == 0) continue;

            PlaceCategorySync(biome.groundObjects, biome.groundObjectCount, positions, roadMask, maskRes, rng, biome, "Ground", chunk, chunkSize, chunkWorldX, chunkWorldZ, allPlacedPositions);
            PlaceCategorySync(biome.rockObjects, biome.rockObjectCount, positions, roadMask, maskRes, rng, biome, "Rock", chunk, chunkSize, chunkWorldX, chunkWorldZ, allPlacedPositions);
            PlaceCategorySync(biome.treeObjects, biome.treeObjectCount, positions, roadMask, maskRes, rng, biome, "Tree", chunk, chunkSize, chunkWorldX, chunkWorldZ, allPlacedPositions);
        }

        chunk.terrain.Flush();
        PlaceChunkStructuresSync(chunk, chunkSize, biomes, biomeMap, roadGen, rng, chunkWorldX, chunkWorldZ, allPlacedPositions);
    }

    void PlaceCategorySync(GameObject[] prefabs, int count, List<Vector2> biomePositions,
        float[,] roadMask, int maskRes, System.Random rng, BiomeDefinition biome, string category,
        TerrainChunk chunk, int chunkSize, float chunkWorldX, float chunkWorldZ, List<Vector3> allPlacedPositions)
    {
        if (prefabs == null || prefabs.Length == 0 || count <= 0) return;
        List<GameObject> validPrefabs = new List<GameObject>();
        foreach (var p in prefabs) if (p != null) validPrefabs.Add(p);
        if (validPrefabs.Count == 0) return;

        Transform parent = new GameObject($"{biome.biomeName}_{category}").transform;
        parent.SetParent(chunk.objectsParent.transform);
        List<Vector3> placed = new List<Vector3>();
        int attempts = 0;
        int maxAttempts = count * 30;

        while (placed.Count < count && attempts < maxAttempts)
        {
            attempts++;
            int posIndex = rng.Next(biomePositions.Count);
            Vector2 basePos = biomePositions[posIndex];
            float normX = Mathf.Clamp01(basePos.x + (float)(rng.NextDouble() * 2.0 - 1.0) * 0.03f);
            float normZ = Mathf.Clamp01(basePos.y + (float)(rng.NextDouble() * 2.0 - 1.0) * 0.03f);
            int mx = Mathf.Clamp(Mathf.FloorToInt(normX * (maskRes - 1)), 0, maskRes - 1);
            int mz = Mathf.Clamp(Mathf.FloorToInt(normZ * (maskRes - 1)), 0, maskRes - 1);
            if (roadMask[mz, mx] > roadAvoidanceThreshold) continue;

            float localX = normX * chunkSize;
            float localZ = normZ * chunkSize;
            float worldX = chunkWorldX + localX;
            float worldZ = chunkWorldZ + localZ;

            if (hangarSpawner != null)
            {
                Vector3 hPos = hangarSpawner.GetExpectedHangarPosition();
                if (Vector2.Distance(new Vector2(worldX, worldZ), new Vector2(hPos.x, hPos.z)) < hangarSpawner.clearanceRadius) continue;
            }

            float minDist = category == "Rock" ? minDistanceForRocks : (category == "Tree" ? minDistanceForTrees : minDistanceForGround);
            Vector3 worldPos = new Vector3(worldX, chunk.terrain.SampleHeight(new Vector3(worldX, 0, worldZ)) + chunk.gameObject.transform.position.y, worldZ);
            bool tooClose = false;
            foreach (Vector3 p in placed) if (Vector3.Distance(p, worldPos) < minDist) { tooClose = true; break; }
            if (tooClose) continue;

            GameObject prefab = validPrefabs[rng.Next(validPrefabs.Count)];
            Vector3 spawnPos = worldPos;
            if (Physics.Raycast(new Vector3(worldX, worldPos.y + 50f, worldZ), Vector3.down, out RaycastHit hit, 200f)) spawnPos.y = hit.point.y;

            GameObject obj = Instantiate(prefab, spawnPos, biome.randomRotation ? Quaternion.Euler(0, (float)rng.NextDouble() * 360f, 0) : Quaternion.identity, parent);
            if (obj.GetComponent<Rigidbody>() != null) obj.AddComponent<SettlePhysics>();
            foreach (Rigidbody rb in obj.GetComponentsInChildren<Rigidbody>()) if (rb.gameObject.GetComponent<SettlePhysics>() == null) rb.gameObject.AddComponent<SettlePhysics>();

            float scaleMult = biome.scaleRange.x + (float)rng.NextDouble() * (biome.scaleRange.y - biome.scaleRange.x);
            obj.transform.localScale = prefab.transform.localScale * scaleMult;

            placed.Add(worldPos);
            allPlacedPositions.Add(worldPos);
        }
    }

    void PlaceChunkStructuresSync(TerrainChunk chunk, int chunkSize, BiomeDefinition[] biomes, BiomeDefinition[,] biomeMap, RoadGenerator roadGen, System.Random rng, float chunkWorldX, float chunkWorldZ, List<Vector3> allPlacedPositions)
    {
        Transform structureParent = new GameObject("Structures").transform;
        structureParent.SetParent(chunk.objectsParent.transform);
        List<Vector3> structurePositions = new List<Vector3>();
        int biomeResX = biomeMap.GetLength(1);
        int biomeResZ = biomeMap.GetLength(0);

        foreach (BiomeDefinition biome in biomes)
        {
            if (biome.structureObjects == null || biome.structureObjects.Length == 0 || biome.structureCount <= 0) continue;
            List<GameObject> validStructurePrefabs = new List<GameObject>();
            foreach (var p in biome.structureObjects) if (p != null) validStructurePrefabs.Add(p);
            if (validStructurePrefabs.Count == 0) continue;

            int placed = 0;
            int attempts = 0;
            int maxAttempts = biome.structureCount * 40;

            while (placed < biome.structureCount && attempts < maxAttempts)
            {
                attempts++;
                float normX = (float)rng.NextDouble();
                float normZ = (float)rng.NextDouble();
                float worldX = chunkWorldX + normX * chunkSize;
                float worldZ = chunkWorldZ + normZ * chunkSize;
                int bx = Mathf.Clamp(Mathf.FloorToInt(normX * biomeResX), 0, biomeResX - 1);
                int bz = Mathf.Clamp(Mathf.FloorToInt(normZ * biomeResZ), 0, biomeResZ - 1);
                if (biomeMap[bz, bx] != biome) continue;

                float distFromRoad = roadGen.GetDistanceFromRoad(worldX, worldZ);
                if (distFromRoad < biome.structureMinRoadDistance || distFromRoad > biome.structureMaxRoadDistance) continue;

                Vector3 worldPos = new Vector3(worldX, chunk.terrain.SampleHeight(new Vector3(worldX, 0, worldZ)) + chunk.gameObject.transform.position.y, worldZ);

                if (hangarSpawner != null)
                {
                    Vector3 hPos = hangarSpawner.GetExpectedHangarPosition();
                    if (Vector2.Distance(new Vector2(worldX, worldZ), new Vector2(hPos.x, hPos.z)) < hangarSpawner.clearanceRadius) continue;
                }

                bool tooClose = false;
                foreach (Vector3 p in structurePositions) if (Vector3.Distance(p, worldPos) < minDistanceBetweenStructures) { tooClose = true; break; }
                if (tooClose) continue;

                bool overlapsOther = false;
                foreach (Vector3 p in allPlacedPositions) if (Vector3.Distance(p, worldPos) < minDistanceFromOtherObjects) { overlapsOther = true; break; }
                if (overlapsOther) continue;

                GameObject prefab = validStructurePrefabs[rng.Next(validStructurePrefabs.Count)];
                Vector3 spawnPos = worldPos;
                if (Physics.Raycast(new Vector3(worldX, worldPos.y + 50f, worldZ), Vector3.down, out RaycastHit hit, 200f)) spawnPos.y = hit.point.y;

                GameObject obj = Instantiate(prefab, spawnPos, Quaternion.Euler(0, (float)rng.NextDouble() * 360f, 0), structureParent);
                obj.isStatic = true;
                obj.AddComponent<ZombieSpawnZone>();
                if (obj.GetComponent<Rigidbody>() != null) obj.AddComponent<SettlePhysics>();
                foreach (Rigidbody rb in obj.GetComponentsInChildren<Rigidbody>()) if (rb.gameObject.GetComponent<SettlePhysics>() == null) rb.gameObject.AddComponent<SettlePhysics>();

                if (structureLootTable != null) StructureLootSpawner.SpawnLoot(obj, structureLootTable, rng, chunk.terrain);

                structurePositions.Add(worldPos);
                allPlacedPositions.Add(worldPos);
                placed++;
            }
        }
    }

    public void ClearChunkObjects(TerrainChunk chunk)
    {
        if (chunk.objectsParent != null)
        {
            SafeDestroy(chunk.objectsParent);
            chunk.objectsParent = null;
        }
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
