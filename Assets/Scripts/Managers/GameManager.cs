using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;
using TMPro;
using System.Collections;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("UI References")]
    [Tooltip("Durdurma/Ayarlar menüsünün ana GameObejesi")]
    public GameObject pausePanel; 
    
    [Header("Death Screen")]
    public GameObject deathPanel;
    public TextMeshProUGUI deathCountdownText;

    public GameObject mainMenuUI; // Kullanıcının ana menü objelerini koyacağı referans
    public GameObject settingsMenuUI; // Ayarlar menüsünü tutan referans

    private bool isPaused = false;
    private bool isDead = false;
    private bool isGameStarted = false; // Oyunun başlayıp başlamadığını kontrol edecek

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        int currentSceneIndex = SceneManager.GetActiveScene().buildIndex;

        if (currentSceneIndex == 0) // Ana Menü Sahnesi
        {
            isGameStarted = false;
            UnlockCursor();
            SetPlayerControlActive(false);

            if (mainMenuUI != null) mainMenuUI.SetActive(true);
            if (pausePanel != null) pausePanel.SetActive(false);
            if (settingsMenuUI != null) settingsMenuUI.SetActive(false);
            if (deathPanel != null) deathPanel.SetActive(false);
        }
        else // Oyun Sahnesi
        {
            isGameStarted = true;
            LockCursor();
            SetPlayerControlActive(true);

            if (mainMenuUI != null) mainMenuUI.SetActive(false);
            if (pausePanel != null) pausePanel.SetActive(false);
            if (settingsMenuUI != null) settingsMenuUI.SetActive(false);
            if (deathPanel != null) deathPanel.SetActive(false);
        }

        // Oyun başladığında kaydedilmiş tuş ayarlarını yükle
        string rebinds = PlayerPrefs.GetString("rebinds", string.Empty);
        if (!string.IsNullOrEmpty(rebinds))
        {
            PlayerInput playerInput = Object.FindAnyObjectByType<PlayerInput>();
            if (playerInput != null && playerInput.actions != null)
            {
                playerInput.actions.LoadBindingOverridesFromJson(rebinds);
            }
        }
    }

    public void StartGame()
    {
        SceneManager.LoadScene(1);
        isGameStarted = true;
        if (mainMenuUI != null) mainMenuUI.SetActive(false);
        LockCursor();
        SetPlayerControlActive(true);
    }

    private void Update()
    {
        if (isDead || !isGameStarted) return; // Oyun başlamadıysa ESC veya Pause işlemez

        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            if (isPaused)
            {
                // Eğer ayarlar menüsü açıksa, onu kapatıp Pause menüsüne dön.
                if (settingsMenuUI != null && settingsMenuUI.activeSelf)
                {
                    settingsMenuUI.SetActive(false);
                    if (pausePanel != null) pausePanel.SetActive(true);
                }
                else
                {
                    ResumeGame();
                }
            }
            else
            {
                PauseGame();
            }
        }
    }

    public void PauseGame()
    {
        isPaused = true;
        Time.timeScale = 0f;

        if (pausePanel != null) pausePanel.SetActive(true);
        if (settingsMenuUI != null) settingsMenuUI.SetActive(false); // Sadece pause açılmalı

        SetPlayerControlActive(false);
        UnlockCursor();
    }

    public void ResumeGame()
    {
        isPaused = false;
        Time.timeScale = 1f;

        if (pausePanel != null) pausePanel.SetActive(false);
        if (settingsMenuUI != null) settingsMenuUI.SetActive(false); // Ayarlar açıksa o da kapansın

        if (isGameStarted)
        {
            SetPlayerControlActive(true);
            LockCursor();
        }
    }

    public void PlayerDied()
    {
        if (isDead) return;
        isDead = true;

        // Oyunun arkada tamamen kilitlenmesi için zamanı durduruyoruz
        Time.timeScale = 0f;

        if (pausePanel != null) pausePanel.SetActive(false);
        if (deathPanel != null) deathPanel.SetActive(true);

        SetPlayerControlActive(false);
        UnlockCursor();

        StartCoroutine(DeathCountdownCoroutine());
    }

    private IEnumerator DeathCountdownCoroutine()
    {
        int countdown = 3;
        while (countdown > 0)
        {
            if (deathCountdownText != null)
                deathCountdownText.text = countdown + " Saniye Icinde Ana Menuye Donuluyor...";
            
            // Time.timeScale = 0 olma ihtimaline karşı gerçek zamanlı sayım
            yield return new WaitForSecondsRealtime(1f);
            countdown--;
        }

        // Oyun başladığı duruma (Ana Menü) geri dönmek için 0. sahneyi yükle
        Time.timeScale = 1f;
        SceneManager.LoadScene(0);
    }

    private void SetPlayerControlActive(bool active)
    {
        // Karakter scriptlerini bulup devredışı bırakıyoruz ki
        // fareyi hareket ettirince arkada karakter de dönmesin.
        PlayerLook playerLook = Object.FindFirstObjectByType<PlayerLook>();
        if (playerLook != null) playerLook.enabled = active;

        PlayerController playerController = Object.FindFirstObjectByType<PlayerController>();
        if (playerController != null) playerController.enabled = active;
    }

    private void LockCursor()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void UnlockCursor()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void ReturnToMainMenu()
    {
        Time.timeScale = 1f;
        // Ana menüye dönmek için 0. indexteki sahneyi yükle
        SceneManager.LoadScene(0);
    }

    public void ExitGame()
    {
        Debug.Log("Oyundan Cikiliyor...");
        #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
        #else
            Application.Quit();
        #endif
    }
}
