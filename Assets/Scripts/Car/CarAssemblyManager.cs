using UnityEngine;
using System.Collections.Generic;

public class CarAssemblyManager : MonoBehaviour
{
    [Header("Montaj Ayarları")]
    [SerializeField] private CarPartSlot[] partSlots;

    [Header("Çalıştırma Gereksinimleri")]
    [SerializeField] private CarPartType[] requiredParts;

    private readonly HashSet<CarPartType> installedParts = new();
    private readonly HashSet<CarPartType> requiredPartsSet = new();
    private int totalSlotCount;

    public int InstalledCount => installedParts.Count;
    public int TotalSlotCount => totalSlotCount;
    public bool IsComplete => installedParts.Count >= totalSlotCount && totalSlotCount > 0;
    public float Progress => totalSlotCount > 0 ? (float)installedParts.Count / totalSlotCount : 0f;

    public bool CanStart
    {
        get
        {
            if (requiredPartsSet.Count == 0) return IsComplete;
            foreach (CarPartType part in requiredPartsSet)
                if (!installedParts.Contains(part)) return false;
            return true;
        }
    }

    private void Awake()
    {
        if (partSlots == null || partSlots.Length == 0)
            partSlots = GetComponentsInChildren<CarPartSlot>(true);

        totalSlotCount = partSlots.Length;

        if (requiredParts != null)
            foreach (CarPartType part in requiredParts)
                requiredPartsSet.Add(part);
    }

    public void OnPartInstalled(CarPartType partType)
    {
        installedParts.Add(partType);
        if (IsComplete) OnAssemblyComplete();
    }

    public void OnPartRemoved(CarPartType partType)
    {
        installedParts.Remove(partType);
    }

    public bool IsPartInstalled(CarPartType partType)
    {
        return installedParts.Contains(partType);
    }

    private void OnAssemblyComplete()
    {
        Debug.Log("[CarAssembly] Araba montajı tamamlandı!");
    }
}
