using UnityEngine;
using UnityEngine.UI;

public class SettingsMenuController : MonoBehaviour
{
    [Header("UI Panels")]
    public GameObject settingsPanel;
    public GameObject backgroundBlurPanel; // Yarı saydam siyah arka plan veya blur materyalli panel

    private void Start()
    {
        // Başlangıçta ayarlar menüsünün kapalı olduğundan emin olalım
        if (settingsPanel != null) settingsPanel.SetActive(false);
        if (backgroundBlurPanel != null) backgroundBlurPanel.SetActive(false);
    }

    // Ayarlar butonuna tıklandığında çağrılacak fonksiyon
    public void OpenSettings()
    {
        if (backgroundBlurPanel != null) backgroundBlurPanel.SetActive(true);
        if (settingsPanel != null) settingsPanel.SetActive(true);
        
        // Eğer oyun içi menüyse ve oyunu durdurmak isterseniz:
        // Time.timeScale = 0f;
    }

    // Ayarlar menüsündeki "Kapat" veya "Geri" butonuna tıklandığında çağrılacak fonksiyon
    public void CloseSettings()
    {
        if (settingsPanel != null) settingsPanel.SetActive(false);
        if (backgroundBlurPanel != null) backgroundBlurPanel.SetActive(false);
        
        // Eğer oyunu durdurduysanız devam ettirmek için:
        // Time.timeScale = 1f;
    }
}
