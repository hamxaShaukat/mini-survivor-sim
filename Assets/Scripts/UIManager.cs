using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class UIManager : MonoBehaviour
{
    [Header("UI Panels")]
    [SerializeField] private GameObject mainMenuPanel;
    [SerializeField] private GameObject settingsPanel;
    
    [Header("Main Menu Buttons")]
    [SerializeField] private Button playButton;
    [SerializeField] private Button settingsButton;
    [SerializeField] private Button quitButton;
    
    [Header("Settings Panel Buttons")]
    [SerializeField] private Button backButton;
    [SerializeField] private Button applyButton;
    
    [Header("Scene Management")]
    [SerializeField] private string gameSceneName = "GameScene";

    void Start()
    {
        InitializeUI();
        SetupButtonListeners();
    }

    void InitializeUI()
    {
        // Start with only main menu visible
        mainMenuPanel.SetActive(true);
        settingsPanel.SetActive(false);
    }

    void SetupButtonListeners()
    {
        // Main Menu Buttons
        playButton.onClick.AddListener(OnPlayClicked);
        settingsButton.onClick.AddListener(OnSettingsClicked);
        quitButton.onClick.AddListener(OnQuitClicked);
        
        // Settings Panel Buttons
        backButton.onClick.AddListener(OnBackClicked);
        applyButton.onClick.AddListener(OnApplyClicked);
    }

    #region Main Menu Functions
    
    void OnPlayClicked()
    {
        Debug.Log("Play button clicked!");
        
        // Load your game scene
        SceneManager.LoadScene(gameSceneName);
        
        // Or if you want a loading screen:
        // StartCoroutine(LoadGameSceneAsync());
    }
    
    void OnSettingsClicked()
    {
        Debug.Log("Settings button clicked!");
        
        // Switch to settings panel
        mainMenuPanel.SetActive(false);
        settingsPanel.SetActive(true);
        
        // Optional: Play sound
        // AudioManager.Instance.PlayButtonClick();
    }
    
    void OnQuitClicked()
    {
        Debug.Log("Quit button clicked!");
        
        #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
        #else
            Application.Quit();
        #endif
    }
    
    #endregion

    #region Settings Panel Functions
    
    void OnBackClicked()
    {
        Debug.Log("Back button clicked!");
        
        // Return to main menu
        settingsPanel.SetActive(false);
        mainMenuPanel.SetActive(true);
        
        // Optional: Reset settings if not applied
        // ResetSettingsToPrevious();
    }
    
    void OnApplyClicked()
    {
        Debug.Log("Apply button clicked!");
        
        // Apply all settings changes
        ApplyAllSettings();
        
        // Optional: Show confirmation message
        // ShowMessage("Settings applied successfully!");
        
        // Optional: Auto-return to main menu
        // OnBackClicked();
    }
    
    void ApplyAllSettings()
    {
        // Implement your settings application logic here
        // Examples:
        
        // 1. Graphics Settings
        // QualitySettings.SetQualityLevel(graphicsDropdown.value);
        
        // 2. Audio Settings
        // AudioListener.volume = volumeSlider.value;
        
        // 3. Game Settings
        // PlayerPrefs.SetInt("Difficulty", difficultyDropdown.value);
        // PlayerPrefs.Save();
        
        Debug.Log("Settings applied!");
    }
    
    #endregion

    // Optional: Handle Escape key for back navigation
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (settingsPanel.activeSelf)
            {
                OnBackClicked();
            }
        }
    }
}