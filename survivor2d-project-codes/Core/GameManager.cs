using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using System;

public class GameManager : MonoBehaviour
{
    public static GameManager I { get; private set; }

    [Header("UI")]
    public GameObject gameOverPanel;

    [Header("Coins")]
    public int startCoins = 0;
    public TextMeshProUGUI coinText;   // bind this only in scenes where you actually show coins
    public int Coins { get; private set; }
    public event Action<int> OnCoinsChanged;

    public event Action OnGameOver;

    bool isGameOver;

    const string COINS_KEY = "coins_persist";

    void Awake()
    {
        // singleton guard (simple approach for now)
        if (I != null && I != this) { Destroy(gameObject); return; }
        I = this;

        Time.timeScale = 1f;
        if (gameOverPanel) gameOverPanel.SetActive(false);

        // load saved coins if available; otherwise start with startCoins
        if (PlayerPrefs.HasKey(COINS_KEY))
        {
            Coins = Mathf.Max(0, PlayerPrefs.GetInt(COINS_KEY));
            UpdateCoinUI();
        }
        else
        {
            ResetCoins(startCoins);
        }
    }

    void OnEnable() { SceneManager.sceneLoaded += OnSceneLoaded; }
    void OnDisable() { SceneManager.sceneLoaded -= OnSceneLoaded; }

    void OnSceneLoaded(Scene s, LoadSceneMode m)
    {
        // refresh game state on scene load
        isGameOver = false;
        if (gameOverPanel) gameOverPanel.SetActive(false);
        Time.timeScale = 1f;
        UpdateCoinUI(); // make sure coin text is correct after scene changes
    }

    public void GameOver()
    {
        // prevent double game over
        if (isGameOver) return;
        isGameOver = true;

        if (gameOverPanel) gameOverPanel.SetActive(true);
        Time.timeScale = 0f; // pause everything
        OnGameOver?.Invoke();
    }

    public void Restart()
    {
        Time.timeScale = 1f;
        var scn = SceneManager.GetActiveScene();
        if (scn.IsValid()) SceneManager.LoadScene(scn.buildIndex);
        // NOTE: coins are saved in PlayerPrefs, not reset on restart unless ResetCoins is called elsewhere
    }

    public void LoadMenu(string sceneName)
    {
        Time.timeScale = 1f;
        if (!string.IsNullOrEmpty(sceneName)) SceneManager.LoadScene(sceneName);
    }

    // === COINS ===
    public void AddCoins(int amount)
    {
        if (amount <= 0) return; // ignore invalid adds
        Coins += amount;
        UpdateCoinUI();
        SaveCoins();
    }

    public bool TrySpendCoins(int amount)
    {
        if (amount <= 0) return true; // spending 0 is fine
        if (Coins < amount) return false;

        Coins -= amount;
        UpdateCoinUI();
        SaveCoins();
        return true;
    }

    // alias, kept for convenience in call sites
    public bool TrySpend(int amount) => TrySpendCoins(amount);

    public void ResetCoins(int value = 0)
    {
        Coins = Mathf.Max(0, value);
        UpdateCoinUI();
        SaveCoins();
    }

    void UpdateCoinUI()
    {
        if (coinText) coinText.text = Coins.ToString();
        OnCoinsChanged?.Invoke(Coins); // notify any listeners (UI, etc.)
    }

    void SaveCoins()
    {
        PlayerPrefs.SetInt(COINS_KEY, Coins);
        PlayerPrefs.Save(); // NOTE: okay since not called too frequently
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        // editor-only preview for the coin label
        if (!Application.isPlaying && coinText)
            coinText.text = startCoins.ToString();
    }
#endif
}
