using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using TMPro;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

public class PauseMenuGenerator : EditorWindow
{
    private static TMP_FontAsset customFont;

    [MenuItem("Tools/Pause Menusu Olustur")]
    public static void CreateMenu()
    {
        GameManager gameManager = Object.FindFirstObjectByType<GameManager>();
        if (gameManager == null)
        {
            Debug.LogError("Sahnede GameManager bulunamadı! Lütfen önce oyuna bir GameManager ekleyin.");
            return;
        }

        // Font bulma (SettingsMenuGenerator ile aynı mantık)
        string[] fontGuids = AssetDatabase.FindAssets("t:TMP_FontAsset");
        foreach (var guid in fontGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            TMP_FontAsset f = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path);
            if (f != null && !path.Contains("Liberation")) 
            {
                customFont = f;
                break;
            }
        }
        if (customFont == null && fontGuids.Length > 0)
        {
            customFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(AssetDatabase.GUIDToAssetPath(fontGuids[0]));
        }

        // Bütün eski menüleri bulup sil (inaktif olanlar dahil)
        GameObject[] allObjects = Resources.FindObjectsOfTypeAll<GameObject>();
        foreach (GameObject obj in allObjects)
        {
            if (obj.name == "PauseMenuHolder" && obj.scene.isLoaded)
            {
                DestroyImmediate(obj);
            }
        }

        // Kök Obje ve Kendi Canvas'ı
        GameObject holder = new GameObject("PauseMenuHolder");
        Canvas canvas = holder.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 99; // Settings 100 olduğu için Settings Pause'un üstüne çıkar
        
