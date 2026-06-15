using UnityEngine;
using UnityEngine.UI;
using UnityEditor;

public class WiringFixer : EditorWindow
{
    [MenuItem("Tools/Sistemi Otomatik Bagla")]
    public static void FixWiring()
    {
        // 1. CreditsMenuHolder'ı bul ve devre dışı bırak (Başta açılmaması için)
        GameObject creditsHolder = null;
        GameObject[] allObjects = Resources.FindObjectsOfTypeAll<GameObject>();
        foreach (GameObject obj in allObjects)
        {
            if (obj.name == "CreditsMenuHolder" && obj.scene.isLoaded)
            {
                creditsHolder = obj;
                break;
            }
        }

        if (creditsHolder != null)
        {
            creditsHolder.SetActive(false);
            EditorUtility.SetDirty(creditsHolder);
            
            // 2. Button_KAPAT'ı bul ve bağla
            Button kapatBtn = null;
            Button[] buttons = creditsHolder.GetComponentsInChildren<Button>(true);
            foreach (Button b in buttons)
            {
                if (b.gameObject.name == "Button_KAPAT")
                {
                    kapatBtn = b;
                    break;
                }
            }

            if (kapatBtn != null)
            {
                // Eski eventleri temizle
                UnityEditor.Events.UnityEventTools.RemovePersistentListener(kapatBtn.onClick, 0);
                // Yeniden bağla (Kapat)
                UnityEditor.Events.UnityEventTools.AddBoolPersistentListener(kapatBtn.onClick, new UnityEngine.Events.UnityAction<bool>(creditsHolder.SetActive), false);
                EditorUtility.SetDirty(kapatBtn);
            }
        }

        // 3. Canvas'taki 'credits' tuşunu bul ve bağla
        GameObject canvasCreditsBtnObj = GameObject.Find("credits");
        if (canvasCreditsBtnObj != null)
        {
            Button creditsBtn = canvasCreditsBtnObj.GetComponent<Button>();
            if (creditsBtn != null && creditsHolder != null)
            {
                // Eski eventleri temizle
                int count = creditsBtn.onClick.GetPersistentEventCount();
                for (int i = count - 1; i >= 0; i--)
                {
                    UnityEditor.Events.UnityEventTools.RemovePersistentListener(creditsBtn.onClick, i);
                }
                
                // Yeniden bağla (Aç)
                UnityEditor.Events.UnityEventTools.AddBoolPersistentListener(creditsBtn.onClick, new UnityEngine.Events.UnityAction<bool>(creditsHolder.SetActive), true);
                EditorUtility.SetDirty(creditsBtn);
            }
        }

        Debug.Log("Sistem otomatik baglandi! Credits menusu gizlendi ve butonlara atandi.");
    }
}
