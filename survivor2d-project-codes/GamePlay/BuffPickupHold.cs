using UnityEngine;
using System.Collections;
using System.Reflection; // to reset private timer field

[RequireComponent(typeof(Collider2D))]
public class BuffPickupHold : MonoBehaviour
{
    [Header("Cost & Hold")]
    public int cost = 0;               // 0 = FREE
    public float holdTime = 0.5f;      // how long player must stay inside

    [Header("What to Equip / Enable")]
    public PlayerEquip playerEquip;           // if null, try from trigger / parent
    public AutoAimShooter autoAimShooter;     // if null, try from trigger / parent
    public bool destroyOnPurchase = true;

    // NEW: should this buff equip SMG? (turn ON on SubmachineBuff)
    [Header("Equip Variant")]
    public bool equipSMGOnPurchase = false;

    // optional shooter override for things like SMG
    [Header("Shooter Override (optional)")]
    public bool overrideShooter = false;      // tick this on SubmachineBuff
    public float newFireInterval = 0.10f;     // faster fire rate (sec)

    // call an existing skin/action method on player (optional)
    [Header("Call Player Method on Equip (optional)")]
    public GameObject messageTarget;          // if empty, use triggering player
    public string sendMessageOnEquip = "";    // e.g. "EquipSMG" (no-arg public method)

    [Header("Misc")]
    public string playerTag = "Player";
    public bool debugLog = true;              // turn off if you don’t want logs

    float hold;
    bool inside;
    GameObject lastPlayerGO;                  // cache who triggered (for SendMessage)

    void Reset()
    {
        var col = GetComponent<Collider2D>();
        if (col) col.isTrigger = true;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag(playerTag)) return;
        inside = true;
        lastPlayerGO = other.transform.root.gameObject; // cache player GO

        // try to grab player components from child/parent too
        if (playerEquip == null)
        {
            playerEquip = other.GetComponent<PlayerEquip>();
            if (playerEquip == null) playerEquip = other.GetComponentInParent<PlayerEquip>();
        }

        if (autoAimShooter == null)
        {
            autoAimShooter = other.GetComponent<AutoAimShooter>();
            if (autoAimShooter == null) autoAimShooter = other.GetComponentInParent<AutoAimShooter>();
        }
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag(playerTag)) return;
        inside = false;
        hold = 0f;
        if (other && lastPlayerGO == other.transform.root.gameObject) lastPlayerGO = null;
    }

    void Update()
    {
        if (!inside) return;

        if (Affordable())
        {
            hold += Time.deltaTime;

            if (hold >= holdTime)
            {
                if (TryPurchase())
                {
                    EquipNow();
                    SfxManager.Play(SfxKey.BuffEquip);
                    if (destroyOnPurchase) Destroy(gameObject);
                }
                else
                {
                    hold = 0f;
                }
            }
        }
        else
        {
            hold = 0f;
        }
    }

    // coin checks via GameManager
    bool Affordable()
    {
        if (cost <= 0) return true; // FREE
        return (GameManager.I != null && GameManager.I.Coins >= cost);
    }

    bool TryPurchase()
    {
        if (cost <= 0) return true;
        return (GameManager.I != null && GameManager.I.TrySpendCoins(cost));
    }

    void EquipNow()
    {
        // which equip? (SMG or regular)
        if (playerEquip != null)
        {
            if (equipSMGOnPurchase)
                playerEquip.EquipWeaponSMG();   // switch to SMG
            else
                playerEquip.EquipWeapon();      // regular pistol flow
        }

        if (autoAimShooter != null) autoAimShooter.enabled = true;

        if (overrideShooter && autoAimShooter != null)
        {
            float iv = Mathf.Max(0.01f, newFireInterval);
            autoAimShooter.fireInterval = iv; // direct set
            if (debugLog) Debug.Log($"[Buff] AutoAimShooter.fireInterval -> {iv}");

            // set private timer so it shoots instantly next frame
            var t = typeof(AutoAimShooter);
            var timerField = t.GetField("timer", BindingFlags.Instance | BindingFlags.NonPublic);
            if (timerField != null && timerField.FieldType == typeof(float))
            {
                timerField.SetValue(autoAimShooter, iv);
                if (debugLog) Debug.Log("[Buff] AutoAimShooter.timer -> set to fireInterval for instant apply");
            }
            TryZeroCooldown(autoAimShooter);
        }

        // optionally call a no-arg method on player (skin/anim hooks etc.)
        if (!string.IsNullOrEmpty(sendMessageOnEquip))
        {
            var target = messageTarget ? messageTarget : (lastPlayerGO ? lastPlayerGO : (autoAimShooter ? autoAimShooter.gameObject : (playerEquip ? playerEquip.gameObject : null)));
            if (target)
            {
                target.SendMessage(sendMessageOnEquip, SendMessageOptions.DontRequireReceiver);
                if (debugLog) Debug.Log($"[Buff] SendMessage -> {target.name}.{sendMessageOnEquip}()");
            }
            else if (debugLog) Debug.LogWarning("[Buff] SendMessage target not found.");
        }
    }

    // try to zero out generic cooldown-like fields
    void TryZeroCooldown(MonoBehaviour shooter)
    {
        var t = shooter.GetType();
        var flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic |
                    BindingFlags.FlattenHierarchy | BindingFlags.IgnoreCase;

        string[] cdNamesF = { "cooldown", "cd", "timeToNextShot", "nextShotTimer" };
        foreach (var n in cdNamesF)
        {
            var f = t.GetField(n, flags);
            if (f != null && f.FieldType == typeof(float))
            {
                f.SetValue(shooter, 0f);
                if (debugLog) Debug.Log($"[Buff] zero {n}(f)");
                return;
            }
        }

        string[] cdNamesP = { "Cooldown", "TimeToNextShot", "NextShotTimer" };
        foreach (var n in cdNamesP)
        {
            var p = t.GetProperty(n, flags);
            if (p != null && p.CanWrite && p.PropertyType == typeof(float))
            {
                p.SetValue(shooter, 0f);
                if (debugLog) Debug.Log($"[Buff] zero {n}(p)");
                return;
            }
        }
    }
}
