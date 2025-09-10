using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

public class MenuController : MonoBehaviour
{
    [Header("Panels")]
    public GameObject mainMenuPanel;     // main menu panel (PLAY/SETTINGS/QUIT)
    public GameObject levelSelectPanel;  // LevelSelect panel
    public GameObject settingsPanel;     // Settings panel
    public GameObject marketPanel;       // added: Market panel

    [Header("Top-left coin (optional)")]
    public TMP_Text coinText; // link if you want a coin text in menu (leave empty if using CoinUI)

    [System.Serializable]
    public class LevelEntry
    {
        public string sceneName;   // like "Level1", "Level2"
        public Button button;      // button for that level
        public TMP_Text costText;  // "Unlocked" / "250 COIN" / "SOON..."
        public bool unlockedByDefault;
        public int unlockCost;

        [Header("Display")]
        public bool alwaysSoon = false;   // if TRUE, always shows "SOON..." and button is disabled
    }

    [Header("Levels")]
    public LevelEntry level1;
    public LevelEntry level2;
    public LevelEntry level3;

    const string PREF = "lvl_unlocked_";

    void OnEnable()
    {
        if (GameManager.I != null) GameManager.I.OnCoinsChanged += OnCoinsChanged;
        OnCoinsChanged(GameManager.I ? GameManager.I.Coins : 0);
        RefreshRows();
        ShowMain(); // show main menu when scene opens
    }

    void OnDisable()
    {
        if (GameManager.I != null) GameManager.I.OnCoinsChanged -= OnCoinsChanged;
    }

    void OnCoinsChanged(int c)
    {
        if (coinText) coinText.text = $"Coins: {c}";
        RefreshRows();
    }

    // === Panel switches ===
    public void ShowMain()
    {
        if (levelSelectPanel) levelSelectPanel.SetActive(false);
        if (settingsPanel) settingsPanel.SetActive(false);
        if (marketPanel) marketPanel.SetActive(false);   // added
        if (mainMenuPanel) mainMenuPanel.SetActive(true);
    }

    public void ShowLevels()
    {
        if (mainMenuPanel) mainMenuPanel.SetActive(false);
        if (settingsPanel) settingsPanel.SetActive(false);
        if (marketPanel) marketPanel.SetActive(false);   // added
        if (levelSelectPanel) levelSelectPanel.SetActive(true);
    }

    public void ShowSettings()
    {
        if (mainMenuPanel) mainMenuPanel.SetActive(false);
        if (levelSelectPanel) levelSelectPanel.SetActive(false);
        if (marketPanel) marketPanel.SetActive(false);   // added
        if (settingsPanel) settingsPanel.SetActive(true);
    }

    public void CloseSettings()
    {
        if (settingsPanel) settingsPanel.SetActive(false);
        if (mainMenuPanel) mainMenuPanel.SetActive(true);
    }

    // new: open/close Market
    public void ShowMarket()
    {
        if (mainMenuPanel) mainMenuPanel.SetActive(false);
        if (levelSelectPanel) levelSelectPanel.SetActive(false);
        if (settingsPanel) settingsPanel.SetActive(false);
        if (marketPanel) marketPanel.SetActive(true);
    }

    public void CloseMarket()
    {
        if (marketPanel) marketPanel.SetActive(false);
        if (mainMenuPanel) mainMenuPanel.SetActive(true);
    }
    // new end

    // === Level buttons ===
    public void OnClickLevel1() => HandleLevel(level1);
    public void OnClickLevel2() => HandleLevel(level2);
    public void OnClickLevel3() => HandleLevel(level3);

    void HandleLevel(LevelEntry e)
    {
        if (e == null || string.IsNullOrEmpty(e.sceneName)) return;

        // if marked SOON..., do nothing
        if (e.alwaysSoon)
        {
            Debug.Log($"{e.sceneName} is marked as SOON...");
            return;
        }

        if (IsUnlocked(e))
        {
            LoadScene(e.sceneName);
            return;
        }

        // if locked: spend coins if enough, unlock, then load
        if (GameManager.I != null && GameManager.I.TrySpendCoins(e.unlockCost))
        {
            SetUnlocked(e, true);
            RefreshRows();
            LoadScene(e.sceneName);
        }
        else
        {
            Debug.Log($"Not enough coins for {e.sceneName}. Need {e.unlockCost}");
            // you could play SFX or a red flash here
        }
    }

    void RefreshRows()
    {
        Setup(level1);
        Setup(level2);
        Setup(level3);
    }

    void Setup(LevelEntry e)
    {
        if (e == null) return;

        // TMP: keep it single-line / avoid overflow (newer API)
        if (e.costText != null)
        {
            e.costText.textWrappingMode = TextWrappingModes.NoWrap;     // instead of enableWordWrapping
            e.costText.overflowMode = TextOverflowModes.Ellipsis;       // show "..." if it overflows
            // e.costText.enableAutoSizing = true; // turn on if you want
        }

        // fixed "SOON..." display
        if (e.alwaysSoon)
        {
            if (e.costText) e.costText.text = "SOON...";
            if (e.button) e.button.interactable = false;
            return;
        }

        bool unlocked = IsUnlocked(e);

        // COST label
        if (e.costText)
        {
            if (e.unlockedByDefault && e.unlockCost <= 0)
                e.costText.text = "UNLOCKED";
            else
                e.costText.text = unlocked ? "UNLOCKED" : $"{e.unlockCost} COIN";
        }

        // button state
        if (e.button)
            e.button.interactable = unlocked || ((GameManager.I?.Coins ?? 0) >= e.unlockCost);
    }

    bool IsUnlocked(LevelEntry e)
    {
        if (e.unlockedByDefault) return true;
        return PlayerPrefs.GetInt(PREF + e.sceneName, 0) == 1;
    }

    void SetUnlocked(LevelEntry e, bool v)
    {
        PlayerPrefs.SetInt(PREF + e.sceneName, v ? 1 : 0);
        PlayerPrefs.Save();
    }

    void LoadScene(string sceneName)
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(sceneName);
    }

    public void Quit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
