using UnityEngine;
using UnityEngine.Serialization;

public class PlayerEquip : MonoBehaviour
{
    [Header("Visuals")]
    public Transform gfx;                 // child that holds SpriteRenderer; auto-found if empty
    public SpriteRenderer spriteRenderer; // SpriteRenderer under gfx
    public Sprite unarmedSprite;          // no-weapon sprite
    [FormerlySerializedAs("armedSprite")]
    public Sprite gunArmedSprite;         // pistol / gun sprite
    public Sprite smgArmedSprite;         // SMG sprite

    [Header("AK-47")]
    public Sprite ak47ArmedSprite;        // AK-47 sprite (new)
    [Tooltip("AK preset: assign a bullet prefab with 2x damage (e.g., Bullet_AK).")]
    public ShooterPreset ak47Preset = new ShooterPreset
    {
        bulletSpeed = 20f,
        fireInterval = 0.1667f, // if pistol is 0.25s, this is ~1.5x faster
        detectRadius = 17f
    };
    [Tooltip("Derive AK fire rate from the pistol baseline (fireInterval = gun / 1.5).")]
    public bool deriveAKFromGun = true;
    public float akSpeedMultiplierVsGun = 1.5f; // compare vs pistol speed

    [Header("Shooter")]
    public AutoAimShooter shooter;        // AutoAimShooter on the player
    public bool overrideShooterValues = true;

    [System.Serializable]
    public class ShooterPreset
    {
        public GameObject bulletPrefab;
        public float bulletSpeed = 18f;
        public float fireInterval = 0.25f;
        public float detectRadius = 15f;
    }

    [FormerlySerializedAs("equippedPreset")]
    public ShooterPreset gunPreset = new ShooterPreset();   // pistol preset

    public ShooterPreset smgPreset = new ShooterPreset      // SMG preset
    {
        bulletSpeed = 20f,
        fireInterval = 0.12f, // ~2x vs pistol (0.25)
        detectRadius = 17f
    };

    bool equipped;

    void Awake()
    {
        if (gfx == null)
        {
            var sr = GetComponentInChildren<SpriteRenderer>();
            if (sr) gfx = sr.transform;
        }

        if (spriteRenderer == null && gfx != null)
            spriteRenderer = gfx.GetComponentInChildren<SpriteRenderer>();

        if (shooter == null)
            shooter = GetComponentInChildren<AutoAimShooter>();
    }

    // === Pistol / Gun (GunBuff) ===
    public void EquipWeapon()
    {
        if (!equipped) equipped = true;

        // 1) Sprite
        if (spriteRenderer != null && gunArmedSprite != null)
            spriteRenderer.sprite = gunArmedSprite;

        // 2) Shooter preset
        ApplyPresetToShooter(gunPreset);

        // MARKET: apply Attack Speed (reduces fireInterval)
        if (shooter != null)
            shooter.fireInterval = shooter.fireInterval * MarketUpgrades.FireIntervalScale(WeaponKind.Pistol);

        // 3) Enable shooter
        if (shooter != null) shooter.enabled = true;
    }

    // === SMG (SubmachineBuff) ===
    public void EquipWeaponSMG()
    {
        if (!equipped) equipped = true;

        // 1) Sprite (fallback to gun if no SMG sprite)
        if (spriteRenderer != null)
        {
            var spr = smgArmedSprite ? smgArmedSprite : gunArmedSprite;
            if (spr) spriteRenderer.sprite = spr;
        }

        // 2) Shooter preset
        ApplyPresetToShooter(smgPreset);

        // MARKET: apply Attack Speed (reduces fireInterval)
        if (shooter != null)
            shooter.fireInterval = shooter.fireInterval * MarketUpgrades.FireIntervalScale(WeaponKind.SMG);

        // 3) Enable shooter
        if (shooter != null) shooter.enabled = true;
    }

    // === AK-47 (2x damage, ~1.5x attack speed) ===
    public void EquipWeaponAK47()
    {
        if (!equipped) equipped = true;

        // 1) Sprite (fallback to SMG/Gun if no AK sprite)
        if (spriteRenderer != null)
        {
            var spr = ak47ArmedSprite ? ak47ArmedSprite :
                      (smgArmedSprite ? smgArmedSprite : gunArmedSprite);
            if (spr) spriteRenderer.sprite = spr;
        }

        // 2) Shooter preset (interval = gun / 1.5)
        ShooterPreset p = ak47Preset;
        if (deriveAKFromGun && gunPreset != null && gunPreset.fireInterval > 0f)
        {
            p = new ShooterPreset
            {
                bulletPrefab = ak47Preset.bulletPrefab, // should be the 2x damage bullet prefab
                bulletSpeed = ak47Preset.bulletSpeed,
                detectRadius = ak47Preset.detectRadius,
                fireInterval = gunPreset.fireInterval / Mathf.Max(0.01f, akSpeedMultiplierVsGun)
            };
        }

        ApplyPresetToShooter(p);

        // MARKET: apply Attack Speed (reduces fireInterval)
        if (shooter != null)
            shooter.fireInterval = shooter.fireInterval * MarketUpgrades.FireIntervalScale(WeaponKind.AK);

        // 3) Enable shooter
        if (shooter != null) shooter.enabled = true;
    }

    // revert if needed
    public void UnequipWeapon()
    {
        if (!equipped) return;
        equipped = false;

        if (spriteRenderer != null && unarmedSprite != null)
            spriteRenderer.sprite = unarmedSprite;

        if (shooter != null)
            shooter.enabled = false;
    }

    // --- Helpers ---
    void ApplyPresetToShooter(ShooterPreset p)
    {
        if (!overrideShooterValues || shooter == null || p == null) return;

        if (p.bulletPrefab != null) shooter.bulletPrefab = p.bulletPrefab; // for AK, assign the 2x damage bullet here
        shooter.bulletSpeed = p.bulletSpeed;
        shooter.fireInterval = p.fireInterval;
        shooter.detectRadius = p.detectRadius;
    }
}
