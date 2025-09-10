using UnityEngine;

public class AppPerformance : MonoBehaviour
{
    [Header("Frame Rate")]
    [Range(15, 240)] public int targetFPS = 60;
    public bool disableVSync = true;

    [Header("Physics (Fixed Timestep)")]
    [Tooltip("e.g., 120 => 1/120 ≈ 0.00833")]
    [Range(30, 240)] public int fixedHz = 120;

    [Header("Mobile Tweaks")]
    public bool runInBackground = false;
    public bool neverSleep = true; // prevent screen from sleeping

    void Awake()
    {
        if (disableVSync) QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = targetFPS;

        Time.fixedDeltaTime = 1f / Mathf.Clamp(fixedHz, 30, 240);

        Application.runInBackground = runInBackground;
        Screen.sleepTimeout = neverSleep ? SleepTimeout.NeverSleep : SleepTimeout.SystemSetting;
    }

    // Can be called at runtime; e.g., drop to 30 FPS if performance dips
    public void SetTargetFPS(int fps)
    {
        targetFPS = Mathf.Clamp(fps, 15, 240);
        Application.targetFrameRate = targetFPS;
    }
}
