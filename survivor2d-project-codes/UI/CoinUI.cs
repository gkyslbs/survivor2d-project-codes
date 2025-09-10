using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class CoinUI : MonoBehaviour
{
    public TextMeshProUGUI coinText;   // drag CoinText here
    public Transform bumpTarget;       // CoinBar or CoinText
    public Image barBackground;        // Image component of the CoinBar
    public Color flashColor = new Color(1f, 0.78f, 0.1f, 0.45f); // semi-transparent gold
    public float bumpScale = 1.12f;

    Color baseColor;

    void Awake()
    {
        if (barBackground) baseColor = barBackground.color;
    }

    void OnEnable()
    {
        if (GameManager.I != null)
        {
            GameManager.I.OnCoinsChanged += OnCoinsChanged;
            OnCoinsChanged(GameManager.I.Coins);
        }
    }

    void OnDisable()
    {
        if (GameManager.I != null)
            GameManager.I.OnCoinsChanged -= OnCoinsChanged;
    }

    void OnCoinsChanged(int val)
    {
        if (coinText) coinText.text = val.ToString();
        if (bumpTarget) StartCoroutine(CoBump());
        if (barBackground) StartCoroutine(CoFlash());
    }

    IEnumerator CoBump()
    {
        Vector3 a = Vector3.one, b = Vector3.one * bumpScale;
        float t = 0f;
        while (t < 0.08f) { t += Time.unscaledDeltaTime; bumpTarget.localScale = Vector3.Lerp(a, b, t / 0.08f); yield return null; }
        t = 0f;
        while (t < 0.10f) { t += Time.unscaledDeltaTime; bumpTarget.localScale = Vector3.Lerp(b, a, t / 0.10f); yield return null; }
        bumpTarget.localScale = a;
    }

    IEnumerator CoFlash()
    {
        float t = 0f;
        while (t < 0.08f) { t += Time.unscaledDeltaTime; barBackground.color = Color.Lerp(baseColor, flashColor, t / 0.08f); yield return null; }
        t = 0f;
        while (t < 0.20f) { t += Time.unscaledDeltaTime; barBackground.color = Color.Lerp(flashColor, baseColor, t / 0.20f); yield return null; }
        barBackground.color = baseColor;
    }
}
