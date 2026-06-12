using UnityEngine;

[RequireComponent(typeof(Seed))]
[RequireComponent(typeof(ChunkManager))]
[RequireComponent(typeof(TerrainGenerator))]
[RequireComponent(typeof(BiomeManager))]
[RequireComponent(typeof(RoadGenerator))]
[RequireComponent(typeof(ObjectPlacer))]
public class MapGenerator : MonoBehaviour
{
    [Header("Biome Definitions (index 0 = Desert/Main)")]
    public BiomeDefinition[] biomes = new BiomeDefinition[4];

    [Header("Biome Map Resolution Per Chunk")]
    public int biomeResolution = 64;

    Seed seed;
    TerrainGenerator terrainGen;
    BiomeManager biomeManager;
    RoadGenerator roadGen;
    ObjectPlacer objectPlacer;

    public void InitializeSeed()
    {
        CacheComponents();

        int currentSeed = seed.GetSeed();
        Debug.Log($"[MapGenerator] Seed: '{seed.gameSeed}' (hash: {currentSeed})");

        roadGen.Initialize(currentSeed);
        terrainGen.Initialize(currentSeed);
        biomeManager.Initialize(currentSeed);
        objectPlacer.Initialize(currentSeed);
    }

    void CacheComponents()
    {
        seed = GetComponent<Seed>();
        terrainGen = GetComponent<TerrainGenerator>();
        biomeManager = GetComponent<BiomeManager>();
        roadGen = GetComponent<RoadGenerator>();
        objectPlacer = GetComponent<ObjectPlacer>();
    }

    public System.Collections.IEnumerator GenerateChunkCoroutine(TerrainChunk chunk, int chunkSize)
    {
        if (biomes == null || biomes.Length < 4)
        {
            Debug.LogError("[MapGenerator] 4 BiomeDefinition required! (index 0 = Desert)");
            yield break;
        }

        for (int i = 0; i < biomes.Length; i++)
        {
            if (biomes[i] == null)
            {
                Debug.LogError($"[MapGenerator] Biome slot {i} is empty!");
                yield break;
            }
        }

        float chunkWorldX = chunk.coord.x * chunkSize;
        float chunkWorldZ = chunk.coord.y * chunkSize;

        roadGen.GenerateRoadMaskForChunk(chunkWorldX, chunkWorldZ, chunkSize,
            out float[,] roadMask, out float[,] elevationMask);
        yield return null;

        BiomeDefinition[,] biomeMap = biomeManager.GenerateChunkBiomeMap(
            chunkWorldX, chunkWorldZ, chunkSize, biomeResolution, biomes, roadGen);
        yield return null;

        terrainGen.GenerateChunkHeightmap(chunk, chunkSize, roadMask, elevationMask,
            roadGen.elevationOffset, biomeMap, biomeResolution);
        yield return null;

        biomeManager.PaintChunkSplatmap(chunk, biomeMap, biomes, roadMask, roadGen.roadLayer);
        yield return null;

        yield return StartCoroutine(objectPlacer.PlaceChunkObjectsCoroutine(chunk, chunkSize, biomes, biomeMap, roadMask, roadGen));

        roadGen.GenerateChunkStripe(chunk, chunkSize);
    }

    public void GenerateChunk(TerrainChunk chunk, int chunkSize)
    {
        if (biomes == null || biomes.Length < 4) return;
        for (int i = 0; i < biomes.Length; i++) if (biomes[i] == null) return;

        float chunkWorldX = chunk.coord.x * chunkSize;
        float chunkWorldZ = chunk.coord.y * chunkSize;

        roadGen.GenerateRoadMaskForChunk(chunkWorldX, chunkWorldZ, chunkSize, out float[,] roadMask, out float[,] elevationMask);
        BiomeDefinition[,] biomeMap = biomeManager.GenerateChunkBiomeMap(chunkWorldX, chunkWorldZ, chunkSize, biomeResolution, biomes, roadGen);
        terrainGen.GenerateChunkHeightmap(chunk, chunkSize, roadMask, elevationMask, roadGen.elevationOffset, biomeMap, biomeResolution);
        biomeManager.PaintChunkSplatmap(chunk, biomeMap, biomes, roadMask, roadGen.roadLayer);
        objectPlacer.PlaceChunkObjects(chunk, chunkSize, biomes, biomeMap, roadMask, roadGen);
        roadGen.GenerateChunkStripe(chunk, chunkSize);
    }
}
