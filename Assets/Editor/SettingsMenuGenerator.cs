using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using TMPro;
using UnityEngine.Events;

public class SettingsMenuGenerator : EditorWindow
{
    private static TMP_FontAsset customFont;

    [MenuItem("Tools/Eski Menuyu Onar (Sadece Tuslar)")]
    public static void FixOldMenu()
    {
        KeybindUI[] keybinds = Resources.FindObjectsOfTypeAll<KeybindUI>();
        if (keybinds.Length == 0)
        {
            Debug.LogWarning("Sahnede hiçbir KeybindUI bulunamadı!");
            return;
        }
        foreach (var k in keybinds)
        {
            if (k.actionName == "Car/Accelerate" || (k.actionName == "Driving/Move" && k.compositePartName == "")) k.compositePartName = "up";
            if (k.actionName == "Car/Brake") k.compositePartName = "down";
            if (k.actionName == "Car/Steer") { k.compositePartName = "left"; /* Saga için ayrı buton varsa manuel düzeltilmeli */ }
            if (k.actionName == "Car/Handbrake") k.compositePartName = "";
            
            if (k.actionName.StartsWith("Car/")) k.actionName = k.actionName.Replace("Car/", "Driving/");
            
            // Eğer bindingIndex eskiden kalmışsa (artık string kullanıyoruz ama scriptte eski değerler varsa)
            string parentName = k.transform.parent.name;
            if (parentName.Contains("Ileri") || parentName.Contains("Gaz")) { k.actionName = "Driving/Move"; k.compositePartName = "up"; }
            if (parentName.Contains("Geri") || parentName.Contains("Fren")) { k.actionName = "Driving/Move"; k.compositePartName = "down"; }
            if (parentName.Contains("Sola")) { k.actionName = "Driving/Move"; k.compositePartName = "left"; }
            if (parentName.Contains("Saga")) { k.actionName = "Driving/Move"; k.compositePartName = "right"; }
        }
        
        Debug.Log("Mevcut menüdeki tuş bağlantıları onarıldı! Artık Dpad hatası vermeyecek.");
    }

    [MenuItem("Tools/Tus Ayarlarini Sifirla (Reset)")]
    public static void ResetKeybinds()
    {
        PlayerPrefs.DeleteKey("rebinds");
        PlayerPrefs.Save();
        Debug.Log("Tüm tuş atamaları sıfırlandı! Lütfen oyunu durdurup tekrar Play'e basın.");
    }

