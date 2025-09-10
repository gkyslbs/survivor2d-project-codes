using UnityEngine;
using UnityEngine.SceneManagement;

public class ResetProgress : MonoBehaviour
{
    [Header("Level unlock keys (scene names)")]
    // List the scene names whose unlock keys should be cleared
    public string[] levelSceneNamesToClear = { "Level2", "Level3" };

    [Header("Post-reset reload")]
    public bool reloadScene = true;          // reload a scene after the reset
    public string mainMenuSceneName = "";    // if empty, reloads the current scene

    // Keep keys consistent with GameManager
    const string COINS_KEY = "coins_persist";
    const string LVL_PREFIX = "lvl_unlocked_";

    // Hook this to a Settings/Restart button
    public void OnHardResetButton()
    {
        // 1) Clear coins (persistent)
        PlayerPrefs.DeleteKey(COINS_KEY);

        // 2) Reset market upgrades (persistent)
        MarketUpgrades.ResetAll();

        // 3) Clear level unlock flags (persistent)
        if (levelSceneNamesToClear != null)
        {
            for (int i = 0; i < levelSceneNamesToClear.Length; i++)
            {
                var name = levelSceneNamesToClear[i];
                if (!string.IsNullOrEmpty(name))
                    PlayerPrefs.DeleteKey(LVL_PREFIX + name);
            }
        }

        PlayerPrefs.Save();

        // 4) Also zero out the runtime coin value and refresh UI
        if (GameManager.I != null) GameManager.I.ResetCoins(0);

        // 5) Optional: reload menu (or current scene)
        if (reloadScene)
        {
            if (!string.IsNullOrEmpty(mainMenuSceneName))
                SceneManager.LoadScene(mainMenuSceneName);
            else
                SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
    }
}
