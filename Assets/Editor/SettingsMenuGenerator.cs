using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Events;

public class SettingsMenuGenerator : EditorWindow
{
    private static TMP_FontAsset customFont;

    [MenuItem("Tools/Ayarlar Menusu Olustur")]
    public static void GenerateSettingsMenu()
    {
        string[] fontGuids = AssetDatabase.FindAssets("Button t:TMP_FontAsset");
        if (fontGuids.Length > 0)
        {
            customFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(AssetDatabase.GUIDToAssetPath(fontGuids[0]));
        }

        // Eski objeleri temizle
        GameObject oldCanvas = GameObject.Find("SettingsCanvas");
        if (oldCanvas != null) DestroyImmediate(oldCanvas);

        // 1. Yeni Canvas
        GameObject canvasObj = new GameObject("SettingsCanvas");
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;

        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        canvasObj.AddComponent<GraphicRaycaster>();

        // EventSystem
        if (FindObjectOfType<UnityEngine.EventSystems.EventSystem>() == null)
        {
            GameObject eventSystem = new GameObject("EventSystem");
            eventSystem.AddComponent<UnityEngine.EventSystems.EventSystem>();
            eventSystem.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
        }

        // Managers
        SettingsManager settingsManager = canvasObj.AddComponent<SettingsManager>();
        KeybindManager keybindManager = canvasObj.AddComponent<KeybindManager>();

        // 2. Blur Paneli
        GameObject blurPanel = new GameObject("BlurBackground");
        blurPanel.transform.SetParent(canvas.transform, false);
        Image blurImage = blurPanel.AddComponent<Image>();
        blurImage.color = new Color(0, 0, 0, 0.85f);
        RectTransform blurRect = blurPanel.GetComponent<RectTransform>();
        SetRectFull(blurRect);

        // Kapatmak için arkaplana buton özelliği
        Button blurBtn = blurPanel.AddComponent<Button>();

        // 3. Ana Panel
        GameObject settingsPanel = new GameObject("SettingsPanel");
        settingsPanel.transform.SetParent(canvas.transform, false);
        Image settingsImage = settingsPanel.AddComponent<Image>();
        settingsImage.color = new Color(0.1f, 0.1f, 0.1f, 0.9f);
        RectTransform settingsRect = settingsPanel.GetComponent<RectTransform>();
        settingsRect.sizeDelta = new Vector2(1200, 800);
        settingsRect.anchoredPosition = Vector2.zero;

        // Başlık
        TextMeshProUGUI titleText = CreateText(settingsPanel.transform, "AYARLAR", 48, TextAlignmentOptions.Center);
        titleText.rectTransform.anchorMin = new Vector2(0.5f, 1f);
        titleText.rectTransform.anchorMax = new Vector2(0.5f, 1f);
        titleText.rectTransform.anchoredPosition = new Vector2(0, -50);

        // Kapat Butonu
        GameObject closeBtnObj = CreateButton(settingsPanel.transform, "X", new Vector2(60, 60), out Button closeBtn);
        closeBtnObj.GetComponent<RectTransform>().anchorMin = new Vector2(1, 1);
        closeBtnObj.GetComponent<RectTransform>().anchorMax = new Vector2(1, 1);
        closeBtnObj.GetComponent<RectTransform>().anchoredPosition = new Vector2(-50, -50);
        closeBtnObj.GetComponent<Image>().color = new Color(0.8f, 0.2f, 0.2f);
        
        UnityEditor.Events.UnityEventTools.AddBoolPersistentListener(closeBtn.onClick, new UnityAction<bool>(canvasObj.SetActive), false);
        UnityEditor.Events.UnityEventTools.AddBoolPersistentListener(blurBtn.onClick, new UnityAction<bool>(canvasObj.SetActive), false);

        // Sekme Butonları (Tabs)
        GameObject tabsContainer = new GameObject("TabsContainer");
        tabsContainer.transform.SetParent(settingsPanel.transform, false);
        RectTransform tabsRect = tabsContainer.AddComponent<RectTransform>();
        tabsRect.anchorMin = new Vector2(0, 1);
        tabsRect.anchorMax = new Vector2(1, 1);
        tabsRect.sizeDelta = new Vector2(-100, 60);
        tabsRect.anchoredPosition = new Vector2(0, -120);
        HorizontalLayoutGroup tabsLayout = tabsContainer.AddComponent<HorizontalLayoutGroup>();
        tabsLayout.spacing = 20;
        tabsLayout.childControlWidth = true;
        tabsLayout.childForceExpandWidth = true;

        GameObject btnSesObj = CreateButton(tabsContainer.transform, "SES", new Vector2(0, 60), out Button btnSes);
        GameObject btnGoruntuObj = CreateButton(tabsContainer.transform, "GÖRÜNTÜ", new Vector2(0, 60), out Button btnGoruntu);
        GameObject btnKontrolObj = CreateButton(tabsContainer.transform, "KONTROLLER", new Vector2(0, 60), out Button btnKontrol);

        // Sayfalar
        GameObject pageSes = CreatePage(settingsPanel.transform, "Page_Ses");
        GameObject pageGoruntu = CreatePage(settingsPanel.transform, "Page_Goruntu");
        GameObject pageKontrol = CreatePage(settingsPanel.transform, "Page_Kontrol");

        // --- SES SAYFASI ---
        settingsManager.masterVolumeSlider = CreateSliderRow(pageSes.transform, "Ana Ses");
        settingsManager.menuVolumeSlider = CreateSliderRow(pageSes.transform, "Menü Sesi");
        settingsManager.musicVolumeSlider = CreateSliderRow(pageSes.transform, "Müzik");
        settingsManager.vfxVolumeSlider = CreateSliderRow(pageSes.transform, "VFX");

        // --- GÖRÜNTÜ SAYFASI ---
        settingsManager.resolutionText = CreateResolutionRow(pageGoruntu.transform, settingsManager);
        settingsManager.fullscreenText = CreateFullscreenRow(pageGoruntu.transform, settingsManager);
        settingsManager.qualityMarkers = CreateQualityRow(pageGoruntu.transform, settingsManager);
        settingsManager.brightnessSlider = CreateSliderRow(pageGoruntu.transform, "Parlaklık");

        // --- KONTROLLER SAYFASI ---
        // Scroll View için
        ScrollRect scrollRect = pageKontrol.AddComponent<ScrollRect>();
        GameObject viewport = new GameObject("Viewport");
        viewport.transform.SetParent(pageKontrol.transform, false);
        SetRectFull(viewport.AddComponent<RectTransform>());
        viewport.AddComponent<RectMask2D>();

        GameObject content = new GameObject("Content");
        content.transform.SetParent(viewport.transform, false);
        RectTransform contentRect = content.AddComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0, 1);
        contentRect.anchorMax = new Vector2(1, 1);
        contentRect.pivot = new Vector2(0.5f, 1);
        contentRect.sizeDelta = new Vector2(0, 800);
        
