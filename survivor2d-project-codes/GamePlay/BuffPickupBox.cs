using UnityEngine;
using TMPro;

[RequireComponent(typeof(Collider2D))]
public class BuffPickupBox : MonoBehaviour
{
    [Header("Setup")]
    public bool isSMG = false;          // GunBuff = false, SubmachineBuff = true
    public bool isAK = false;           // NEW: AK-47 buff (true equips AK)
    public int cost = 0;                // GunBuff = 0 (FREE), Submachine/AK ~ 40 etc.
    public float holdTime = 1.0f;       // time to hold inside trigger (sec)
    public string playerTag = "Player";
    public bool destroyOnPurchase = true;

    [Header("World-Space Visuals")]
    public SpriteRenderer dashedFrame;  // dashed square
    public SpriteRenderer fill;         // fill bar (pivot should be Bottom)
    public TMP_Text label;              // "FREE" or a number like "50"

    [Header("Colors")]
    public Color fillColorReady = new Color(0.2f, 1f, 0.6f, 0.7f);
    public Color fillColorCant = new Color(1f, 0.2f, 0.2f, 0.7f);

    [Header("FX (optional)")]
    public ParticleSystem equipVfxPrefab;

    // audio while holding, and short pings on complete/cancel
    [Header("Audio (optional)")]
    public AudioSource audioSrc;            // AS_Charge on the prefab root
    public AudioClip holdLoopClip;          // charging loop (import with Loop=ON)
    [Range(0f, 1f)] public float holdLoopVol = 0.6f;
    public bool playOnlyIfAffordable = true; // if not enough coins, don’t play loop
    public AudioClip completeClip;          // short ping when purchased
    [Range(0f, 1f)] public float completeVol = 0.9f;
    public AudioClip cancelClip;            // ping when purchase fails
    [Range(0f, 1f)] public float cancelVol = 0.5f;

    float hold;
    bool inside;
    PlayerEquip playerEquip;   // cached from the triggering player

    void Reset()
    {
        var col = GetComponent<Collider2D>();
        if (col) col.isTrigger = true;
    }

    void OnEnable()
    {
        SetLabel();
        SetFill(0f, Affordable());
        StopHoldSfx(); // just in case
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag(playerTag)) return;
        inside = true;

        // get PlayerEquip
        playerEquip = other.GetComponent<PlayerEquip>();
        if (!playerEquip) playerEquip = other.GetComponentInParent<PlayerEquip>();

        // when entering, decide loop SFX by affordability
        UpdateHoldSfx(Affordable());
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag(playerTag)) return;
        inside = false;
        hold = 0f;
        SetFill(0f, Affordable());
        StopHoldSfx();
    }

    void Update()
    {
        if (!inside) return;

        bool can = Affordable();

        // check loop SFX state each frame (based on can/inside)
        UpdateHoldSfx(can);

        if (can)
        {
            hold += Time.deltaTime;
            SetFill(hold / Mathf.Max(0.01f, holdTime), true);

            if (hold >= holdTime)
            {
                if (TryPurchase())
                {
                    EquipNow();
                    // success ping
                    if (completeClip && audioSrc) audioSrc.PlayOneShot(completeClip, completeVol);
                    StopHoldSfx();
                    if (destroyOnPurchase) Destroy(gameObject);
                }
                else
                {
                    // rare: coins became insufficient exactly now
                    hold = 0f;
                    SetFill(0f, false);
                    if (cancelClip && audioSrc) audioSrc.PlayOneShot(cancelClip, cancelVol);
                    // loop will be stopped below if not affordable
                }
            }
        }
        else
        {
            hold = 0f;
            SetFill(0f, false);
            // if not affordable, UpdateHoldSfx() will stop the loop
        }
    }

    // === helpers ===
    bool Affordable()
    {
        if (cost <= 0) return true;
        if (GameManager.I == null) return false;
        return GameManager.I.Coins >= cost;
    }

    bool TryPurchase()
    {
        if (cost <= 0) return true;
        if (GameManager.I == null) return false;
        return GameManager.I.TrySpendCoins(cost); // spend via GameManager
    }

    void EquipNow()
    {
        if (playerEquip != null)
        {
            // priority: AK > SMG > Gun (if multiple are true)
            if (isAK) playerEquip.EquipWeaponAK47();
            else if (isSMG) playerEquip.EquipWeaponSMG();
            else playerEquip.EquipWeapon();
        }

        SfxManager.Play(SfxKey.BuffEquip);

        if (equipVfxPrefab)
        {
            var vfx = Instantiate(equipVfxPrefab, transform.position, Quaternion.identity);
            Destroy(vfx.gameObject, 2.5f);
        }
    }

    void SetLabel()
    {
        if (!label) return;
        label.text = (cost <= 0) ? "FREE" : cost.ToString();
    }

    void SetFill(float t01, bool can)
    {
        float x = Mathf.Clamp01(t01);
        if (fill)
        {
            var s = fill.transform.localScale;
            fill.transform.localScale = new Vector3(s.x, x, s.z);
            fill.color = can ? fillColorReady : fillColorCant;
        }
        if (label)
        {
            label.color = can ? Color.white : new Color(1f, 0.6f, 0.6f, 1f);
        }
    }

    // ----------------
    // AUDIO HELPERS
    // ----------------
    void UpdateHoldSfx(bool can)
    {
        if (!audioSrc || !holdLoopClip) return;
        if (!inside) { StopHoldSfx(); return; }

        if (playOnlyIfAffordable && !can)
        {
            StopHoldSfx();
            return;
        }

        if (!audioSrc.isPlaying)
        {
            audioSrc.clip = holdLoopClip;
            audioSrc.volume = holdLoopVol;
            audioSrc.loop = true;
            audioSrc.Play();
        }
    }

    void StopHoldSfx()
    {
        if (audioSrc && audioSrc.isPlaying)
        {
            audioSrc.Stop();
            audioSrc.clip = null;
        }
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        // avoid having both flags on at the same time in Inspector
        if (isAK && isSMG) isSMG = false;
    }
#endif
}
