using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class SettingsManager : MonoBehaviour
{
    [Header("Tabs")]
    public GameObject audioTab;
    public GameObject graphicsTab;
    public GameObject controlsTab;

    [Header("Graphics UI")]
    public TextMeshProUGUI resolutionText;
    public TextMeshProUGUI fullscreenText;
    public GameObject[] qualityMarkers; // 0: Düşük, 1: Orta, 2: Yüksek
    public Slider brightnessSlider;

    [Header("Audio UI")]
    public Slider masterVolumeSlider;
    public Slider menuVolumeSlider;
    public Slider musicVolumeSlider;
    public Slider vfxVolumeSlider;

    private Resolution[] resolutions;
    private int currentResolutionIndex = 0;
    private bool isFullscreen = true;

    private void Start()
    {
        InitializeResolutions();
        LoadSettings();
    }

    #region Tab Management
    public void OpenAudioTab()
    {
        audioTab.SetActive(true);
        graphicsTab.SetActive(false);
        controlsTab.SetActive(false);
    }

    public void OpenGraphicsTab()
    {
        audioTab.SetActive(false);
        graphicsTab.SetActive(true);
        controlsTab.SetActive(false);
    }

    public void OpenControlsTab()
    {
        audioTab.SetActive(false);
        graphicsTab.SetActive(false);
        controlsTab.SetActive(true);
    }
    #endregion

    #region Graphics Settings
    private void InitializeResolutions()
    {
        resolutions = Screen.resolutions;
        currentResolutionIndex = 0;

        int savedWidth = PlayerPrefs.GetInt("ResWidth", Screen.currentResolution.width);
        int savedHeight = PlayerPrefs.GetInt("ResHeight", Screen.currentResolution.height);

        for (int i = 0; i < resolutions.Length; i++)
        {
            if (resolutions[i].width == savedWidth && resolutions[i].height == savedHeight)
            {
                currentResolutionIndex = i;
                break;
            }
        }
        UpdateResolutionText();
    }

    public void NextResolution()
    {
        currentResolutionIndex++;
        if (currentResolutionIndex >= resolutions.Length) currentResolutionIndex = 0;
        ApplyResolution();
    }

    public void PreviousResolution()
    {
        currentResolutionIndex--;
        if (currentResolutionIndex < 0) currentResolutionIndex = resolutions.Length - 1;
        ApplyResolution();
    }

    private void ApplyResolution()
    {
        UpdateResolutionText();
        Screen.SetResolution(resolutions[currentResolutionIndex].width, resolutions[currentResolutionIndex].height, isFullscreen);
        PlayerPrefs.SetInt("ResWidth", resolutions[currentResolutionIndex].width);
        PlayerPrefs.SetInt("ResHeight", resolutions[currentResolutionIndex].height);
        PlayerPrefs.Save();
    }

    private void UpdateResolutionText()
    {
        if (resolutionText != null && resolutions.Length > 0)
            resolutionText.text = resolutions[currentResolutionIndex].width + "x" + resolutions[currentResolutionIndex].height;
    }

    public void ToggleFullscreen()
    {
        isFullscreen = !isFullscreen;
        if (fullscreenText != null) fullscreenText.text = isFullscreen ? "Tam Ekran" : "Pencereli";
        Screen.fullScreen = isFullscreen;
        PlayerPrefs.SetInt("Fullscreen", isFullscreen ? 1 : 0);
        PlayerPrefs.Save();
    }

    public void SetQuality(int qualityIndex)
    {
        QualitySettings.SetQualityLevel(qualityIndex);
        PlayerPrefs.SetInt("QualityLevel", qualityIndex);
        PlayerPrefs.Save();
        UpdateQualityMarkers(qualityIndex);
    }

    private void UpdateQualityMarkers(int index)
    {
        for (int i = 0; i < qualityMarkers.Length; i++)
        {
            if (qualityMarkers[i] != null)
                qualityMarkers[i].SetActive(i == index);
        }
    }

    public void SetBrightness(float value)
    {
        PlayerPrefs.SetFloat("Brightness", value);
        PlayerPrefs.Save();
        // Parlaklık uygulaması oyunun Post Processing Volume ayarlarından yapılmalı.
        // Bu yüzden şimdilik sadece kaydediyoruz. 
    }
    #endregion

    #region Audio Settings
    public void SetMasterVolume(float volume)
    {
        AudioListener.volume = volume;
        PlayerPrefs.SetFloat("MasterVolume", volume);
        PlayerPrefs.Save();
    }

    public void SetMenuVolume(float volume)
    {
        PlayerPrefs.SetFloat("MenuVolume", volume);
        PlayerPrefs.Save();
    }

    public void SetMusicVolume(float volume)
    {
        PlayerPrefs.SetFloat("MusicVolume", volume);
        PlayerPrefs.Save();
    }

    public void SetVFXVolume(float volume)
    {
        PlayerPrefs.SetFloat("VFXVolume", volume);
        PlayerPrefs.Save();
    }
    #endregion

    private void LoadSettings()
    {
        // Görüntü
        isFullscreen = PlayerPrefs.GetInt("Fullscreen", 1) == 1;
        if (fullscreenText != null) fullscreenText.text = isFullscreen ? "Tam Ekran" : "Pencereli";
        Screen.fullScreen = isFullscreen;

        int qualityIndex = PlayerPrefs.GetInt("QualityLevel", QualitySettings.GetQualityLevel());
        UpdateQualityMarkers(qualityIndex);

        if (brightnessSlider != null) brightnessSlider.value = PlayerPrefs.GetFloat("Brightness", 0.5f);

        // Ses
        if (masterVolumeSlider != null)
        {
            masterVolumeSlider.value = PlayerPrefs.GetFloat("MasterVolume", 1f);
            SetMasterVolume(masterVolumeSlider.value);
        }
        if (menuVolumeSlider != null) menuVolumeSlider.value = PlayerPrefs.GetFloat("MenuVolume", 1f);
        if (musicVolumeSlider != null) musicVolumeSlider.value = PlayerPrefs.GetFloat("MusicVolume", 1f);
        if (vfxVolumeSlider != null) vfxVolumeSlider.value = PlayerPrefs.GetFloat("VFXVolume", 1f);
    }
}
