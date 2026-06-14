using UnityEngine;

public class BiomeManager : MonoBehaviour
{
    [Header("Biome Strip Layout")]
    public float biomeStripLength = 2000f;
    [Range(0.2f, 0.8f)] public float desertRatio = 0.45f;

    int globalSeed;
    int[] biomeOrder;
    float totalCycleLength;

    public void Initialize(int seed)
    {
        globalSeed = seed;
        BuildBiomeOrder();
    }

    void BuildBiomeOrder()
    {
        System.Random rng = new System.Random(globalSeed + 777);
        int[] otherIndices = { 1, 2, 3 };
        for (int i = otherIndices.Length - 1; i > 0; i--)
        {
            int j = rng.Next(i + 1);
            int tmp = otherIndices[i];
            otherIndices[i] = otherIndices[j];
            otherIndices[j] = tmp;
        }
        biomeOrder = new int[] { 0, otherIndices[0], otherIndices[1], otherIndices[2] };
        totalCycleLength = biomeStripLength;
    }

    public int GetBiomeIndexAtZ(float worldZ)
    {
        float desertLen = biomeStripLength * desertRatio;
        float otherLen = biomeStripLength * (1f - desertRatio) / 3f;

        float offset = globalSeed * 17.3f;
        float z = worldZ + offset;

        if (z < 0) z += Mathf.Ceil(Mathf.Abs(z) / totalCycleLength) * totalCycleLength;

        float posInCycle = z % totalCycleLength;

        if (posInCycle < desertLen) return 0;
        else if (posInCycle < desertLen + otherLen) return biomeOrder[1];
        else if (posInCycle < desertLen + otherLen * 2f) return biomeOrder[2];
        else return biomeOrder[3];
    }

    public BiomeDefinition[,] GenerateChunkBiomeMap(float chunkWorldX, float chunkWorldZ,
        int chunkSize, int resolution, BiomeDefinition[] biomes, RoadGenerator roadGen)
    {
        BiomeDefinition[,] biomeMap = new BiomeDefinition[resolution, resolution];

        for (int z = 0; z < resolution; z++)
        {
            for (int x = 0; x < resolution; x++)
            {
                float normZ = (float)z / (resolution - 1);
                float worldZ = chunkWorldZ + normZ * chunkSize;

                int biomeIndex = GetBiomeIndexAtZ(worldZ);
                biomeIndex = Mathf.Clamp(biomeIndex, 0, biomes.Length - 1);
                biomeMap[z, x] = biomes[biomeIndex];
            }
        }
        return biomeMap;
    }

    public void PaintChunkSplatmap(TerrainChunk chunk, BiomeDefinition[,] biomeMap,
        BiomeDefinition[] biomes, float[,] roadMask, TerrainLayer roadLayer)
    {
        TerrainData td = chunk.terrainData;

        bool hasRoadLayer = roadLayer != null;
        int totalLayers = biomes.Length + (hasRoadLayer ? 1 : 0);
        TerrainLayer[] allLayers = new TerrainLayer[totalLayers];
        for (int i = 0; i < biomes.Length; i++)
            allLayers[i] = biomes[i].terrainLayer;
        if (hasRoadLayer) allLayers[biomes.Length] = roadLayer;
        td.terrainLayers = allLayers;

        int alphamapRes = td.alphamapResolution;
        int layerCount = allLayers.Length;
        float[,,] splatmapData = new float[alphamapRes, alphamapRes, layerCount];

        int biomeResX = biomeMap.GetLength(1);
        int biomeResZ = biomeMap.GetLength(0);
        int maskRes = roadMask.GetLength(0);

        for (int z = 0; z < alphamapRes; z++)
        {
            for (int x = 0; x < alphamapRes; x++)
            {
                float normX = (float)x / (alphamapRes - 1);
                float normZ = (float)z / (alphamapRes - 1);

                int bx = Mathf.Clamp(Mathf.FloorToInt(normX * biomeResX), 0, biomeResX - 1);
                int bz = Mathf.Clamp(Mathf.FloorToInt(normZ * biomeResZ), 0, biomeResZ - 1);
                BiomeDefinition currentBiome = biomeMap[bz, bx];

                int mx = Mathf.Clamp(Mathf.FloorToInt(normX * (maskRes - 1)), 0, maskRes - 1);
                int mz = Mathf.Clamp(Mathf.FloorToInt(normZ * (maskRes - 1)), 0, maskRes - 1);
                float roadWeight = roadMask[mz, mx];

                int biomeLayerIndex = 0;
                for (int i = 0; i < biomes.Length; i++)
                {
                    if (biomes[i] == currentBiome) { biomeLayerIndex = i; break; }
                }

                for (int l = 0; l < layerCount; l++)
                    splatmapData[z, x, l] = 0f;

                splatmapData[z, x, biomeLayerIndex] = 1f - roadWeight;
                if (hasRoadLayer) splatmapData[z, x, biomes.Length] = roadWeight;
                else splatmapData[z, x, biomeLayerIndex] = 1f;
            }
        }
        td.SetAlphamaps(0, 0, splatmapData);
    }

