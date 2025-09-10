using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class MarketSimpleController : MonoBehaviour
{
    [System.Serializable]
    public class SimpleRow
    {
        public WeaponKind kind;        // Pistol / SMG / AK
        public TMP_Text titleText;     // "PISTOL" / "SMG" / "AK"
        public TMP_Text descText;      // "+10% Attack Speed (x/6)"
        public Button buyButton;       // purchase button
        public TMP_Text buyButtonText; // price on button / "MAX"
    }

    [Header("Rows")]
    public SimpleRow pistol;
    public SimpleRow smg;
    public SimpleRow ak;

    [Header("Config")]
    public int maxSteps = 6; // can buy up to 6 times

    void OnEnable()
    {
        Bind(pistol);
        Bind(smg);
        Bind(ak);

        RefreshAll();

        if (GameManager.I != null)
            GameManager.I.OnCoinsChanged += OnCoinsChanged;
    }

    void OnDisable()
    {
        if (GameManager.I != null)
            GameManager.I.OnCoinsChanged -= OnCoinsChanged;
    }

    void OnCoinsChanged(int _) => RefreshAll();

    void Bind(SimpleRow r)
    {
        if (r == null || r.buyButton == null) return;
        r.buyButton.onClick.RemoveAllListeners();
        r.buyButton.onClick.AddListener(() => TryBuy(r));
    }

    void TryBuy(SimpleRow r)
    {
        if (r == null) return;

        int lvl = MarketUpgrades.GetLevel(r.kind);
        if (lvl >= maxSteps) return; // already MAX

        int cost = MarketUpgrades.NextCost(r.kind, maxSteps);
        if (cost < 0) return;

        if (GameManager.I != null && GameManager.I.TrySpendCoins(cost))
        {
            MarketUpgrades.SetLevel(r.kind, lvl + 1);
            Refresh(r);
            // optional SFX / haptics:
            // SfxManager.Play(SfxKey.BuffEquip);
            // Haptics.Try();
        }
        else
        {
            Debug.Log("Not enough coins for: " + r.kind);
        }
    }

    void RefreshAll()
    {
        Refresh(pistol);
        Refresh(smg);
        Refresh(ak);
    }

    void Refresh(SimpleRow r)
    {
        if (r == null) return;

        int lvl = MarketUpgrades.GetLevel(r.kind);
        int cost = MarketUpgrades.NextCost(r.kind, maxSteps);
        bool maxed = (cost < 0);

        if (r.titleText) r.titleText.text = r.kind.ToString().ToUpper();
        if (r.descText) r.descText.text = $"+10% Attack Speed ({Mathf.Min(lvl, maxSteps)}/{maxSteps})";

        if (r.buyButtonText) r.buyButtonText.text = maxed ? "MAX" : cost.ToString();

        if (r.buyButton)
        {
            int coins = GameManager.I ? GameManager.I.Coins : 0;
            r.buyButton.interactable = !maxed && coins >= cost;
        }
    }
}
