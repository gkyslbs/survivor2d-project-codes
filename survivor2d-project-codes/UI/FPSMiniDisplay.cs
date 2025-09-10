using UnityEngine;
using TMPro;

public class FPSMiniDisplay : MonoBehaviour
{
    public TMP_Text label;
    [Range(0.01f, 1f)] public float smooth = 0.15f;
    float _avgDt;

    void Update()
    {
        float dt = Time.unscaledDeltaTime;
        _avgDt = Mathf.Lerp(_avgDt <= 0 ? dt : _avgDt, dt, smooth);
        float fps = 1f / Mathf.Max(0.00001f, _avgDt);
        if (label) label.text = $"{fps:0.#} FPS  ({_avgDt * 1000f:0.#} ms)";
    }
}