    [MenuItem("Tools/Ayarlar Menusu Olustur")]
    public static void CreateMenu()
    {
        GameManager gameManager = FindObjectOfType<GameManager>();
        if (gameManager == null)
        {
            Debug.LogError("Sahnede GameManager bulunamadı! Lütfen önce bir GameManager ekleyin.");
            return;
        }

        // Font bulma
        string[] guids = AssetDatabase.FindAssets("t:TMP_FontAsset");
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path);
            if (font != null && font.name.Contains("Orbitron"))
            {
                customFont = font;
                break;
            }
        }

        // Ana Taşıyıcı
        GameObject holder = new GameObject("SettingsMenuHolder");
        Canvas canvas = holder.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100; // En üstte görünsün
        CanvasScaler scaler = holder.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        holder.AddComponent<GraphicRaycaster>();

        SettingsManager settingsManager = holder.AddComponent<SettingsManager>();
        gameManager.pausePanel = holder; // GameManager'a bağla

        // Arka Plan Blur (veya yarı saydam siyah)
        GameObject bg = new GameObject("BlurBackground");
        bg.transform.SetParent(holder.transform, false);
        Image bgImg = bg.AddComponent<Image>();
        bgImg.color = new Color(0, 0, 0, 0.8f);
        SetRectFull(bg.GetComponent<RectTransform>());

        // Panel Ana Çerçeve
        GameObject settingsPanel = new GameObject("SettingsPanel");
        settingsPanel.transform.SetParent(holder.transform, false);
        Image panelImg = settingsPanel.AddComponent<Image>();
        panelImg.color = new Color(0.1f, 0.1f, 0.1f, 1f); // Koyu gri
        RectTransform panelRect = settingsPanel.GetComponent<RectTransform>();
        panelRect.sizeDelta = new Vector2(1200, 800);
        panelRect.anchoredPosition = Vector2.zero;

        // Death Panel
        GameObject deathPanel = new GameObject("DeathPanel");
        deathPanel.transform.SetParent(holder.transform, false);
        Image deathImg = deathPanel.AddComponent<Image>();
        deathImg.color = new Color(0.2f, 0, 0, 0.95f);
        SetRectFull(deathPanel.GetComponent<RectTransform>());
        
        TextMeshProUGUI deathText = CreateText(deathPanel.transform, "OLDUN!", 100, TextAlignmentOptions.Center);
        deathText.rectTransform.anchoredPosition = new Vector2(0, 100);
        
        TextMeshProUGUI respawnText = CreateText(deathPanel.transform, "5 Saniye Icinde Ana Menuye Donuluyor...", 40, TextAlignmentOptions.Center);
        respawnText.rectTransform.anchoredPosition = new Vector2(0, -100);
        
        gameManager.deathPanel = deathPanel;
        deathPanel.SetActive(false); // Başlangıçta kapalı

        // Kapatmak için arkaplana buton özelliği
        Button bgBtn = bg.AddComponent<Button>();
        UnityEditor.Events.UnityEventTools.AddBoolPersistentListener(bgBtn.onClick, new UnityAction<bool>(holder.SetActive), false);

        // Başlık
        TextMeshProUGUI title = CreateText(settingsPanel.transform, "AYARLAR", 48, TextAlignmentOptions.Center);
        title.rectTransform.anchorMin = new Vector2(0.5f, 1);
        title.rectTransform.anchorMax = new Vector2(0.5f, 1);
        title.rectTransform.pivot = new Vector2(0.5f, 1);
        title.rectTransform.anchoredPosition = new Vector2(0, -55);
        title.rectTransform.sizeDelta = new Vector2(300, 50);

        // X Butonu
        GameObject closeBtnObj = CreateButton(settingsPanel.transform, "X", new Vector2(60, 60), out Button closeBtn);
        RectTransform cRect = closeBtnObj.GetComponent<RectTransform>();
        cRect.anchorMin = new Vector2(1, 1);
        cRect.anchorMax = new Vector2(1, 1);
        cRect.pivot = new Vector2(1, 1);
        cRect.anchoredPosition = new Vector2(-20, -20);
        closeBtn.GetComponent<Image>().color = new Color(0.8f, 0.2f, 0.2f, 1f);
        UnityEditor.Events.UnityEventTools.AddBoolPersistentListener(closeBtn.onClick, new UnityAction<bool>(holder.SetActive), false);

        // Sekmeler (Tabs)
        GameObject tabsContainer = new GameObject("TabsContainer");
        tabsContainer.transform.SetParent(settingsPanel.transform, false);
        RectTransform tabsRect = tabsContainer.AddComponent<RectTransform>();
        tabsRect.anchorMin = new Vector2(0.5f, 1);
        tabsRect.anchorMax = new Vector2(0.5f, 1);
        tabsRect.pivot = new Vector2(0.5f, 1);
        tabsRect.anchoredPosition = new Vector2(0, -130);
        tabsRect.sizeDelta = new Vector2(1100, 60);

        HorizontalLayoutGroup tabsLayout = tabsContainer.AddComponent<HorizontalLayoutGroup>();
        tabsLayout.spacing = 20;
        tabsLayout.childForceExpandHeight = true;
        tabsLayout.childForceExpandWidth = true;

        GameObject btnSesObj = CreateButton(tabsContainer.transform, "SES", new Vector2(350, 60), out Button btnSes);
        GameObject btnGoruntuObj = CreateButton(tabsContainer.transform, "GORUNTU", new Vector2(350, 60), out Button btnGoruntu);
        GameObject btnKontrolObj = CreateButton(tabsContainer.transform, "KONTROLLER", new Vector2(350, 60), out Button btnKontrol);

        // Sayfalar
        GameObject pageSes = CreatePage(settingsPanel.transform, "Page_Ses");
        GameObject pageGoruntu = CreatePage(settingsPanel.transform, "Page_Goruntu");
        GameObject pageKontrol = CreatePage(settingsPanel.transform, "Page_Kontrol");

        // --- SES SAYFASI ---
        settingsManager.masterVolumeSlider = CreateSliderRow(pageSes.transform, "Ana Ses");
        settingsManager.menuVolumeSlider = CreateSliderRow(pageSes.transform, "Menu Sesi");
        settingsManager.musicVolumeSlider = CreateSliderRow(pageSes.transform, "Muzik Sesi");
        settingsManager.vfxVolumeSlider = CreateSliderRow(pageSes.transform, "Efekt Sesi");

        // --- GÖRÜNTÜ SAYFASI ---
        settingsManager.resolutionText = CreateResolutionRow(pageGoruntu.transform, settingsManager);
        settingsManager.fullscreenText = CreateFullscreenRow(pageGoruntu.transform, settingsManager);
        settingsManager.brightnessSlider = CreateSliderRow(pageGoruntu.transform, "Parlaklik");
        settingsManager.qualityMarkers = CreateQualityRow(pageGoruntu.transform, settingsManager);

        // --- KONTROLLER SAYFASI (YAYA / ARAÇ SEKMELİ) ---
        // Alt sekme butonları için yatay düzen
        GameObject subTabsContainer = new GameObject("SubTabsContainer");
        subTabsContainer.transform.SetParent(pageKontrol.transform, false);
        RectTransform subTabsRect = subTabsContainer.AddComponent<RectTransform>();
        subTabsRect.sizeDelta = new Vector2(1100, 60);
        
        HorizontalLayoutGroup subTabsLayout = subTabsContainer.AddComponent<HorizontalLayoutGroup>();
        subTabsLayout.childControlWidth = false;
        subTabsLayout.childForceExpandWidth = false;
        subTabsLayout.spacing = 20;
        subTabsLayout.childAlignment = TextAnchor.MiddleCenter;

        GameObject btnYayaObj = CreateButton(subTabsContainer.transform, "YAYA KONTROLLERİ", new Vector2(300, 60), out Button btnYaya);
        GameObject btnAracObj = CreateButton(subTabsContainer.transform, "ARAÇ KONTROLLERİ", new Vector2(300, 60), out Button btnArac);

        // Yaya Paneli
        GameObject yayaPanel = new GameObject("YayaPanel");
        yayaPanel.transform.SetParent(pageKontrol.transform, false);
        VerticalLayoutGroup yayaLayout = yayaPanel.AddComponent<VerticalLayoutGroup>();
        yayaLayout.spacing = 15;
        yayaLayout.childControlHeight = false;
        yayaLayout.childForceExpandHeight = false;

        // Araç Paneli
        GameObject aracPanel = new GameObject("AracPanel");
        aracPanel.transform.SetParent(pageKontrol.transform, false);
        VerticalLayoutGroup aracLayout = aracPanel.AddComponent<VerticalLayoutGroup>();
        aracLayout.spacing = 15;
        aracLayout.childControlHeight = false;
        aracLayout.childForceExpandHeight = false;

        // Bekleme Ekranı (Tuş atanırken çıkacak)
        GameObject waitingPanel = new GameObject("WaitingPanel");
        waitingPanel.transform.SetParent(canvas.transform, false);
        Image waitImg = waitingPanel.AddComponent<Image>();
        waitImg.color = new Color(0, 0, 0, 0.9f);
        SetRectFull(waitingPanel.GetComponent<RectTransform>());
        TextMeshProUGUI waitText = CreateText(waitingPanel.transform, "Yeni bir tusa basin...", 48, TextAlignmentOptions.Center);
        waitText.rectTransform.anchoredPosition = Vector2.zero;
        waitingPanel.SetActive(false);

        // Yaya Tuşları
        CreateKeybindRow(yayaPanel.transform, "Parça ile Etkilesim", "Player/Pickup", waitingPanel);
        CreateKeybindRow(yayaPanel.transform, "Nesne ile Etkilesim", "Player/Interact", waitingPanel);
        CreateKeybindRow(yayaPanel.transform, "Sürükleme / Kapı Açma", "Player/Attack", waitingPanel);
        CreateKeybindRow(yayaPanel.transform, "Kosma", "Player/Sprint", waitingPanel);
        CreateKeybindRow(yayaPanel.transform, "Egilme", "Player/Crouch", waitingPanel);
        CreateKeybindRow(yayaPanel.transform, "Zoom", "Player/Zoom", waitingPanel);

        // Araç Tuşları
        CreateKeybindRow(aracPanel.transform, "Ileri / Gaz", "Driving/Move", waitingPanel, "up");
        CreateKeybindRow(aracPanel.transform, "Geri / Fren", "Driving/Move", waitingPanel, "down");
        CreateKeybindRow(aracPanel.transform, "Sola", "Driving/Move", waitingPanel, "left");
        CreateKeybindRow(aracPanel.transform, "Saga", "Driving/Move", waitingPanel, "right");
        CreateKeybindRow(aracPanel.transform, "El Freni", "Driving/Handbrake", waitingPanel);

        // Alt Sekme Buton Bağlantıları
        UnityEditor.Events.UnityEventTools.AddBoolPersistentListener(btnYaya.onClick, new UnityAction<bool>(yayaPanel.SetActive), true);
        UnityEditor.Events.UnityEventTools.AddBoolPersistentListener(btnYaya.onClick, new UnityAction<bool>(aracPanel.SetActive), false);
        
        UnityEditor.Events.UnityEventTools.AddBoolPersistentListener(btnArac.onClick, new UnityAction<bool>(aracPanel.SetActive), true);
        UnityEditor.Events.UnityEventTools.AddBoolPersistentListener(btnArac.onClick, new UnityAction<bool>(yayaPanel.SetActive), false);

        // Başlangıçta Yaya paneli açık olsun
        yayaPanel.SetActive(true);
        aracPanel.SetActive(false);

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
        holder.SetActive(false); // Başlangıçta ayarlar menüsü kapalı

        // --- ANA MENÜ BUTONLARINI BAĞLAMA ---
        GameObject settingsBtnObj = GameObject.Find("settings");
        if (settingsBtnObj != null)
        {
            Button settingsBtn = settingsBtnObj.GetComponent<Button>();
            if (settingsBtn == null) settingsBtn = settingsBtnObj.AddComponent<Button>(); // Eğer Button yoksa otomatik ekle
            
            UnityEditor.Events.UnityEventTools.AddPersistentListener(settingsBtn.onClick, new UnityAction(gameManager.PauseGame));
        }

        GameObject playBtnObj = GameObject.Find("play");
        if (playBtnObj != null)
        {
            Button playBtn = playBtnObj.GetComponent<Button>();
            if (playBtn == null) playBtn = playBtnObj.AddComponent<Button>(); // Eğer Button yoksa otomatik ekle
            
            UnityEditor.Events.UnityEventTools.AddPersistentListener(playBtn.onClick, new UnityAction(gameManager.StartGame));
        }

        GameObject mainMenuObj = GameObject.Find("MainMenuUI");
        if (mainMenuObj != null)
        {
            gameManager.mainMenuUI = mainMenuObj;
        }
        else if (playBtnObj != null)
        {
            Canvas parentCanvas = playBtnObj.GetComponentInParent<Canvas>();
            if (parentCanvas != null)
            {
                gameManager.mainMenuUI = parentCanvas.gameObject;
            }
        }
        // ---------------------------------

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

        string[] levels = { "Düsük", "Orta", "Yüksek" };
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

    private static void CreateKeybindRow(Transform parent, string labelText, string actionName, GameObject waitingPanel, string compositePartName = "")
    {
        GameObject row = CreateRow(parent, labelText);

        GameObject btnObj = CreateButton(row.transform, "...", new Vector2(250, 50), out Button btn);
        KeybindUI keybindUI = btnObj.AddComponent<KeybindUI>();
        keybindUI.actionName = actionName;
        keybindUI.compositePartName = compositePartName;
        keybindUI.buttonText = btnObj.GetComponentInChildren<TextMeshProUGUI>();
        keybindUI.waitingPanel = waitingPanel;

        UnityEditor.Events.UnityEventTools.AddPersistentListener(btn.onClick, new UnityAction(keybindUI.Rebind));
    }
    #endregion
}
