using UnityEngine;
using UnityEngine.Rendering;
using System.Collections.Generic;

public class RoadGenerator : MonoBehaviour
{
    [Header("Road Settings")]
    public float roadWidth = 18f;
    public int roadMaskResolution = 128;

    [Header("Road Elevation")]
    [Range(0f, 1f)]
    public float elevationOffset = 0.15f;
    public float elevationFalloff = 2.5f;
    public float shoulderWidth = 6f;

    [Header("Center Stripe")]
    public float stripeWidth = 0.15f;
    public float stripeHeightOffset = 0.05f;
    public Color stripeColor = new Color(1f, 0.85f, 0f);
    public int stripeSegments = 80;
    Material stripeMaterial;

    [Header("Road Curvature")]
    [Range(0f, 300f)]
    public float curveAmplitude = 80f;
    [Range(0.0001f, 0.005f)]
    public float curveFrequency = 0.001f;
    [Range(0f, 150f)]
    public float curveAmplitude2 = 30f;
    [Range(0.0001f, 0.01f)]
    public float curveFrequency2 = 0.003f;
    [Range(0f, 80f)]
    public float curveAmplitude3 = 10f;
    [Range(0.001f, 0.02f)]
    public float curveFrequency3 = 0.007f;

    [Header("Road Terrain Layer")]
    public TerrainLayer roadLayer;

    int globalSeed;

    public void Initialize(int seed)
    {
        globalSeed = seed;

        if (roadLayer != null)
        {
            int texSize = 256;
            Texture2D cleanAsphalt = new Texture2D(texSize, texSize, TextureFormat.RGB24, true);
            for (int y = 0; y < texSize; y++)
            {
                for (int x = 0; x < texSize; x++)
                {
                    float n1 = Mathf.PerlinNoise(x * 0.08f, y * 0.08f);
                    float n2 = Mathf.PerlinNoise(x * 0.25f + 100f, y * 0.25f + 100f);
                    float n3 = Mathf.PerlinNoise(x * 0.6f + 200f, y * 0.6f + 200f);
                    float val = 0.18f + n1 * 0.06f + n2 * 0.04f + n3 * 0.02f;
                    cleanAsphalt.SetPixel(x, y, new Color(val, val, val * 1.03f));
                }
            }
            cleanAsphalt.Apply(true);
            cleanAsphalt.filterMode = FilterMode.Bilinear;
            cleanAsphalt.wrapMode = TextureWrapMode.Repeat;

            roadLayer.diffuseTexture = cleanAsphalt;
            roadLayer.tileSize = new Vector2(roadWidth * 2f, roadWidth * 2f);
            roadLayer.tileOffset = Vector2.zero;
        }

        Shader unlitShader = Shader.Find("Universal Render Pipeline/Unlit");
        if (unlitShader == null)
            unlitShader = Shader.Find("Unlit/Color");
        if (unlitShader != null)
        {
            stripeMaterial = new Material(unlitShader);
            stripeMaterial.color = stripeColor;
        }
    }

    public float GetRoadCenterX(float worldZ)
    {
        float seed1 = globalSeed * 0.1f;
        float seed2 = globalSeed * 0.2f;
        float seed3 = globalSeed * 0.3f;

        float wave1 = (Mathf.PerlinNoise(worldZ * curveFrequency + seed1, seed1) - 0.5f) * 2f * curveAmplitude;
        float wave2 = (Mathf.PerlinNoise(worldZ * curveFrequency2 + seed2, seed2) - 0.5f) * 2f * curveAmplitude2;
        float wave3 = (Mathf.PerlinNoise(worldZ * curveFrequency3 + seed3, seed3) - 0.5f) * 2f * curveAmplitude3;

        return wave1 + wave2 + wave3;
    }

    public void GenerateRoadMaskForChunk(float chunkWorldX, float chunkWorldZ, int chunkSize,
        out float[,] roadMask, out float[,] elevationMask)
    {
        int res = roadMaskResolution;
        roadMask = new float[res, res];
        elevationMask = new float[res, res];

        float halfRoad = roadWidth * 0.5f;
        float totalWidth = halfRoad + shoulderWidth * elevationFalloff;

        for (int z = 0; z < res; z++)
        {
            for (int x = 0; x < res; x++)
            {
                float normX = (float)x / (res - 1);
                float normZ = (float)z / (res - 1);

                float worldX = chunkWorldX + normX * chunkSize;
                float worldZ = chunkWorldZ + normZ * chunkSize;

                float roadCenterX = GetRoadCenterX(worldZ);
                float distFromRoad = Mathf.Abs(worldX - roadCenterX);

                if (distFromRoad <= halfRoad)
                {
                    roadMask[z, x] = 1f;
                    elevationMask[z, x] = 1f;
                }
                else if (distFromRoad <= totalWidth)
                {
                    float t = (distFromRoad - halfRoad) / (totalWidth - halfRoad);
                    float falloff = 1f - t * t;
                    elevationMask[z, x] = Mathf.Max(0f, falloff);
                }
            }
        }
    }

    public void GenerateChunkStripe(TerrainChunk chunk, int chunkSize)
    {
        float chunkWorldX = chunk.coord.x * chunkSize;
        float chunkWorldZ = chunk.coord.y * chunkSize;

        int segs = stripeSegments;
        List<Vector3> verts = new List<Vector3>();
        List<int> tris = new List<int>();

        for (int i = 0; i <= segs; i++)
        {
            float t = (float)i / segs;
            float worldZ = chunkWorldZ + t * chunkSize;
            float centerX = GetRoadCenterX(worldZ);

            Vector3 samplePos = new Vector3(centerX, 0, worldZ);
            float terrainY = chunk.terrain.SampleHeight(samplePos)
                + chunk.gameObject.transform.position.y + stripeHeightOffset;

            verts.Add(new Vector3(centerX - stripeWidth, terrainY, worldZ));
            verts.Add(new Vector3(centerX + stripeWidth, terrainY, worldZ));

            if (i > 0)
            {
                int b = (i - 1) * 2;
                tris.Add(b); tris.Add(b + 2); tris.Add(b + 1);
                tris.Add(b + 1); tris.Add(b + 2); tris.Add(b + 3);
            }
        }

        Mesh mesh = new Mesh();
        mesh.SetVertices(verts);
        mesh.SetTriangles(tris, 0);
        mesh.RecalculateNormals();

        GameObject stripeObj = new GameObject("RoadStripe");
        stripeObj.transform.SetParent(chunk.gameObject.transform);
        stripeObj.transform.position = Vector3.zero;

        MeshFilter mf = stripeObj.AddComponent<MeshFilter>();
        mf.sharedMesh = mesh;

        MeshRenderer mr = stripeObj.AddComponent<MeshRenderer>();
        mr.sharedMaterial = stripeMaterial;
        mr.shadowCastingMode = ShadowCastingMode.Off;
        mr.receiveShadows = false;
    }

    public float GetDistanceFromRoad(float worldX, float worldZ)
    {
        float roadCenterX = GetRoadCenterX(worldZ);
        return Mathf.Abs(worldX - roadCenterX);
    }

    public bool IsOnRoad(float worldX, float worldZ)
    {
        return GetDistanceFromRoad(worldX, worldZ) <= roadWidth * 0.5f;
    }
}
