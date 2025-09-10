using UnityEngine;
using UnityEngine.UI;

public class HPBarUI : MonoBehaviour
{
    [Header("Refs")]
    public PlayerHealth playerHealth;   // drag your Player here
    public Image hpFillImage;           // Image component of HP_Fill

    [Header("Sprites (0..6)")]
    // index 0 = HP_0, ... index 6 = HP_6 (full)
    public Sprite[] sprites = new Sprite[7];

    void Reset()
    {
        hpFillImage = GetComponent<Image>();
    }

    void OnEnable()
    {
        if (playerHealth != null)
        {
            playerHealth.OnHealthChanged += HandleHPChanged;
            HandleHPChanged(playerHealth.CurrentHP, playerHealth.MaxHP);
        }
    }

    void OnDisable()
    {
        if (playerHealth != null)
            playerHealth.OnHealthChanged -= HandleHPChanged;
    }

    void HandleHPChanged(int current, int max)
    {
        if (max <= 0) max = 1;

        // 0..max HP -> 0..6 sprite index (full = 6, zero = 0)
        int idx = Mathf.Clamp(Mathf.RoundToInt((current / (float)max) * 6f), 0, 6);

        if (sprites != null && idx < sprites.Length && sprites[idx] != null)
        {
            hpFillImage.sprite = sprites[idx];
            // hpFillImage.SetNativeSize(); // enable if aspect gets skewed
        }
    }
}
