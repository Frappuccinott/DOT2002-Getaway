using UnityEngine;

[CreateAssetMenu(fileName = "NewLootTable", menuName = "Map Generation/Structure Loot Table")]
public class StructureLootTable : ScriptableObject
{
    [System.Serializable]
    public class LootEntry
    {
        public string partName;
        public GameObject prefab;
        [Range(1, 100)] public int spawnWeight = 10;
        public int maxPerStructure = 0;
    }

    [Header("Loot Entries")]
    public LootEntry[] parts;

    [Header("Spawn Count")]
    [Range(0, 30)] public int minPartsPerStructure = 2;
    [Range(0, 30)] public int maxPartsPerStructure = 6;

    [Header("Placement")]
    [Range(1f, 30f)] public float spawnRadius = 8f;
    [Range(0f, 3f)] public float dropHeight = 0.5f;
    public bool randomPartRotation = true;

    [Header("Scale Variation")]
    public Vector2 partScaleRange = new Vector2(0.9f, 1.1f);

    public LootEntry GetRandomPart(System.Random rng)
    {
        if (parts == null || parts.Length == 0) return null;

        int totalWeight = 0;
        foreach (var entry in parts)
            if (entry.prefab != null) totalWeight += entry.spawnWeight;
        if (totalWeight <= 0) return null;

        int roll = rng.Next(totalWeight);
        int cumulative = 0;
        foreach (var entry in parts)
        {
            if (entry.prefab == null) continue;
            cumulative += entry.spawnWeight;
            if (roll < cumulative) return entry;
        }
        return parts[parts.Length - 1];
    }

    public int GetRandomPartCount(System.Random rng)
    {
        if (minPartsPerStructure >= maxPartsPerStructure) return minPartsPerStructure;
        return rng.Next(minPartsPerStructure, maxPartsPerStructure + 1);
    }
}