        VerticalLayoutGroup contentLayout = content.AddComponent<VerticalLayoutGroup>();
        contentLayout.spacing = 15;
        contentLayout.padding = new RectOffset(20, 20, 20, 20);
        contentLayout.childControlHeight = false;
        contentLayout.childForceExpandHeight = false;

        scrollRect.content = contentRect;
        scrollRect.viewport = viewport.GetComponent<RectTransform>();
        scrollRect.horizontal = false;
        scrollRect.scrollSensitivity = 30f;

        // Bekleme Ekranı (Tuş atanırken çıkacak)
        GameObject waitingPanel = new GameObject("WaitingPanel");
        waitingPanel.transform.SetParent(canvas.transform, false);
        Image waitImg = waitingPanel.AddComponent<Image>();
        waitImg.color = new Color(0, 0, 0, 0.9f);
        SetRectFull(waitingPanel.GetComponent<RectTransform>());
        TextMeshProUGUI waitText = CreateText(waitingPanel.transform, "Yeni bir tuşa basın...", 48, TextAlignmentOptions.Center);
        waitText.rectTransform.anchoredPosition = Vector2.zero;
        waitingPanel.SetActive(false);

        // Yaya Kontrolleri
        CreateText(content.transform, "YAYA KONTROLLERİ", 32, TextAlignmentOptions.Left).color = Color.yellow;
        CreateKeybindRow(content.transform, "Hareket Etme (İleri)", "Player/Move", waitingPanel);
        CreateKeybindRow(content.transform, "Parça ile Etkileşim", "Player/Pickup", waitingPanel);
        CreateKeybindRow(content.transform, "Nesne ile Etkileşim", "Player/Interact", waitingPanel);
        CreateKeybindRow(content.transform, "Sürükleme / Kapı Açma", "Player/Attack", waitingPanel);
        CreateKeybindRow(content.transform, "Koşma", "Player/Sprint", waitingPanel);
        CreateKeybindRow(content.transform, "Eğilme", "Player/Crouch", waitingPanel);
        CreateKeybindRow(content.transform, "Zoom", "Player/Zoom", waitingPanel);

        // Araç Kontrolleri
        CreateText(content.transform, "", 20, TextAlignmentOptions.Left); // Boşluk
        CreateText(content.transform, "ARAÇ KONTROLLERİ", 32, TextAlignmentOptions.Left).color = Color.yellow;
        // Not: Araç kontrolleri "Player" map'inde değilse bu isimleri projene göre güncellemelisin. Şimdilik genel yazdım.
        CreateKeybindRow(content.transform, "Gaz", "Car/Accelerate", waitingPanel);
        CreateKeybindRow(content.transform, "Fren", "Car/Brake", waitingPanel);
        CreateKeybindRow(content.transform, "Sola / Sağa", "Car/Steer", waitingPanel);
        CreateKeybindRow(content.transform, "El Freni", "Car/Handbrake", waitingPanel);

