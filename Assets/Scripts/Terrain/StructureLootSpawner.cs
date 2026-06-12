using UnityEngine;
using System.Collections.Generic;

public class StructureLootSpawner : MonoBehaviour
{
    public static void SpawnLoot(GameObject structure, StructureLootTable lootTable,
        System.Random rng, Terrain terrain)
    {
        if (lootTable == null || lootTable.parts == null || lootTable.parts.Length == 0) return;

        int partCount = lootTable.GetRandomPartCount(rng);
        if (partCount <= 0) return;

        Vector3 center = structure.transform.position;
        float radius = lootTable.spawnRadius;

        Transform lootParent = new GameObject("Loot").transform;
        lootParent.SetParent(structure.transform);
        lootParent.localPosition = Vector3.zero;

        Dictionary<StructureLootTable.LootEntry, int> spawnCounts = new Dictionary<StructureLootTable.LootEntry, int>();

        int spawned = 0;
        int maxAttempts = partCount * 10;
        int attempts = 0;

        while (spawned < partCount && attempts < maxAttempts)
        {
            attempts++;

            StructureLootTable.LootEntry entry = lootTable.GetRandomPart(rng);
            if (entry == null || entry.prefab == null) continue;

            if (entry.maxPerStructure > 0)
            {
                spawnCounts.TryGetValue(entry, out int currentCount);
                if (currentCount >= entry.maxPerStructure) continue;
            }

            float angle = (float)rng.NextDouble() * Mathf.PI * 2f;
            float dist = (float)rng.NextDouble() * radius;
            float offsetX = Mathf.Cos(angle) * dist;
            float offsetZ = Mathf.Sin(angle) * dist;

            float worldX = center.x + offsetX;
            float worldZ = center.z + offsetZ;

            float groundY = center.y;
            if (terrain != null)
            {
                float sampledY = terrain.SampleHeight(new Vector3(worldX, 0, worldZ)) + terrain.transform.position.y;
                groundY = sampledY;
            }

            Vector3 spawnPos = new Vector3(worldX, groundY + lootTable.dropHeight, worldZ);

            Quaternion rotation = Quaternion.identity;
            if (lootTable.randomPartRotation)
            {
                rotation = Quaternion.Euler(
                    (float)rng.NextDouble() * 15f - 7.5f,
                    (float)rng.NextDouble() * 360f,
                    (float)rng.NextDouble() * 15f - 7.5f
                );
            }

            GameObject part = Object.Instantiate(entry.prefab, spawnPos, rotation, lootParent);

            float scale = lootTable.partScaleRange.x +
                (float)rng.NextDouble() * (lootTable.partScaleRange.y - lootTable.partScaleRange.x);
            part.transform.localScale = entry.prefab.transform.localScale * scale;

            foreach (var mc in part.GetComponentsInChildren<MeshCollider>())
                mc.convex = true;

            if (part.GetComponentInChildren<Collider>() == null)
            {
                MeshFilter mf = part.GetComponentInChildren<MeshFilter>();
                if (mf != null && mf.sharedMesh != null)
                {
                    MeshCollider mc = mf.gameObject.AddComponent<MeshCollider>();
                    mc.sharedMesh = mf.sharedMesh;
                    mc.convex = true;
                }
            }

            Rigidbody rb = part.GetComponent<Rigidbody>();
            if (rb == null) rb = part.AddComponent<Rigidbody>();
            rb.mass = 5f;
            rb.interpolation = RigidbodyInterpolation.None;
            rb.collisionDetectionMode = CollisionDetectionMode.Discrete;

            if (part.GetComponent<SettleAndFreeze>() == null)
                part.AddComponent<SettleAndFreeze>();

            if (!spawnCounts.ContainsKey(entry)) spawnCounts[entry] = 0;
            spawnCounts[entry]++;
            spawned++;
        }
    }
}
