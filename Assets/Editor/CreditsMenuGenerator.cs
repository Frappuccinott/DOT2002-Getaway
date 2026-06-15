using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using TMPro;
using UnityEngine.Events;

public class CreditsMenuGenerator : EditorWindow
{
    private static TMP_FontAsset customFont;

    [MenuItem("Tools/Credits Menusu Olustur")]
    public static void CreateMenu()
    {
        // Font bulma (Kullanıcının isteği üzerine 'Button' fontunu bul)
        string[] fontGuids = AssetDatabase.FindAssets("Button t:TMP_FontAsset");
        if (fontGuids.Length > 0)
        {
            customFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(AssetDatabase.GUIDToAssetPath(fontGuids[0]));
        }
        else
        {
            // Bulamazsa ilk bulduğu fontu al
            fontGuids = AssetDatabase.FindAssets("t:TMP_FontAsset");
            if (fontGuids.Length > 0)
                customFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(AssetDatabase.GUIDToAssetPath(fontGuids[0]));
        }

        // Bütün eski menüleri bulup sil
        GameObject[] allObjects = Resources.FindObjectsOfTypeAll<GameObject>();
        foreach (GameObject obj in allObjects)
        {
            if (obj.name == "CreditsMenuHolder" && obj.scene.isLoaded)
            {
                DestroyImmediate(obj);
            }
        }

        // Kök Obje ve Canvas
        GameObject holder = new GameObject("CreditsMenuHolder");
        Canvas canvas = holder.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 99; // Pause ile aynı seviyede olabilir veya üstünde olabilir
        
        CanvasScaler scaler = holder.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080); 
        
        holder.AddComponent<GraphicRaycaster>();

        RectTransform hRect = holder.GetComponent<RectTransform>();
        SetRectFull(hRect);

        // Arkaplan Koyulaştırma/Blur
        GameObject bg = new GameObject("BlurBackground");
        bg.transform.SetParent(holder.transform, false);
        RectTransform bgRect = bg.AddComponent<RectTransform>();
        SetRectFull(bgRect);
        Image bgImg = bg.AddComponent<Image>();
        bgImg.color = new Color(0, 0, 0, 0.8f);

        // Ana Panel (Biraz daha uzattık ki yazılar rahat sığsın ve üst üste binmesin: 500x750)
        GameObject creditsPanel = new GameObject("CreditsPanel");
        creditsPanel.transform.SetParent(holder.transform, false);
        RectTransform pRect = creditsPanel.AddComponent<RectTransform>();
        pRect.anchorMin = new Vector2(0.5f, 0.5f);
        pRect.anchorMax = new Vector2(0.5f, 0.5f);
        pRect.pivot = new Vector2(0.5f, 0.5f);
        pRect.sizeDelta = new Vector2(500, 750); 

        Image pImage = creditsPanel.AddComponent<Image>();
        SetBackground(pImage);

        // Başlık (Daha yukarı aldık)
        TextMeshProUGUI title = CreateText(creditsPanel.transform, "CREDITS", 45, TextAlignmentOptions.Center);
        title.rectTransform.anchorMin = new Vector2(0.5f, 1);
        title.rectTransform.anchorMax = new Vector2(0.5f, 1);
        title.rectTransform.pivot = new Vector2(0.5f, 1);
        title.rectTransform.anchoredPosition = new Vector2(0, -40); // -60'tan -40'a çıkarıldı
        title.rectTransform.sizeDelta = new Vector2(400, 60);

        // İsimler için Container (Daha aşağı aldık)
        GameObject listContainer = new GameObject("ListContainer");
        listContainer.transform.SetParent(creditsPanel.transform, false);
        RectTransform listRect = listContainer.AddComponent<RectTransform>();
        listRect.anchorMin = new Vector2(0.5f, 0.5f);
        listRect.anchorMax = new Vector2(0.5f, 0.5f);
        listRect.pivot = new Vector2(0.5f, 0.5f);
        listRect.anchoredPosition = new Vector2(0, 0); // 20'den 0'a indirildi
        listRect.sizeDelta = new Vector2(480, 500); // Biraz daha büyütüldü

        VerticalLayoutGroup layout = listContainer.AddComponent<VerticalLayoutGroup>();
        layout.spacing = 15;
        layout.childAlignment = TextAnchor.UpperCenter;
        layout.childControlHeight = true;
        layout.childControlWidth = true;

        // Ekibi Ekle (ğ ve ş hariç Türkçe karakterler geri getirildi)
        AddCredit(listContainer.transform, "Mehmet Emin Çoban", "Lead Developer");
        AddCredit(listContainer.transform, "Arda Oktar", "Developer");
        AddCredit(listContainer.transform, "Yigit Aydın", "Level Designer");
        AddCredit(listContainer.transform, "Muhammed Burak Özbek", "Game Designer");
        AddCredit(listContainer.transform, "Arda Özgür Kaya", "UI Design & UI Developer");
        AddCredit(listContainer.transform, "Berkay Isık", "Audio Director");