        // --- BAĞLANTILAR (Wiring) ---
        settingsManager.audioTab = pageSes;
        settingsManager.graphicsTab = pageGoruntu;
        settingsManager.controlsTab = pageKontrol;

        UnityEditor.Events.UnityEventTools.AddPersistentListener(btnSes.onClick, new UnityAction(settingsManager.OpenAudioTab));
        UnityEditor.Events.UnityEventTools.AddPersistentListener(btnGoruntu.onClick, new UnityAction(settingsManager.OpenGraphicsTab));
        UnityEditor.Events.UnityEventTools.AddPersistentListener(btnKontrol.onClick, new UnityAction(settingsManager.OpenControlsTab));

        // Event listenerlar
        UnityEditor.Events.UnityEventTools.AddPersistentListener(settingsManager.masterVolumeSlider.onValueChanged, new UnityAction<float>(settingsManager.SetMasterVolume));
        UnityEditor.Events.UnityEventTools.AddPersistentListener(settingsManager.menuVolumeSlider.onValueChanged, new UnityAction<float>(settingsManager.SetMenuVolume));
        UnityEditor.Events.UnityEventTools.AddPersistentListener(settingsManager.musicVolumeSlider.onValueChanged, new UnityAction<float>(settingsManager.SetMusicVolume));
        UnityEditor.Events.UnityEventTools.AddPersistentListener(settingsManager.vfxVolumeSlider.onValueChanged, new UnityAction<float>(settingsManager.SetVFXVolume));
        UnityEditor.Events.UnityEventTools.AddPersistentListener(settingsManager.brightnessSlider.onValueChanged, new UnityAction<float>(settingsManager.SetBrightness));

        // Başlangıç durumu
        pageSes.SetActive(true);
        pageGoruntu.SetActive(false);
        pageKontrol.SetActive(false);

