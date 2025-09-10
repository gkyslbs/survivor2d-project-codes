using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SettingsController : MonoBehaviour
{
    [Header("UI Refs")]
    public Slider volumeSlider;      // SldVolume
    public TMP_Text volumePercent;   // TxtVolPct
    public Toggle vibrateToggle;     // TglVibrate
    public AudioSource previewClick; // (optional) short click sound

    [Header("Debug")]
    public bool debugLogs = false;

    // PlayerPrefs keys
    const string KEY_VOLUME = "cfg_volume";
    const string KEY_VIBRATE = "cfg_vibrate";

    void Awake()
    {
        // Auto-bind: even if forgotten in Inspector, hook the slider event
        if (volumeSlider != null)
        {
            volumeSlider.onValueChanged.RemoveListener(OnVolumeChanged);
            volumeSlider.onValueChanged.AddListener(OnVolumeChanged);
        }

        // Prefs -> UI
        float v = PlayerPrefs.GetFloat(KEY_VOLUME, 0.8f);
        bool vib = PlayerPrefs.GetInt(KEY_VIBRATE, 1) == 1;

        if (volumeSlider) volumeSlider.SetValueWithoutNotify(Mathf.Clamp01(v));
        if (vibrateToggle) vibrateToggle.SetIsOnWithoutNotify(vib);

        ApplyVolume(v);
        UpdateVolumeText(v);

        if (debugLogs) Debug.Log($"[Settings] Awake: v={v:0.00}, vib={vib}");
    }

    void OnEnable()
    {
        // sync values every time the panel opens
        float v = PlayerPrefs.GetFloat(KEY_VOLUME, 0.8f);
        bool vib = PlayerPrefs.GetInt(KEY_VIBRATE, 1) == 1;

        if (volumeSlider) volumeSlider.SetValueWithoutNotify(Mathf.Clamp01(v));
        if (vibrateToggle) vibrateToggle.SetIsOnWithoutNotify(vib);

        ApplyVolume(v);
        UpdateVolumeText(v);

        if (debugLogs) Debug.Log($"[Settings] OnEnable: v={v:0.00}, vib={vib}");
    }

    // ==== UI Events ====

    public void OnVolumeChanged(float v)
    {
        v = Mathf.Clamp01(v);

        ApplyVolume(v);
        UpdateVolumeText(v);

        PlayerPrefs.SetFloat(KEY_VOLUME, v);
        PlayerPrefs.Save();

        if (debugLogs) Debug.Log($"[Settings] OnVolumeChanged -> {v:0.00} ({Mathf.RoundToInt(v * 100f)}%)");

        if (previewClick && previewClick.gameObject.activeInHierarchy && !previewClick.isPlaying)
            previewClick.Play();
    }

    public void OnVibrateChanged(bool on)
    {
        PlayerPrefs.SetInt(KEY_VIBRATE, on ? 1 : 0);
        PlayerPrefs.Save();

        if (debugLogs) Debug.Log($"[Settings] OnVibrateChanged -> {(on ? "ON" : "OFF")}");

        if (on) Haptics.Try();
    }

    // ==== Helpers ====

    void ApplyVolume(float v)
    {
        AudioListener.volume = Mathf.Clamp01(v); // simple global volume
    }

    void UpdateVolumeText(float v)
    {
        if (volumePercent)
            volumePercent.text = Mathf.RoundToInt(v * 100f) + "%";
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void BootApplySavedVolume()
    {
        float v = PlayerPrefs.GetFloat(KEY_VOLUME, 0.8f);
        AudioListener.volume = Mathf.Clamp01(v);
    }

    public static bool VibrationEnabled => PlayerPrefs.GetInt(KEY_VIBRATE, 1) == 1;
}

public static class Haptics
{
    public static void Try()
    {
        if (!SettingsController.VibrationEnabled) return;
#if UNITY_ANDROID || UNITY_IOS
        Handheld.Vibrate();
#endif
    }
}
