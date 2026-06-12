using UnityEngine;

[CreateAssetMenu(fileName = "NewBiome", menuName = "Map Generation/Biome Definition")]
public class BiomeDefinition : ScriptableObject
{
    [Header("Biome Info")]
    public string biomeName = "New Biome";

    [Header("Terrain")]
    public TerrainLayer terrainLayer;
    [Range(0.1f, 3f)] public float heightMultiplier = 1f;
    [Range(0.001f, 0.1f)] public float noiseFrequency = 0.01f;

    [Header("Ground Objects")]
    public GameObject[] groundObjects;
    public int groundObjectCount = 4;

    [Header("Rock Objects")]
    public GameObject[] rockObjects;
    public int rockObjectCount = 10;

    [Header("Tree Objects")]
    public GameObject[] treeObjects;
    public int treeObjectCount = 5;

    [Header("Structure Objects (POI)")]
    public GameObject[] structureObjects;
    public int structureCount = 3;
    [Range(20f, 200f)] public float structureMinRoadDistance = 30f;
    [Range(50f, 400f)] public float structureMaxRoadDistance = 120f;

    [Header("Object Scale")]
    public Vector2 scaleRange = new Vector2(1f, 1f);
    public bool randomRotation = true;
}
