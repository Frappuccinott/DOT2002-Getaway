using UnityEngine;

public class TerrainGenerator : MonoBehaviour
{
    [Header("Terrain Settings")]
    public int heightmapResolution = 257;
    public float terrainHeight = 60f;

    [Header("Noise Settings")]
    public float baseScale = 0.003f;
    public int octaves = 3;
    [Range(0f, 1f)]
    public float persistence = 0.35f;
    public float lacunarity = 2f;

    [Header("Road Flattening")]
    [Range(0f, 0.5f)]
    public float baseGroundHeight = 0.05f;

    int globalSeed;

    public void Initialize(int seed)
    {
        globalSeed = seed;
    }

    public void GenerateChunkHeightmap(TerrainChunk chunk, int chunkSize,
        float[,] roadMask, float[,] elevationMask, float elevationOffset,
        BiomeDefinition[,] biomeMap, int biomeResolution)
    {
        TerrainData td = chunk.terrainData;
        td.heightmapResolution = heightmapResolution;
        td.size = new Vector3(chunkSize, terrainHeight, chunkSize);

        int res = td.heightmapResolution;
        float[,] heights = new float[res, res];

        float chunkWorldX = chunk.coord.x * chunkSize;
        float chunkWorldZ = chunk.coord.y * chunkSize;

        float offsetX = globalSeed * 0.1f;
        float offsetZ = globalSeed * 0.17f;

        int maskRes = roadMask.GetLength(0);

        for (int z = 0; z < res; z++)
        {
            for (int x = 0; x < res; x++)
            {
                float normX = (float)x / (res - 1);
                float normZ = (float)z / (res - 1);

                float worldX = chunkWorldX + normX * chunkSize;
                float worldZ = chunkWorldZ + normZ * chunkSize;

                int bx = Mathf.Clamp(Mathf.FloorToInt(normX * biomeResolution), 0, biomeResolution - 1);
                int bz = Mathf.Clamp(Mathf.FloorToInt(normZ * biomeResolution), 0, biomeResolution - 1);
                BiomeDefinition biome = biomeMap[bz, bx];

                float heightMult = biome != null ? biome.heightMultiplier : 1f;
                float biomeNoiseFreq = biome != null ? biome.noiseFrequency : baseScale;
                int biomeOctaves = octaves;

                if (heightMult > 1.5f)
                    biomeOctaves = octaves + 2;

                float amplitude = 1f;
                float frequency = 1f;
                float noiseHeight = 0f;
                float maxAmplitude = 0f;

                for (int o = 0; o < biomeOctaves; o++)
                {
                    float sampleX = (worldX * biomeNoiseFreq + offsetX) * frequency;
                    float sampleZ = (worldZ * biomeNoiseFreq + offsetZ) * frequency;

                    float perlin = Mathf.PerlinNoise(sampleX, sampleZ) * 2f - 1f;
                    noiseHeight += perlin * amplitude;

                    maxAmplitude += amplitude;
                    amplitude *= persistence;
                    frequency *= lacunarity;
                }

                noiseHeight = (noiseHeight / maxAmplitude + 1f) / 2f;

                float terrainNoiseHeight = baseGroundHeight + noiseHeight * heightMult * 0.15f;

                int mx = Mathf.Clamp(Mathf.FloorToInt(normX * (maskRes - 1)), 0, maskRes - 1);
                int mz = Mathf.Clamp(Mathf.FloorToInt(normZ * (maskRes - 1)), 0, maskRes - 1);

                float road = roadMask[mz, mx];
                float elev = elevationMask[mz, mx];

                float roadHeight = baseGroundHeight + elevationOffset;

                if (road > 0.5f)
                {
                    heights[z, x] = roadHeight;
                }
                else
                {
                    heights[z, x] = Mathf.Lerp(terrainNoiseHeight, roadHeight, elev);
                }
            }
        }

        td.SetHeights(0, 0, heights);
    }

    public System.Collections.IEnumerator GenerateChunkHeightmapCoroutine(TerrainChunk chunk, int chunkSize,
        float[,] roadMask, float[,] elevationMask, float elevationOffset,
        BiomeDefinition[,] biomeMap, int biomeResolution)
    {
        TerrainData td = chunk.terrainData;
        td.heightmapResolution = heightmapResolution;
        td.size = new Vector3(chunkSize, terrainHeight, chunkSize);

        int res = td.heightmapResolution;
        float[,] heights = new float[res, res];

        float chunkWorldX = chunk.coord.x * chunkSize;
        float chunkWorldZ = chunk.coord.y * chunkSize;

        float offsetX = globalSeed * 0.1f;
        float offsetZ = globalSeed * 0.17f;

        int maskRes = roadMask.GetLength(0);

        for (int z = 0; z < res; z++)
        {
            for (int x = 0; x < res; x++)
            {
                float normX = (float)x / (res - 1);
                float normZ = (float)z / (res - 1);

                float worldX = chunkWorldX + normX * chunkSize;
                float worldZ = chunkWorldZ + normZ * chunkSize;

                int bx = Mathf.Clamp(Mathf.FloorToInt(normX * biomeResolution), 0, biomeResolution - 1);
                int bz = Mathf.Clamp(Mathf.FloorToInt(normZ * biomeResolution), 0, biomeResolution - 1);
                BiomeDefinition biome = biomeMap[bz, bx];

                float heightMult = biome != null ? biome.heightMultiplier : 1f;
                float biomeNoiseFreq = biome != null ? biome.noiseFrequency : baseScale;
                int biomeOctaves = octaves;

                if (heightMult > 1.5f)
                    biomeOctaves = octaves + 2;

                float amplitude = 1f;
                float frequency = 1f;
                float noiseHeight = 0f;
                float maxAmplitude = 0f;

                for (int o = 0; o < biomeOctaves; o++)
                {
                    float sampleX = (worldX * biomeNoiseFreq + offsetX) * frequency;
                    float sampleZ = (worldZ * biomeNoiseFreq + offsetZ) * frequency;

                    float perlin = Mathf.PerlinNoise(sampleX, sampleZ) * 2f - 1f;
                    noiseHeight += perlin * amplitude;

                    maxAmplitude += amplitude;
                    amplitude *= persistence;
                    frequency *= lacunarity;
                }

                noiseHeight = (noiseHeight / maxAmplitude + 1f) / 2f;

                float terrainNoiseHeight = baseGroundHeight + noiseHeight * heightMult * 0.15f;

                int mx = Mathf.Clamp(Mathf.FloorToInt(normX * (maskRes - 1)), 0, maskRes - 1);
                int mz = Mathf.Clamp(Mathf.FloorToInt(normZ * (maskRes - 1)), 0, maskRes - 1);

                float road = roadMask[mz, mx];
                float elev = elevationMask[mz, mx];

                float roadHeight = baseGroundHeight + elevationOffset;

                if (road > 0.5f)
                {
                    heights[z, x] = roadHeight;
                }
                else
                {
                    heights[z, x] = Mathf.Lerp(terrainNoiseHeight, roadHeight, elev);
                }
            }

            if (z % 16 == 0) yield return null;
        }

        td.SetHeights(0, 0, heights);
    }
}
