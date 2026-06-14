using UnityEngine;
using System.Collections.Generic;
using Unity.AI.Navigation;
using UnityEngine.AI;

public class ChunkManager : MonoBehaviour
{
    [Header("Chunk Settings")]
    public int chunkSize = 250;
    public int viewDistance = 2;
    public float checkInterval = 0.5f;

    [Header("References")]
    public Transform player;
    public Material terrainMaterial;

    MapGenerator mapGenerator;
    Dictionary<Vector2Int, TerrainChunk> activeChunks = new Dictionary<Vector2Int, TerrainChunk>();
    Vector2Int lastPlayerChunk = new Vector2Int(int.MinValue, int.MinValue);
    float nextCheckTime;

    Coroutine updateCoroutine;

    void Start()
    {
        mapGenerator = GetComponent<MapGenerator>();
        if (mapGenerator == null) return;

        mapGenerator.InitializeSeed();
        updateCoroutine = StartCoroutine(UpdateChunksCoroutine());

        HangarSpawner hangarSpawner = GetComponent<HangarSpawner>();
        if (hangarSpawner != null)
            hangarSpawner.SpawnHangar();

        if (player == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
                player = playerObj.transform;
            else
            {
                Camera cam = Camera.main;
                if (cam != null) player = cam.transform;
            }
        }

        Camera mainCam = Camera.main;
        if (mainCam != null)
        {
            mainCam.farClipPlane = 400f;
        }

        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.Linear;
        RenderSettings.fogStartDistance = 150f;
        RenderSettings.fogEndDistance = 400f;
        if (RenderSettings.skybox != null && RenderSettings.skybox.HasProperty("_Tint"))
            RenderSettings.fogColor = RenderSettings.skybox.GetColor("_Tint");
        else
            RenderSettings.fogColor = new Color(0.6f, 0.7f, 0.8f);
    }

    void Update()
    {
        if (Time.time < nextCheckTime) return;
        nextCheckTime = Time.time + checkInterval;

        Vector2Int currentChunk = GetPlayerChunkCoord();
        if (currentChunk != lastPlayerChunk)
        {
            lastPlayerChunk = currentChunk;
            if (updateCoroutine != null) StopCoroutine(updateCoroutine);
            updateCoroutine = StartCoroutine(UpdateChunksCoroutine());
        }
    }

    Vector2Int GetPlayerChunkCoord()
    {
        if (player != null) return WorldToChunkCoord(player.position);
        return Vector2Int.zero;
    }

    System.Collections.IEnumerator UpdateChunksCoroutine()
    {
        Vector2Int playerChunk = GetPlayerChunkCoord();

        HashSet<Vector2Int> neededChunks = new HashSet<Vector2Int>();
        for (int z = -viewDistance; z <= viewDistance; z++)
        {
            for (int x = -viewDistance; x <= viewDistance; x++)
            {
                if (Mathf.RoundToInt(Mathf.Sqrt(x * x + z * z)) <= viewDistance)
                    neededChunks.Add(new Vector2Int(playerChunk.x + x, playerChunk.y + z));
            }
        }

        List<Vector2Int> toRemove = new List<Vector2Int>();
        foreach (var kvp in activeChunks)
        {
            if (!neededChunks.Contains(kvp.Key))
                toRemove.Add(kvp.Key);
        }

        foreach (var coord in toRemove)
            DestroyChunk(coord);

        List<Vector2Int> chunksToCreate = new List<Vector2Int>();
        foreach (var coord in neededChunks)
        {
            if (!activeChunks.ContainsKey(coord))
                chunksToCreate.Add(coord);
        }

        chunksToCreate.Sort((a, b) => {
            float distA = Vector2Int.Distance(a, playerChunk);
            float distB = Vector2Int.Distance(b, playerChunk);
            return distA.CompareTo(distB);
        });

        foreach (var coord in chunksToCreate)
        {
            if (coord == playerChunk)
            {
                CreateChunkSync(coord);
            }
            else
            {
                yield return StartCoroutine(CreateChunkCoroutine(coord));
            }
        }
    }

    void CreateChunkSync(Vector2Int coord)
    {
        Vector3 worldPos = ChunkCoordToWorld(coord);

        GameObject chunkObj = new GameObject($"Chunk_{coord.x}_{coord.y}");
        chunkObj.transform.position = worldPos;
        chunkObj.transform.SetParent(transform);

        Terrain terrain = chunkObj.AddComponent<Terrain>();
        TerrainCollider collider = chunkObj.AddComponent<TerrainCollider>();

        TerrainData terrainData = new TerrainData();
        terrainData.heightmapResolution = 257;
        terrainData.alphamapResolution = 257;
        terrainData.baseMapResolution = 512;
        terrainData.size = new Vector3(chunkSize, 60f, chunkSize);

        terrain.terrainData = terrainData;
        collider.terrainData = terrainData;
        terrain.drawInstanced = true;

        if (terrainMaterial != null)
        {
            terrain.materialTemplate = terrainMaterial;
        }
        else
        {
            Shader terrainShader = Shader.Find("Universal Render Pipeline/Terrain/Lit");
            if (terrainShader == null) terrainShader = Shader.Find("Nature/Terrain/Standard");
            if (terrainShader != null)
            {
                terrainMaterial = new Material(terrainShader);
                terrain.materialTemplate = terrainMaterial;
            }
        }

        TerrainChunk chunk = new TerrainChunk
        {
            coord = coord,
            gameObject = chunkObj,
            terrain = terrain,
            terrainData = terrainData
        };

        activeChunks[coord] = chunk;

        try { mapGenerator.GenerateChunk(chunk, chunkSize); }
        catch (System.Exception e) { Debug.LogError($"[ChunkManager] Error generating chunk {coord}: {e.Message}"); }

        NavMeshSurface navMeshSurface = chunkObj.AddComponent<NavMeshSurface>();
        navMeshSurface.collectObjects = CollectObjects.Children;
        navMeshSurface.useGeometry = NavMeshCollectGeometry.PhysicsColliders;
        
        navMeshSurface.BuildNavMesh();

        SetNeighbors(coord);
    }

