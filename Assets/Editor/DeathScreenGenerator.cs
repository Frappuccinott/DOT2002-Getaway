using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;

public class DeathScreenGenerator : EditorWindow
{
    [MenuItem("Tools/Death Screen Olustur")]
    public static void CreateDeathScreen()
    {
        // GameManager'ı bul
        GameManager gm = FindFirstObjectByType<GameManager>();
        if (gm == null)
        {
            Debug.LogError("Sahne'de GameManager bulunamadi!");
            return;
        }

        // Ana Menü Canvas'ını bul (GameManager'da tanımlı olan Canvas)
        if (gm.mainMenuUI == null)
        {
            Debug.LogError("GameManager'da Main Menu UI atanmamis! Lutfen Canvas'i GameManager'a bagla.");
            return;
        }

        Transform canvasTransform = gm.mainMenuUI.transform;

        // Varsa eski DeathScreen'i sil
        Transform oldDeathScreen = canvasTransform.Find("DeathScreenHolder");
        if (oldDeathScreen != null)
        {
            DestroyImmediate(oldDeathScreen.gameObject);
        }

        // Ana Holder Objesi
        GameObject deathHolder = new GameObject("DeathScreenHolder");
        deathHolder.transform.SetParent(canvasTransform, false);
        RectTransform deathRect = deathHolder.AddComponent<RectTransform>();
        SetRectFull(deathRect);
        
        // Arka Plan (Tamamen Siyah)
        GameObject bgObj = new GameObject("Background");
        bgObj.transform.SetParent(deathHolder.transform, false);
        RectTransform bgRect = bgObj.AddComponent<RectTransform>();
        SetRectFull(bgRect);
        Image bgImage = bgObj.AddComponent<Image>();
        bgImage.color = new Color(0f, 0f, 0f, 1f); // Simsiyah, %100 opak
        bgImage.raycastTarget = true; // Tıklamaları engellesin

        // ÖLDÜN! Yazısı
        GameObject oldunObj = new GameObject("OldunText");
        oldunObj.transform.SetParent(deathHolder.transform, false);
        RectTransform oldunRect = oldunObj.AddComponent<RectTransform>();
        oldunRect.anchorMin = new Vector2(0.5f, 0.5f);
        oldunRect.anchorMax = new Vector2(0.5f, 0.5f);
        oldunRect.pivot = new Vector2(0.5f, 0.5f);
        oldunRect.anchoredPosition = new Vector2(0f, 50f);
        oldunRect.sizeDelta = new Vector2(800f, 200f);
        
        TextMeshProUGUI oldunText = oldunObj.AddComponent<TextMeshProUGUI>();
        oldunText.text = "ÖLDÜN!";
        oldunText.fontSize = 150f;
        oldunText.color = Color.red;
        oldunText.alignment = TextAlignmentOptions.Center;
        oldunText.fontStyle = FontStyles.Bold;

        // Geri Sayım Yazısı
        GameObject countdownObj = new GameObject("CountdownText");
        countdownObj.transform.SetParent(deathHolder.transform, false);
        RectTransform countdownRect = countdownObj.AddComponent<RectTransform>();
        countdownRect.anchorMin = new Vector2(0.5f, 0.5f);
        countdownRect.anchorMax = new Vector2(0.5f, 0.5f);
        countdownRect.pivot = new Vector2(0.5f, 0.5f);
        countdownRect.anchoredPosition = new Vector2(0f, -100f);
        countdownRect.sizeDelta = new Vector2(800f, 100f);

        TextMeshProUGUI countdownText = countdownObj.AddComponent<TextMeshProUGUI>();
        countdownText.text = "3 Saniye Icinde Ana Menuye Donuluyor...";
        countdownText.fontSize = 40f;
        countdownText.color = Color.white;
        countdownText.alignment = TextAlignmentOptions.Center;

        // GameManager'a bağla
        gm.deathPanel = deathHolder;
        gm.deathCountdownText = countdownText;

        // Varsayılan olarak kapalı başlat
        deathHolder.SetActive(false);

        EditorUtility.SetDirty(gm);

        Debug.Log("Death Screen basariyla eklendi ve GameManager'a baglandi!");
    }

    private static void SetRectFull(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }
}