    public System.Collections.IEnumerator PaintChunkSplatmapCoroutine(TerrainChunk chunk, BiomeDefinition[,] biomeMap,
        BiomeDefinition[] biomes, float[,] roadMask, TerrainLayer roadLayer)
    {
        TerrainData td = chunk.terrainData;

        bool hasRoadLayer = roadLayer != null;
        int totalLayers = biomes.Length + (hasRoadLayer ? 1 : 0);
        TerrainLayer[] allLayers = new TerrainLayer[totalLayers];
        for (int i = 0; i < biomes.Length; i++)
            allLayers[i] = biomes[i].terrainLayer;
        if (hasRoadLayer) allLayers[biomes.Length] = roadLayer;
        td.terrainLayers = allLayers;

        int alphamapRes = td.alphamapResolution;
        int layerCount = allLayers.Length;
        float[,,] splatmapData = new float[alphamapRes, alphamapRes, layerCount];

        int biomeResX = biomeMap.GetLength(1);
        int biomeResZ = biomeMap.GetLength(0);
        int maskRes = roadMask.GetLength(0);

        for (int z = 0; z < alphamapRes; z++)
        {
            for (int x = 0; x < alphamapRes; x++)
            {
                float normX = (float)x / (alphamapRes - 1);
                float normZ = (float)z / (alphamapRes - 1);

                int bx = Mathf.Clamp(Mathf.FloorToInt(normX * biomeResX), 0, biomeResX - 1);
                int bz = Mathf.Clamp(Mathf.FloorToInt(normZ * biomeResZ), 0, biomeResZ - 1);
                BiomeDefinition currentBiome = biomeMap[bz, bx];

                int mx = Mathf.Clamp(Mathf.FloorToInt(normX * (maskRes - 1)), 0, maskRes - 1);
                int mz = Mathf.Clamp(Mathf.FloorToInt(normZ * (maskRes - 1)), 0, maskRes - 1);
                float roadWeight = roadMask[mz, mx];

                int biomeLayerIndex = 0;
                for (int i = 0; i < biomes.Length; i++)
                {
                    if (biomes[i] == currentBiome) { biomeLayerIndex = i; break; }
                }

                for (int l = 0; l < layerCount; l++)
                    splatmapData[z, x, l] = 0f;

                splatmapData[z, x, biomeLayerIndex] = 1f - roadWeight;
                if (hasRoadLayer) splatmapData[z, x, biomes.Length] = roadWeight;
                else splatmapData[z, x, biomeLayerIndex] = 1f;
            }

            if (z % 32 == 0) yield return null;
        }
        td.SetAlphamaps(0, 0, splatmapData);
    }
}
