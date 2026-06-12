using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public class RemoveMeshCollidersEditor : MonoBehaviour
{
    [MenuItem("Tools/Hızlı Çözümler/Mesh Collider'ları Temizle")]
    public static void RemoveMeshColliders()
    {
        string[] guids = AssetDatabase.FindAssets("t:Prefab");
        int removedCount = 0;
        int modifiedPrefabs = 0;

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);

            if (prefab != null)
            {
                MeshCollider[] meshColliders = prefab.GetComponentsInChildren<MeshCollider>(true);

                if (meshColliders.Length > 0)
                {
                    GameObject contentsRoot = PrefabUtility.LoadPrefabContents(path);
                    MeshCollider[] collidersToRemove = contentsRoot.GetComponentsInChildren<MeshCollider>(true);

                    if (collidersToRemove.Length > 0)
                    {
                        foreach (MeshCollider mc in collidersToRemove)
                        {
                            DestroyImmediate(mc, true);
                            removedCount++;
                        }
                        PrefabUtility.SaveAsPrefabAsset(contentsRoot, path);
                        modifiedPrefabs++;
                    }
                    PrefabUtility.UnloadPrefabContents(contentsRoot);
                }
            }
        }

        Debug.Log($"[Mesh Collider Temizleyici] İşlem tamamlandı! Toplam {modifiedPrefabs} prefab düzenlendi ve {removedCount} adet gereksiz Mesh Collider başarıyla silindi.");
        EditorUtility.DisplayDialog("İşlem Başarılı", $"Toplam {removedCount} adet gizli Mesh Collider başarıyla silindi!", "Tamam");
    }
}
