using UnityEngine;
using TMPro;
using System.Collections;

[DisallowMultipleComponent]
public class CoinCounterUI : MonoBehaviour
{
    [Header("Optional (can be left empty)")]
    public TextMeshProUGUI coinText;       // auto-finds if left empty

    [Header("Visuals")]
    public float animateDuration = 0.3f;   // 0 => instant
    public bool useThousandsSeparator = true;

    int displayed;
    Coroutine anim;
    bool subscribed;

    void OnEnable()
    {
        // auto-locate text (if Inspector left empty)
        if (!coinText)
            coinText = GetComponentInChildren<TextMeshProUGUI>(true);

        // wait for GameManager to be ready, then bind
        StartCoroutine(BindWhenReady());
    }

    void OnDisable()
    {
        if (subscribed && GameManager.I != null)
            GameManager.I.OnCoinsChanged -= OnCoinsChanged;
        subscribed = false;
        if (anim != null) { StopCoroutine(anim); anim = null; }
    }

    IEnumerator BindWhenReady()
    {
        // if GameManager.I is null, wait a few frames (safe for scene init order)
        while (GameManager.I == null) yield return null;

        // set initial value
        displayed = GameManager.I.Coins;
        SetText(displayed);

        // subscribe to the event
        if (!subscribed)
        {
            GameManager.I.OnCoinsChanged += OnCoinsChanged;
            subscribed = true;
        }
    }

    void OnCoinsChanged(int newValue)
    {
        if (animateDuration <= 0f)
        {
            displayed = newValue;
            SetText(displayed);
            return;
        }

        if (anim != null) StopCoroutine(anim);
        anim = StartCoroutine(AnimateTo(newValue));
    }

    IEnumerator AnimateTo(int target)
    {
        int start = displayed;
        float t = 0f;
        while (t < 1f)
        {
            t += Time.unscaledDeltaTime / animateDuration; // runs even while paused
            float k = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t));
            displayed = Mathf.RoundToInt(Mathf.Lerp(start, target, k));
            SetText(displayed);
            yield return null;
        }
        displayed = target;
        SetText(displayed);
        anim = null;
    }

    void SetText(int v)
    {
        if (!coinText) return;
        coinText.text = useThousandsSeparator ? v.ToString("N0") : v.ToString();
    }
}
