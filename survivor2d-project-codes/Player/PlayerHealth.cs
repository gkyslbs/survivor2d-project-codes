using UnityEngine;
using System;

[DisallowMultipleComponent]
public class PlayerHealth : MonoBehaviour
{
    [Header("HP")]
    public int maxHP = 5;                  // starting health
    [SerializeField] int currentHP;

    [Header("Hit Logic")]
    public float invincibleAfterHit = 0.25f;  // short i-frames after getting hit
    float invulTimer;

    // Integrated effects (flash + blink + camera shake + haptics)
    [Header("FX: Hit Flash")]
    public bool enableFlash = true;
    public Color flashColor = new Color(1f, 0.25f, 0.25f, 1f);
    public float flashTime = 0.12f;        // duration of one flash
    public int flashCount = 2;             // how many flashes

    [Header("FX: Invul Blink (blink during i-frames)")]
    public bool enableBlink = true;
    public float blinkInterval = 0.08f;

    [Header("FX: Camera Shake")]
    public bool enableCameraShake = true;
    public float shakeDuration = 0.15f;
    public float shakeAmplitude = 0.35f;
    public float shakeFrequency = 28f;

    [Header("FX: Haptics (mobile vibration)")]
    public bool enableHaptics = true;      // vibrate on hit (mobile)

    // HitStop & Slow-Mo
    [Header("FX: HitStop")]
    public bool enableHitStop = true;                  // tiny pause on hit
    [Range(0f, 0.2f)] public float hitStop = 0.06f;

    [Header("FX: Slow-Mo (optional)")]
    public bool enableSlowMo = false;                  // optional slow-mo after hitstop
    [Range(0.05f, 1f)] public float slowMoScale = 0.35f;
    [Range(0f, 0.5f)] public float slowMoDuration = 0.18f;

    [Header("Pickup (optional, no extra script needed)")]
    public bool autoConsumeHPTaggedPickups = false;  // if true, Tag=HP pickups heal + destroy

    // Death effect (final flash then vanish)
    [Header("Death FX")]
    public bool enableDeathFlash = true;
    public Color deathFlashColor = Color.white;
    public float deathFlashTime = 0.14f;
    public int deathFlashCount = 2;        // how many bursts on final hit
    public float deathDestroyDelay = 0.05f; // tiny delay so the flash is visible

    // SFX (hurt / death)
    [Header("SFX: Voice Grunts")]
    public AudioClip[] hurtGrunts;         // short "ugh/ah" clips
    public AudioClip[] deathGrunts;        // optional death grunt
    [Range(0f, 1f)] public float voiceVolume = 0.9f;
    AudioSource voiceSrc;

    // Event for UI
    public event Action<int, int> OnHealthChanged; // (current, max)

    public bool IsAlive => currentHP > 0;
    public bool IsInvulnerable => invulTimer > 0f;
    public int CurrentHP => currentHP;
    public int MaxHP => maxHP;

    // internal
    Rigidbody2D rb;
    SpriteRenderer[] srs;
    Color[] baseColors;
    Transform cam;                 // Camera.main
    Vector3 lastShakeOffset;       // additive per-frame offset
    Coroutine coBlink, coShake, coFlash;

    // HitStop state
    bool hitStopActive = false;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        if (rb)
        {
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            rb.sleepMode = RigidbodySleepMode2D.NeverSleep;
            rb.useFullKinematicContacts = true;
        }

        srs = GetComponentsInChildren<SpriteRenderer>(true);
        baseColors = new Color[srs.Length];
        for (int i = 0; i < srs.Length; i++) baseColors[i] = srs[i].color;

        var mainCam = Camera.main;
        if (mainCam) cam = mainCam.transform;
        lastShakeOffset = Vector3.zero;

