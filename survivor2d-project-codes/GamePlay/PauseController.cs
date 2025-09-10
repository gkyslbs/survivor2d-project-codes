using UnityEngine;
using UnityEngine.SceneManagement;

public class PauseController : MonoBehaviour
{
    [Header("Panels (keep them siblings)")]
    public GameObject pausePanel;     // Continue / Settings / MainMenu
    public GameObject settingsPanel;  // settings UI (sliders/toggles)

    [Header("Main Menu")]
    public string mainMenuSceneName = "MainMenu";

    [Header("Hotkey (Editor testing)")]
    public KeyCode toggleKey = KeyCode.Escape;

    bool paused;
    bool settingsOpen;

    void Awake()
    {
        HideAll(); // ensure both panels start hidden
    }

    void Update()
    {
        if (Input.GetKeyDown(toggleKey))
        {
            if (!paused) ShowPause();
            else if (settingsOpen) BackFromSettings();
            else Resume();
        }
    }

    public void ShowPause()
    {
        paused = true;
        settingsOpen = false;

        Time.timeScale = 0f;
        AudioListener.pause = true;

        if (pausePanel) pausePanel.SetActive(true);
        if (settingsPanel) settingsPanel.SetActive(false);
    }

    public void Resume()
    {
        paused = false;
        settingsOpen = false;

        Time.timeScale = 1f;
        AudioListener.pause = false;

        HideAll(); // hide both panels
    }

    public void OpenSettings()
    {
        // show Settings as a separate panel under Pause
        if (!paused) ShowPause(); // make sure game is paused
        settingsOpen = true;

        if (pausePanel) pausePanel.SetActive(false);
        if (settingsPanel) settingsPanel.SetActive(true);
    }

    public void BackFromSettings()
    {
        settingsOpen = false;

        if (settingsPanel) settingsPanel.SetActive(false);
        if (pausePanel) pausePanel.SetActive(true);
    }

    public void GoMainMenu()
    {
        Time.timeScale = 1f;
        AudioListener.pause = false;
        HideAll();
        SceneManager.LoadScene(mainMenuSceneName);
    }

    // simple quit hook for buttons
    public void Quit()
    {
        Time.timeScale = 1f;
        AudioListener.pause = false;
        HideAll();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    void HideAll()
    {
        if (pausePanel) pausePanel.SetActive(false);
        if (settingsPanel) settingsPanel.SetActive(false);
    }

    void OnDisable()
    {
        // safety: normalize when leaving the scene
        Time.timeScale = 1f;
        AudioListener.pause = false;
    }
}