        Debug.Log("Ayarlar Menüsü tam teşekküllü olarak oluşturuldu!");
        Selection.activeGameObject = settingsPanel;
    }

    #region Helpers
    private static void SetRectFull(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    private static GameObject CreatePage(Transform parent, string name)
    {
        GameObject page = new GameObject(name);
        page.transform.SetParent(parent, false);
        RectTransform rect = page.AddComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = new Vector2(50, 50);
        rect.offsetMax = new Vector2(-50, -180); // Üstten başlık ve sekmeler için pay
        
        VerticalLayoutGroup layout = page.AddComponent<VerticalLayoutGroup>();
        layout.spacing = 30;
        layout.childControlHeight = false;
        layout.childForceExpandHeight = false;
        
        return page;
    }

    private static TextMeshProUGUI CreateText(Transform parent, string text, int size, TextAlignmentOptions align)
    {
        GameObject obj = new GameObject("Text");
        obj.transform.SetParent(parent, false);
        TextMeshProUGUI tmp = obj.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = size;
        tmp.alignment = align;
        tmp.color = Color.white;
        if (customFont != null) tmp.font = customFont;
        return tmp;
    }

    private static GameObject CreateButton(Transform parent, string text, Vector2 size, out Button btn)
    {
        GameObject obj = new GameObject("Button_" + text);
        obj.transform.SetParent(parent, false);
        Image img = obj.AddComponent<Image>();
        img.color = new Color(0.2f, 0.2f, 0.2f, 1f);
        btn = obj.AddComponent<Button>();

        RectTransform rect = obj.GetComponent<RectTransform>();
        if (size != Vector2.zero) rect.sizeDelta = size;

        TextMeshProUGUI tmp = CreateText(obj.transform, text, 24, TextAlignmentOptions.Center);
        SetRectFull(tmp.rectTransform);

        return obj;
    }

    private static GameObject CreateRow(Transform parent, string labelText)
    {
        GameObject row = new GameObject("Row_" + labelText);
        row.transform.SetParent(parent, false);
        RectTransform rect = row.AddComponent<RectTransform>();
        rect.sizeDelta = new Vector2(0, 50);

        HorizontalLayoutGroup layout = row.AddComponent<HorizontalLayoutGroup>();
        layout.childControlWidth = false;
        layout.childForceExpandWidth = false;
        layout.childAlignment = TextAnchor.MiddleLeft;

        TextMeshProUGUI label = CreateText(row.transform, labelText, 28, TextAlignmentOptions.Left);
        label.rectTransform.sizeDelta = new Vector2(400, 50);

        return row;
    }

    private static Slider CreateSliderRow(Transform parent, string labelText)
    {
        GameObject row = CreateRow(parent, labelText);
        
        // Standart Unity UI Slider'ı koduyla yaratmak zordur, bu yüzden basit hiyerarşi kuruyoruz
        GameObject sliderObj = new GameObject("Slider");
        sliderObj.transform.SetParent(row.transform, false);
        RectTransform sliderRect = sliderObj.AddComponent<RectTransform>();
        sliderRect.sizeDelta = new Vector2(400, 20);
        Slider slider = sliderObj.AddComponent<Slider>();

        GameObject bg = new GameObject("Background");
        bg.transform.SetParent(sliderObj.transform, false);
        Image bgImg = bg.AddComponent<Image>();
        bgImg.color = Color.gray;
        SetRectFull(bg.GetComponent<RectTransform>());

        GameObject fillArea = new GameObject("Fill Area");
        fillArea.transform.SetParent(sliderObj.transform, false);
        SetRectFull(fillArea.AddComponent<RectTransform>());

        GameObject fill = new GameObject("Fill");
        fill.transform.SetParent(fillArea.transform, false);
        Image fillImg = fill.AddComponent<Image>();
        fillImg.color = Color.white;
        SetRectFull(fill.GetComponent<RectTransform>());

        slider.fillRect = fill.GetComponent<RectTransform>();
        slider.value = 1f;

        return slider;
    }

    private static TextMeshProUGUI CreateResolutionRow(Transform parent, SettingsManager manager)
    {
        GameObject row = CreateRow(parent, "Çözünürlük");

        GameObject leftBtnObj = CreateButton(row.transform, "<", new Vector2(50, 50), out Button leftBtn);
        UnityEditor.Events.UnityEventTools.AddPersistentListener(leftBtn.onClick, new UnityAction(manager.PreviousResolution));

        TextMeshProUGUI resText = CreateText(row.transform, "1920x1080", 28, TextAlignmentOptions.Center);
        resText.rectTransform.sizeDelta = new Vector2(250, 50);

        GameObject rightBtnObj = CreateButton(row.transform, ">", new Vector2(50, 50), out Button rightBtn);
        UnityEditor.Events.UnityEventTools.AddPersistentListener(rightBtn.onClick, new UnityAction(manager.NextResolution));

        return resText;
    }

    private static TextMeshProUGUI CreateFullscreenRow(Transform parent, SettingsManager manager)
    {
        GameObject row = CreateRow(parent, "Görüntü Modu");
        
        GameObject btnObj = CreateButton(row.transform, "Tam Ekran", new Vector2(250, 50), out Button btn);
        UnityEditor.Events.UnityEventTools.AddPersistentListener(btn.onClick, new UnityAction(manager.ToggleFullscreen));

        return btnObj.GetComponentInChildren<TextMeshProUGUI>();
    }

    private static GameObject[] CreateQualityRow(Transform parent, SettingsManager manager)
    {
        GameObject row = CreateRow(parent, "Grafik Kalitesi");
        GameObject[] markers = new GameObject[3];

        string[] levels = { "Düşük", "Orta", "Yüksek" };
        for(int i=0; i<3; i++)
        {
            int index = i;
            GameObject btnObj = CreateButton(row.transform, levels[i], new Vector2(150, 50), out Button btn);
            UnityEditor.Events.UnityEventTools.AddIntPersistentListener(btn.onClick, new UnityAction<int>(manager.SetQuality), index);
            
            // Marker
            GameObject marker = new GameObject("Marker");
            marker.transform.SetParent(btnObj.transform, false);
            Image img = marker.AddComponent<Image>();
            img.color = Color.yellow;
            RectTransform mRect = marker.GetComponent<RectTransform>();
            mRect.anchorMin = new Vector2(0, 0);
            mRect.anchorMax = new Vector2(1, 0);
            mRect.sizeDelta = new Vector2(0, 5); // Altı çizili gibi
            mRect.anchoredPosition = new Vector2(0, -5);
            markers[i] = marker;
            marker.SetActive(false);
        }

        return markers;
    }

    private static void CreateKeybindRow(Transform parent, string labelText, string actionName, GameObject waitingPanel)
    {
        GameObject row = CreateRow(parent, labelText);
        
        GameObject btnObj = CreateButton(row.transform, "Tuş", new Vector2(200, 50), out Button btn);
        TextMeshProUGUI btnText = btnObj.GetComponentInChildren<TextMeshProUGUI>();

        // Custom script to hold reference
        KeybindUI keybindUI = btnObj.AddComponent<KeybindUI>();
        keybindUI.actionName = actionName;
        keybindUI.buttonText = btnText;
        keybindUI.waitingPanel = waitingPanel;

        UnityEditor.Events.UnityEventTools.AddPersistentListener(btn.onClick, new UnityAction(keybindUI.Rebind));
    }
    #endregion
}