        // prepare 2D AudioSource
        voiceSrc = GetComponent<AudioSource>();
        if (!voiceSrc) voiceSrc = gameObject.AddComponent<AudioSource>();
        voiceSrc.playOnAwake = false;
        voiceSrc.loop = false;
        voiceSrc.spatialBlend = 0f; // 2D
    }

    void OnEnable()
    {
        currentHP = Mathf.Max(1, maxHP);
        invulTimer = 0f;
        OnHealthChanged?.Invoke(currentHP, maxHP);
        StopAllFX();
        SetVisible(true);
        RestoreColors();
        RemoveShakeOffset(); // just in case
    }

    void OnDisable()
    {
        // if disabled during HitStop, normalize time scale
        if (hitStopActive)
        {
            Time.timeScale = 1f;
            hitStopActive = false;
        }
        RemoveShakeOffset();
    }

    void Update()
    {
        if (invulTimer > 0f) invulTimer -= Time.deltaTime;
        if (!IsInvulnerable && coBlink == null) SetVisible(true);
    }

    public void TakeDamage(int dmg)
    {
        if (!IsAlive) return;
        if (invulTimer > 0f) return;

        int amount = Mathf.Max(0, dmg);
        if (amount == 0) return;

        int prev = currentHP;
        currentHP = Mathf.Max(0, currentHP - amount);
        invulTimer = invincibleAfterHit;

        OnHealthChanged?.Invoke(currentHP, maxHP);

        // --- FX (on hit) ---
        if (enableFlash) { if (coFlash != null) StopCoroutine(coFlash); coFlash = StartCoroutine(CoFlash()); }
        if (enableBlink) { if (coBlink != null) StopCoroutine(coBlink); coBlink = StartCoroutine(CoBlink()); }
        if (enableCameraShake) { if (coShake != null) StopCoroutine(coShake); coShake = StartCoroutine(CoShakeAdditive()); }
        if (enableHaptics) VibrateOnce();

        // --- SFX (on hit) ---
        PlayRandom(hurtGrunts, voiceVolume);

        // --- HitStop / SlowMo (only if not dying) ---
        if (currentHP > 0 && enableHitStop)
            StartCoroutine(CoHitStop());

        if (currentHP <= 0)
        {
            Die();
        }
    }

    public void Heal(int amount)
    {
        if (amount <= 0) return;
        int prev = currentHP;
        currentHP = Mathf.Min(maxHP, currentHP + amount);
        if (currentHP != prev) OnHealthChanged?.Invoke(currentHP, maxHP);
    }

    void Die()
    {
        // stop further hits/collisions
        var cols = GetComponentsInChildren<Collider2D>(true);
        for (int i = 0; i < cols.Length; i++) cols[i].enabled = false;

        // disable controls (toggle your own controllers here)
        var shooter = GetComponent<AutoAimShooter>(); if (shooter) shooter.enabled = false;
        var pc = GetComponent<PlayerController>(); if (pc) pc.enabled = false;
        GameManager.I?.GameOver();   // show Game Over panel

        // final flow
        StartCoroutine(CoDeath());
    }

    System.Collections.IEnumerator CoDeath()
    {
        // stop any running blink/flash/shake
        StopAllFX();
        RestoreColors();
        SetVisible(true);

        // death SFX
        PlayRandom(deathGrunts, voiceVolume);

        // final bright flash
        if (enableDeathFlash)
        {
            for (int k = 0; k < Mathf.Max(1, deathFlashCount); k++)
            {
                for (int i = 0; i < srs.Length; i++) if (srs[i]) srs[i].color = deathFlashColor;
                yield return new WaitForSeconds(deathFlashTime * 0.5f);
                RestoreColors();
                yield return new WaitForSeconds(deathFlashTime * 0.5f);
            }
        }

        // hide sprite(s)
        SetVisible(false);

        // tiny delay so the flash is visible
        if (deathDestroyDelay > 0f) yield return new WaitForSeconds(deathDestroyDelay);

        // clean up camera offset and destroy
        RemoveShakeOffset();
        Debug.Log("PLAYER DEAD");
        Destroy(gameObject);
    }

    // === FX IMPLEMENTATION ===
    System.Collections.IEnumerator CoFlash()
    {
        for (int k = 0; k < Mathf.Max(1, flashCount); k++)
        {
            for (int i = 0; i < srs.Length; i++) if (srs[i]) srs[i].color = flashColor;
            yield return new WaitForSeconds(flashTime * 0.5f);
            RestoreColors();
            yield return new WaitForSeconds(flashTime * 0.5f);
        }
        coFlash = null;
    }

    System.Collections.IEnumerator CoBlink()
    {
        while (IsInvulnerable)
        {
            SetVisible(false); yield return new WaitForSeconds(blinkInterval);
            SetVisible(true); yield return new WaitForSeconds(blinkInterval);
        }
        SetVisible(true);
        coBlink = null;
    }

    // Camera shake — additive offset on the player
    System.Collections.IEnumerator CoShakeAdditive()
    {
        if (!cam) { coShake = null; yield break; }
        lastShakeOffset = Vector3.zero;
        float t = 0f;
        while (t < shakeDuration)
        {
            t += Time.deltaTime;
            float f = t / shakeDuration;
            float damp = 1f - f;
            float x = (Mathf.PerlinNoise(Time.time * shakeFrequency, 0f) - 0.5f) * 2f;
            float y = (Mathf.PerlinNoise(0f, Time.time * shakeFrequency) - 0.5f) * 2f;
            Vector3 offset = new Vector3(x, y, 0f) * (shakeAmplitude * damp);

            cam.localPosition += (offset - lastShakeOffset);
            lastShakeOffset = offset;

            yield return null;
        }
        RemoveShakeOffset();
        coShake = null;
    }

    // HitStop / SlowMo coroutine
    System.Collections.IEnumerator CoHitStop()
    {
        if (hitStopActive) yield break;
        hitStopActive = true;

        float prevScale = Time.timeScale;

        // micro pause (realtime wait)
        Time.timeScale = 0f;
        yield return new WaitForSecondsRealtime(hitStop);

        // optional short slow motion
        if (enableSlowMo && slowMoDuration > 0f)
        {
            Time.timeScale = Mathf.Clamp(slowMoScale, 0.05f, 1f);
            yield return new WaitForSecondsRealtime(slowMoDuration);
        }

        Time.timeScale = prevScale;
        hitStopActive = false;
    }

    void RemoveShakeOffset()
    {
        if (cam != null && lastShakeOffset != Vector3.zero)
        {
            cam.localPosition -= lastShakeOffset;
            lastShakeOffset = Vector3.zero;
        }
    }

    void SetVisible(bool v)
    {
        for (int i = 0; i < srs.Length; i++) if (srs[i]) srs[i].enabled = v;
    }

    void RestoreColors()
    {
        for (int i = 0; i < srs.Length; i++) if (srs[i]) srs[i].color = baseColors[i];
    }

    void StopAllFX()
    {
        if (coFlash != null) StopCoroutine(coFlash);
        if (coBlink != null) StopCoroutine(coBlink);
        if (coShake != null) { StopCoroutine(coShake); RemoveShakeOffset(); }
        coFlash = coBlink = coShake = null;
    }

    // Optional: consume HP pickups by Tag (no extra script)
    void OnTriggerEnter2D(Collider2D other)
    {
        if (!autoConsumeHPTaggedPickups) return;
        if (other.CompareTag("HP"))
        {
            Heal(1);
            Destroy(other.gameObject);
        }
    }

    // Haptics
    void VibrateOnce()
    {
#if UNITY_ANDROID || UNITY_IOS
        Handheld.Vibrate();
#endif
    }

    // Audio helpers
    void PlayRandom(AudioClip[] clips, float vol = 1f)
    {
        if (clips == null || clips.Length == 0 || voiceSrc == null) return;
        var clip = clips[UnityEngine.Random.Range(0, clips.Length)];
        if (clip) voiceSrc.PlayOneShot(clip, Mathf.Clamp01(vol));
    }
}
