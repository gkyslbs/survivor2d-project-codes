using UnityEngine;
using UnityEngine.UI;

public class LowHPVignette : MonoBehaviour
{
    [Header("Refs")]
    public PlayerHealth player;    // if left empty, will try to find one
    public Image vignette;         // the Image this script sits on

    [Header("Baseline (Low HP)")]
    [Range(0f, 1f)] public float showBelowNormalized = 0.35f; // start showing under 35% HP
    public float maxAlpha = 0.35f;   // max alpha for low HP
    public int criticalHP = 1;       // strong/red when stuck at 1 HP
    public float criticalAlpha = 0.55f;
    public float baselineFadeSpeed = 3f; // smooth approach speed towards baseline

    [Header("Hit Flash")]
    public float hitFlashAlpha = 0.6f;   // spike alpha when taking a hit
    public float hitFlashDecay = 4f;     // how fast the flash fades (unscaled)

    int lastHP = -1;
    float baselineA;   // target alpha from HP
    float curBaseline; // applied baseline alpha
    float flashA;      // transient alpha on hit

    void Awake()
    {
        if (!vignette) vignette = GetComponent<Image>();
        if (!player)
        {
#if UNITY_2023_1_OR_NEWER
            player = Object.FindFirstObjectByType<PlayerHealth>();
#else
            player = Object.FindObjectOfType<PlayerHealth>();
#endif
        }
        // safe start before gameplay
        SetAlpha(0f);
        if (vignette) vignette.raycastTarget = false;
    }

    void OnEnable()
    {
        if (player != null)
        {
            player.OnHealthChanged += OnHP;
            OnHP(player.CurrentHP, player.MaxHP);
        }
        else
        {
            SetAlpha(0f);
        }
    }

    void OnDisable()
    {
        if (player != null) player.OnHealthChanged -= OnHP;
    }

    void OnHP(int current, int max)
    {
        float n = (max > 0) ? current / (float)max : 0f;

        if (current <= Mathf.Max(1, criticalHP))
        {
            baselineA = criticalAlpha; // when at 1 HP, keep it strong
        }
        else if (n <= showBelowNormalized)
        {
            float t = 1f - (n / Mathf.Max(0.0001f, showBelowNormalized));
            baselineA = Mathf.Lerp(0f, maxAlpha, t);
        }
        else
        {
            baselineA = 0f;
        }

        // on HP drop, trigger a quick flash
        if (lastHP >= 0 && current < lastHP)
            flashA = hitFlashAlpha;

        lastHP = current;
    }

    void Update()
    {
        flashA = Mathf.MoveTowards(flashA, 0f, hitFlashDecay * Time.unscaledDeltaTime);
        curBaseline = Mathf.MoveTowards(curBaseline, baselineA, baselineFadeSpeed * Time.unscaledDeltaTime);
        float finalA = Mathf.Max(curBaseline, flashA);
        SetAlpha(finalA);
    }

    void SetAlpha(float a)
    {
        if (!vignette) return;
        // if almost invisible, disable the Image (also keeps editor preview clean)
        bool visible = a > 0.001f;
        if (vignette.enabled != visible) vignette.enabled = visible;

        var c = vignette.color;
        c.a = a;
        vignette.color = c;
    }

#if UNITY_EDITOR
    // reset editor preview when values change
    void OnValidate()
    {
        if (!vignette) vignette = GetComponent<Image>();
        if (vignette)
        {
            vignette.raycastTarget = false;
            var c = vignette.color; c.a = 0f; vignette.color = c;
            vignette.enabled = false;
        }
    }
#endif
}