        // Kapat Butonu
        GameObject closeBtnObj = CreateButton(creditsPanel.transform, "KAPAT", out Button closeBtn);
        RectTransform closeRect = closeBtnObj.GetComponent<RectTransform>();
        closeRect.anchorMin = new Vector2(0.5f, 0);
        closeRect.anchorMax = new Vector2(0.5f, 0);
        closeRect.pivot = new Vector2(0.5f, 0);
        closeRect.anchoredPosition = new Vector2(0, 70);

        // Buton kapattığında menüyü gizlesin
        UnityEditor.Events.UnityEventTools.AddVoidPersistentListener(closeBtn.onClick, new UnityAction(
            () => holder.SetActive(false) // Editörde UnityEventTools ile lambda eklemek her zaman çalışmaz.
        ));
        
        // Lambda yerine doğrudan SetActive bağlayalım
        UnityEditor.Events.UnityEventTools.AddBoolPersistentListener(closeBtn.onClick, new UnityAction<bool>(holder.SetActive), false);
        
        // Arkaplana da tıklayınca kapansın
        Button bgBtn = bg.AddComponent<Button>();
        bgBtn.transition = Selectable.Transition.None;
        UnityEditor.Events.UnityEventTools.AddBoolPersistentListener(bgBtn.onClick, new UnityAction<bool>(holder.SetActive), false);

        // Menüyü kapalı başlat ki sadece tıklanınca açılsın
        holder.SetActive(false);

        Debug.Log("Credits Menusu basariyla olusturuldu!");
    }

    private static void AddCredit(Transform parent, string name, string role)
    {
        GameObject item = new GameObject("CreditItem_" + name);
        item.transform.SetParent(parent, false);
        
        VerticalLayoutGroup vLayout = item.AddComponent<VerticalLayoutGroup>();
        vLayout.childAlignment = TextAnchor.MiddleCenter;
        vLayout.childControlHeight = true;
        vLayout.childControlWidth = true;
        vLayout.spacing = 0; // İsim ve rol birbirine yapışık olsun

        TextMeshProUGUI nameText = CreateText(item.transform, name, 28, TextAlignmentOptions.Center);
        nameText.fontStyle = FontStyles.Bold;

        TextMeshProUGUI roleText = CreateText(item.transform, role, 18, TextAlignmentOptions.Center);
        roleText.color = new Color(0.7f, 0.7f, 0.7f, 1f); // Biraz daha soluk renk
    }

    private static TextMeshProUGUI CreateText(Transform parent, string content, int fontSize, TextAlignmentOptions align)
    {
        GameObject go = new GameObject("Text");
        go.transform.SetParent(parent, false);
        TextMeshProUGUI tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = content;
        tmp.fontSize = fontSize;
        tmp.alignment = align;
        if (customFont != null) tmp.font = customFont;
        tmp.color = Color.white;
        return tmp;
    }

    private static GameObject CreateButton(Transform parent, string textStr, out Button btnComponent)
    {
        GameObject btnObj = new GameObject("Button_" + textStr);
        btnObj.transform.SetParent(parent, false);
        RectTransform r = btnObj.AddComponent<RectTransform>();
        r.sizeDelta = new Vector2(250, 50);

        Image img = btnObj.AddComponent<Image>();
        img.color = new Color(0.1f, 0.1f, 0.1f, 0.9f); // Koyu renk

        btnComponent = btnObj.AddComponent<Button>();
        ColorBlock cb = btnComponent.colors;
        cb.normalColor = Color.white;
        cb.highlightedColor = new Color(0.8f, 0.8f, 0.8f);
        cb.pressedColor = new Color(0.5f, 0.5f, 0.5f);
        btnComponent.colors = cb;

        TextMeshProUGUI tmp = CreateText(btnObj.transform, textStr, 24, TextAlignmentOptions.Center);
        tmp.rectTransform.anchorMin = Vector2.zero;
        tmp.rectTransform.anchorMax = Vector2.one;
        tmp.rectTransform.sizeDelta = Vector2.zero;
        
        return btnObj;
    }

    private static void SetBackground(Image img)
    {
        img.color = new Color(0.05f, 0.05f, 0.05f, 0.9f); // Koyu arka plan
        
        Texture2D tex = new Texture2D(2, 2);
        tex.SetPixels(new Color[] { Color.white, Color.white, Color.white, Color.white });
        tex.Apply();
        
        Sprite sprite = Sprite.Create(tex, new Rect(0, 0, 2, 2), new Vector2(0.5f, 0.5f));
        img.sprite = sprite;
        img.type = Image.Type.Sliced;
    }

    private static void SetRectFull(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }
}
