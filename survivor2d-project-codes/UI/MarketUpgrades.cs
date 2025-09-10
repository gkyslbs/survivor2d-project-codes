using UnityEngine;

public enum WeaponKind { Pistol, SMG, AK }

public static class MarketUpgrades
{
    const string KEY_PREFIX = "mk_lvl_";
    static readonly int[] Costs = { 20, 40, 80, 160, 320, 640 }; // 6 steps

    public static int GetLevel(WeaponKind kind)
    {
        return Mathf.Clamp(PlayerPrefs.GetInt(KEY_PREFIX + kind, 0), 0, Costs.Length);
    }

    public static void SetLevel(WeaponKind kind, int level)
    {
        level = Mathf.Clamp(level, 0, Costs.Length);
        PlayerPrefs.SetInt(KEY_PREFIX + kind, level);
        PlayerPrefs.Save();
    }

    // maxSteps: 6
    public static int NextCost(WeaponKind kind, int maxSteps)
    {
        int lvl = GetLevel(kind);
        if (lvl >= maxSteps) return -1; // MAX
        int idx = Mathf.Min(lvl, Costs.Length - 1);
        return Costs[idx];
    }

    // Each step gives +10% fire rate. You can change per-step via PlayerPrefs if needed.
    public static float FireRateMultiplier(WeaponKind kind)
    {
        int lvl = GetLevel(kind);
        float perStep = PlayerPrefs.GetFloat("mk_rate_perstep", 0.10f); // 10%
        return 1f + perStep * lvl;
    }

    // Shooter.fireInterval = baseInterval * FireIntervalScale(...)
    public static float FireIntervalScale(WeaponKind kind)
    {
        return 1f / FireRateMultiplier(kind);
    }

    public static void ResetAll()
    {
        PlayerPrefs.DeleteKey(KEY_PREFIX + WeaponKind.Pistol);
        PlayerPrefs.DeleteKey(KEY_PREFIX + WeaponKind.SMG);
        PlayerPrefs.DeleteKey(KEY_PREFIX + WeaponKind.AK);
    }
}