    System.Collections.IEnumerator CreateChunkCoroutine(Vector2Int coord)
    {
        Vector3 worldPos = ChunkCoordToWorld(coord);

        GameObject chunkObj = new GameObject($"Chunk_{coord.x}_{coord.y}");
        chunkObj.transform.position = worldPos;
        chunkObj.transform.SetParent(transform);

        Terrain terrain = chunkObj.AddComponent<Terrain>();
        TerrainCollider collider = chunkObj.AddComponent<TerrainCollider>();

        TerrainData terrainData = new TerrainData();
        terrainData.heightmapResolution = 257;
        terrainData.alphamapResolution = 257;
        terrainData.baseMapResolution = 512;
        terrainData.size = new Vector3(chunkSize, 60f, chunkSize);

        terrain.terrainData = terrainData;
        collider.terrainData = terrainData;
        terrain.drawInstanced = true;

        if (terrainMaterial != null)
        {
            terrain.materialTemplate = terrainMaterial;
        }
        else
        {
            Shader terrainShader = Shader.Find("Universal Render Pipeline/Terrain/Lit");
            if (terrainShader == null) terrainShader = Shader.Find("Nature/Terrain/Standard");
            if (terrainShader != null)
            {
                terrainMaterial = new Material(terrainShader);
                terrain.materialTemplate = terrainMaterial;
            }
        }

        TerrainChunk chunk = new TerrainChunk
        {
            coord = coord,
            gameObject = chunkObj,
            terrain = terrain,
            terrainData = terrainData
        };

        activeChunks[coord] = chunk;

        yield return null;

        yield return StartCoroutine(mapGenerator.GenerateChunkCoroutine(chunk, chunkSize));

        yield return null;

        NavMeshSurface navMeshSurface = chunkObj.AddComponent<NavMeshSurface>();
        navMeshSurface.collectObjects = CollectObjects.Children;
        navMeshSurface.useGeometry = NavMeshCollectGeometry.PhysicsColliders;
        
        // Asynchronous NavMesh generation workaround
        var buildSources = new List<NavMeshBuildSource>();
        var markups = new List<NavMeshBuildMarkup>();
        NavMeshBuilder.CollectSources(chunkObj.transform, navMeshSurface.layerMask, navMeshSurface.useGeometry, navMeshSurface.defaultArea, markups, buildSources);
        
        var bounds = new Bounds(chunkObj.transform.position, new Vector3(chunkSize * 2, 1000, chunkSize * 2));
        var buildSettings = navMeshSurface.GetBuildSettings();
        
        NavMeshData navMeshData = new NavMeshData(navMeshSurface.agentTypeID);
        AsyncOperation asyncOp = NavMeshBuilder.UpdateNavMeshDataAsync(navMeshData, buildSettings, buildSources, bounds);
        
        yield return asyncOp;
        
        navMeshSurface.navMeshData = navMeshData;
        navMeshSurface.enabled = false;
        navMeshSurface.enabled = true;

        SetNeighbors(coord);
    }

    void DestroyChunk(Vector2Int coord)
    {
        if (activeChunks.TryGetValue(coord, out TerrainChunk chunk))
        {
            if (chunk.objectsParent != null) Destroy(chunk.objectsParent);
            Destroy(chunk.gameObject);
            activeChunks.Remove(coord);
        }
    }

    void SetNeighbors(Vector2Int coord)
    {
        Terrain center = GetTerrainAt(coord);
        if (center == null) return;
        center.SetNeighbors(
            GetTerrainAt(coord + Vector2Int.left),
            GetTerrainAt(coord + Vector2Int.up),
            GetTerrainAt(coord + Vector2Int.right),
            GetTerrainAt(coord + Vector2Int.down));
    }

    Terrain GetTerrainAt(Vector2Int coord)
    {
        if (activeChunks.TryGetValue(coord, out TerrainChunk chunk)) return chunk.terrain;
        return null;
    }

    public Vector2Int WorldToChunkCoord(Vector3 worldPos)
    {
        return new Vector2Int(
            Mathf.FloorToInt(worldPos.x / chunkSize),
            Mathf.FloorToInt(worldPos.z / chunkSize));
    }

    public Vector3 ChunkCoordToWorld(Vector2Int coord)
    {
        return new Vector3(coord.x * chunkSize, 0, coord.y * chunkSize);
    }

    [ContextMenu("Force Regenerate All")]
    public void ForceRegenerateAll()
    {
        List<Vector2Int> allCoords = new List<Vector2Int>(activeChunks.Keys);
        foreach (var coord in allCoords) DestroyChunk(coord);
        activeChunks.Clear();
        mapGenerator.InitializeSeed();
        if (updateCoroutine != null) StopCoroutine(updateCoroutine);
        updateCoroutine = StartCoroutine(UpdateChunksCoroutine());
    }
}

public class TerrainChunk
{
    public Vector2Int coord;
    public GameObject gameObject;
    public Terrain terrain;
    public TerrainData terrainData;
    public GameObject objectsParent;
}
