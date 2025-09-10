using UnityEngine;
using System.Collections;

[DisallowMultipleComponent]
public class HitFlash2D : MonoBehaviour
{
    [Header("Flash")]
    public Color flashColor = new Color(1f, 0.3f, 0.3f, 1f);
    public float flashTime = 0.10f;   // single on/off cycle duration
    public int flashCount = 1;        // how many times to repeat

    SpriteRenderer[] srs;
    Color[] baseColors;
    Coroutine co;

    void Awake()
    {
        srs = GetComponentsInChildren<SpriteRenderer>(true);
        baseColors = new Color[srs.Length];
        for (int i = 0; i < srs.Length; i++)
            baseColors[i] = srs[i].color;
    }

    void OnEnable()
    {
        RestoreColors();
    }

    void OnDisable()
    {
        RestoreColors();
    }

    public void Flash()
    {
        if (!isActiveAndEnabled) return;
        if (co != null) StopCoroutine(co);
        co = StartCoroutine(CoFlash());
    }

    IEnumerator CoFlash()
    {
        int count = Mathf.Max(1, flashCount);
        for (int k = 0; k < count; k++)
        {
            // paint to flash color
            for (int i = 0; i < srs.Length; i++)
                if (srs[i]) srs[i].color = flashColor;

            yield return new WaitForSeconds(flashTime * 0.5f);

            // revert to base
            RestoreColors();

            yield return new WaitForSeconds(flashTime * 0.5f);
        }
        co = null;
    }

    void RestoreColors()
    {
        for (int i = 0; i < srs.Length; i++)
            if (srs[i]) srs[i].color = baseColors[i];
    }
}
