using UnityEngine;
using UnityEditor;

public class DisableGenerateCollidersEditor : MonoBehaviour
{
    [MenuItem("Tools/Hızlı Çözümler/Tüm FBX Colliderlarını Kapat")]
    public static void DisableFBXColliders()
    {
        string[] guids = AssetDatabase.FindAssets("t:Model");
        int modifiedCount = 0;

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            ModelImporter importer = AssetImporter.GetAtPath(path) as ModelImporter;

            if (importer != null && importer.addCollider)
            {
                importer.addCollider = false;
                importer.SaveAndReimport();
                modifiedCount++;
            }
        }

        Debug.Log($"[FBX Collider Temizleyici] İşlem tamamlandı! Toplam {modifiedCount} adet 3D modelin otomatik Mesh Collider özelliği kapatıldı.");
        EditorUtility.DisplayDialog("İşlem Başarılı", $"Toplam {modifiedCount} adet 3D modelin otomatik Mesh Collider özelliği başarıyla kapatıldı! Artık hata almayacaksınız.", "Tamam");
    }
}