        CanvasScaler scaler = holder.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080); // Boyutların düzgün olması için
        
        holder.AddComponent<GraphicRaycaster>();

        RectTransform hRect = holder.GetComponent<RectTransform>();
        SetRectFull(hRect);



        // Arkaplan Blur/Koyulaştırma
        GameObject bg = new GameObject("BlurBackground");
        bg.transform.SetParent(holder.transform, false);
        RectTransform bgRect = bg.AddComponent<RectTransform>();
        SetRectFull(bgRect);
        Image bgImg = bg.AddComponent<Image>();
        bgImg.color = new Color(0, 0, 0, 0.7f);

        // Kapatmak için arkaplana buton özelliği (ESC işlevi görsün diye)
        Button bgBtn = bg.AddComponent<Button>();
        UnityEditor.Events.UnityEventTools.AddPersistentListener(bgBtn.onClick, new UnityAction(gameManager.ResumeGame));

        // Ana Panel
        GameObject pausePanel = new GameObject("PausePanel");
        pausePanel.transform.SetParent(holder.transform, false);
        RectTransform pRect = pausePanel.AddComponent<RectTransform>();
        pRect.anchorMin = new Vector2(0.5f, 0.5f);
        pRect.anchorMax = new Vector2(0.5f, 0.5f);
        pRect.pivot = new Vector2(0.5f, 0.5f);
        pRect.sizeDelta = new Vector2(400, 600); // Kullanıcının istediği 400x600 dikey boyut

        Image pImage = pausePanel.AddComponent<Image>();
        SetBackground(pImage);

        // Başlık
        TextMeshProUGUI title = CreateText(pausePanel.transform, "OYUN DURDURULDU", 40, TextAlignmentOptions.Center);
        title.rectTransform.anchorMin = new Vector2(0.5f, 1);
        title.rectTransform.anchorMax = new Vector2(0.5f, 1);
        title.rectTransform.pivot = new Vector2(0.5f, 1);
        title.rectTransform.anchoredPosition = new Vector2(0, -60);
        title.rectTransform.sizeDelta = new Vector2(360, 60);

        // Butonlar İçin Layout
        GameObject buttonsContainer = new GameObject("ButtonsContainer");
        buttonsContainer.transform.SetParent(pausePanel.transform, false);
        RectTransform btnRect = buttonsContainer.AddComponent<RectTransform>();
        btnRect.anchorMin = new Vector2(0.5f, 0.5f);
        btnRect.anchorMax = new Vector2(0.5f, 0.5f);
        btnRect.pivot = new Vector2(0.5f, 0.5f);
        btnRect.anchoredPosition = new Vector2(0, -50);
        btnRect.sizeDelta = new Vector2(300, 300);

        VerticalLayoutGroup layout = buttonsContainer.AddComponent<VerticalLayoutGroup>();
        layout.spacing = 30;
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childControlHeight = true;
        layout.childControlWidth = true;

        // Butonları Oluştur
        GameObject btnDevamObj = CreateButton(buttonsContainer.transform, "Devam Et", out Button btnDevam);
        GameObject btnAyarlarObj = CreateButton(buttonsContainer.transform, "Ayarlar", out Button btnAyarlar);
        GameObject btnAnaMenuObj = CreateButton(buttonsContainer.transform, "Ana Menü", out Button btnAnaMenu);

        // Bağlantılar (Wiring)
        UnityEditor.Events.UnityEventTools.AddPersistentListener(btnDevam.onClick, new UnityAction(gameManager.ResumeGame));
        
        // GameManager'a ReturnToMainMenu eklemeliyiz. Eğer yoksa uyarı vereceğiz ama ben GameManager'ı güncelleyip ekleyeceğim!
        var methodInfo = typeof(GameManager).GetMethod("ReturnToMainMenu");
        if (methodInfo != null)
        {
            UnityEditor.Events.UnityEventTools.AddPersistentListener(btnAnaMenu.onClick, new UnityAction(gameManager.GetComponent<GameManager>().ReturnToMainMenu));
        }
        else
        {
            Debug.LogWarning("GameManager'da ReturnToMainMenu fonksiyonu bulunamadı! Lütfen GameManager scriptine 'public void ReturnToMainMenu()' ekleyin.");
        }

        // Ayarlar Butonu
        GameObject settingsHolder = null;
        
        // Hiyerarşideki inaktif dahil tüm objelerden SettingsMenuHolder'ı ismine göre bulalım
        Transform[] allTransforms = Resources.FindObjectsOfTypeAll<Transform>();
        foreach (Transform t in allTransforms)
        {
            if (t.name == "SettingsMenuHolder" && t.gameObject.scene.isLoaded)
            {
                settingsHolder = t.gameObject;
                break;
            }
        }

        if (settingsHolder != null)
        {
            gameManager.settingsMenuUI = settingsHolder; // GameManager'a tanıt
            
            // Eğer SettingsMenuHolder bir Canvas'ın içindeyse (nested ise) kendi sırasını kullanması için override'ı açalım
            Canvas sCanvas = settingsHolder.GetComponent<Canvas>();
            if (sCanvas != null)
            {
                sCanvas.overrideSorting = true;
                sCanvas.sortingOrder = 100;
            }

            // Ayarlar tuşuna basınca: Pause Menüsü kapanmasın, Ayarlar Menüsü açılsın.
            UnityEditor.Events.UnityEventTools.AddBoolPersistentListener(btnAyarlar.onClick, new UnityAction<bool>(settingsHolder.SetActive), true);
        }
        else
        {
            Debug.LogWarning("Sahnede SettingsMenuHolder bulunamadı! Ayarlar butonu boş bırakıldı.");
        }

        gameManager.pausePanel = holder; // GameManager'a bağla
        holder.SetActive(false); // Başlangıçta gizli

        // --- ANA MENÜ AYARLAR BUTONUNU DÜZELTME ---
        // Eskiden Ana Menü'deki Settings butonu PauseGame çağırıyordu, bunu düzeltmeliyiz!
        GameObject mainSettingsBtnObj = GameObject.Find("settings");
        if (mainSettingsBtnObj != null)
        {
            Button mainSettingsBtn = mainSettingsBtnObj.GetComponent<Button>();
            if (mainSettingsBtn != null && settingsHolder != null)
            {
                // Mevcut OnClick eventlerini temizle (sadece bizim eklediğimiz PauseGame'i silmek zor, bu yüzden temizleyip baştan ekliyoruz)
                UnityEditor.Events.UnityEventTools.RemovePersistentListener(mainSettingsBtn.onClick, new UnityAction(gameManager.PauseGame));
                
                // Artık Ana menüdeki Ayarlar tuşu DİREKT olarak Settings menüsünü açsın, Pause menüsünü çağırmasın!
                UnityEditor.Events.UnityEventTools.AddBoolPersistentListener(mainSettingsBtn.onClick, new UnityAction<bool>(settingsHolder.SetActive), true);
            }
        }

        EditorUtility.SetDirty(gameManager); // Yapılan atamaların Unity tarafından kaydedilmesi için
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(gameManager.gameObject.scene);

        Debug.Log("Pause Menüsü başarıyla oluşturuldu! Ana Menü ayarlar tuşu da düzeltildi.");
        Selection.activeGameObject = pausePanel;
    }

    #region Helpers
    private static void SetRectFull(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    private static TextMeshProUGUI CreateText(Transform parent, string content, float size, TextAlignmentOptions align)
    {
        GameObject txtObj = new GameObject("Text_" + content);
        txtObj.transform.SetParent(parent, false);
        TextMeshProUGUI txt = txtObj.AddComponent<TextMeshProUGUI>();
        txt.text = content;
        txt.fontSize = size;
        txt.alignment = align;
        txt.color = Color.white;
        if (customFont != null) txt.font = customFont;
        return txt;
    }

    private static GameObject CreateButton(Transform parent, string text, out Button btn)
    {
        GameObject obj = new GameObject("Button_" + text);
        obj.transform.SetParent(parent, false);
        
        Image img = obj.AddComponent<Image>();
        img.color = new Color(0.1f, 0.1f, 0.1f, 0.8f);
        
        btn = obj.AddComponent<Button>();

        // Layout elementi ekleyelim ki VerticalLayoutGroup güzel davransın
        LayoutElement le = obj.AddComponent<LayoutElement>();
        le.minHeight = 60;
        
        TextMeshProUGUI txt = CreateText(obj.transform, text, 32, TextAlignmentOptions.Center);
        SetRectFull(txt.rectTransform);

        return obj;
    }

    private static void SetBackground(Image img)
    {
        // SettingsMenuGenerator ile aynı arka plan resmini bulmaya çalışalım
        string[] guids = AssetDatabase.FindAssets("t:Sprite");
        foreach (var guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (path.Contains("Gemini_Generated_Image"))
            {
                Sprite s = AssetDatabase.LoadAssetAtPath<Sprite>(path);
                if (s != null)
                {
                    img.sprite = s;
                    img.type = Image.Type.Sliced;
                    return;
                }
            }
        }
        
        // Bulunamazsa gri arkaplan
        img.color = new Color(0.2f, 0.2f, 0.2f, 1f);
    }
    #endregion
}
