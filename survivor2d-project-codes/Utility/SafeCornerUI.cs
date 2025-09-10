using UnityEngine;
using UnityEngine.UI;

[ExecuteAlways]
[DisallowMultipleComponent]
[RequireComponent(typeof(RectTransform))]
public class SafeCornerUI : MonoBehaviour
{
    public enum Corner { TopLeft, TopRight, BottomLeft, BottomRight }

    [Header("Pin to Safe Area Corner")]
    public Corner corner = Corner.TopLeft;

    [Tooltip("Padding from the safe corner in pixels.\nTop corners: (x → right, y → down). Bottom corners: (x → right, y → up).")]
    public Vector2 padding = new Vector2(12f, 12f);

    [Header("Stack (Optional)")]
    [Tooltip("Auto-place this UI UNDER the given RectTransform.\nExample: HPBar.stackBelow = CoinBar, stackSpacingPx = 8")]
    public RectTransform stackBelow;
    [Tooltip("Pixel spacing between stacked elements.")]
    public float stackSpacingPx = 8f;

    [Header("Behaviour")]
    [Tooltip("Re-apply every frame (handles resolution/safe area changes).")]
    public bool alwaysApply = true;

    [Tooltip("Lock anchor/pivot to the chosen corner (turn off if you want manual control).")]
    public bool lockAnchorAndPivot = true;

    Canvas canvas;
    RectTransform rt;
    RectTransform parentRT;
    Camera cam;

    Rect lastSafe; Vector2 lastRes; Corner lastCorner; Vector2 lastPad; bool lastOverlay;
    RectTransform lastStack; float lastStackSpace;

    void OnEnable() { Cache(); Apply(true); }
    void OnRectTransformDimensionsChange() { if (!alwaysApply) Apply(); }
#if UNITY_EDITOR
    void Update() { if (alwaysApply) Apply(); }
#else
    void LateUpdate() { if (alwaysApply) Apply(); }
#endif

    void Cache()
    {
        if (!rt) rt = GetComponent<RectTransform>();
        if (!canvas) canvas = GetComponentInParent<Canvas>();
        if (!parentRT && rt) parentRT = rt.parent as RectTransform;
        if (!cam && canvas && canvas.renderMode != RenderMode.ScreenSpaceOverlay)
            cam = canvas.worldCamera ? canvas.worldCamera : Camera.main;
    }

    void Apply(bool force = false)
    {
        Cache();
        if (!rt || !canvas || !parentRT) return;

        var safe = Screen.safeArea;
        bool overlay = (canvas.renderMode == RenderMode.ScreenSpaceOverlay);

        if (!force &&
            safe == lastSafe &&
            lastRes.x == Screen.width && lastRes.y == Screen.height &&
            lastCorner == corner && lastPad == padding &&
            lastOverlay == overlay &&
            lastStack == stackBelow && Mathf.Approximately(lastStackSpace, stackSpacingPx))
        {
            return;
        }

        lastSafe = safe; lastRes = new Vector2(Screen.width, Screen.height);
        lastCorner = corner; lastPad = padding; lastOverlay = overlay;
        lastStack = stackBelow; lastStackSpace = stackSpacingPx;

        // Safe margins (px)
        float left = safe.xMin;
        float rightM = Screen.width - safe.xMax;
        float topM = Screen.height - safe.yMax;
        float bot = safe.yMin;

        // Base screen position (px)
        Vector2 sp;
        switch (corner)
        {
            default:
            case Corner.TopLeft:
                sp = new Vector2(left + padding.x, Screen.height - topM - padding.y);
                if (lockAnchorAndPivot) { rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f); rt.pivot = new Vector2(0f, 1f); }
                break;

            case Corner.TopRight:
                sp = new Vector2(Screen.width - rightM - padding.x, Screen.height - topM - padding.y);
                if (lockAnchorAndPivot) { rt.anchorMin = rt.anchorMax = new Vector2(1f, 1f); rt.pivot = new Vector2(1f, 1f); }
                break;

            case Corner.BottomLeft:
                sp = new Vector2(left + padding.x, bot + padding.y);
                if (lockAnchorAndPivot) { rt.anchorMin = rt.anchorMax = new Vector2(0f, 0f); rt.pivot = new Vector2(0f, 0f); }
                break;

            case Corner.BottomRight:
                sp = new Vector2(Screen.width - rightM - padding.x, bot + padding.y);
                if (lockAnchorAndPivot) { rt.anchorMin = rt.anchorMax = new Vector2(1f, 0f); rt.pivot = new Vector2(1f, 0f); }
                break;
        }

        // ---- Auto Stack (px) ----
        if (stackBelow)
        {
            // reference element's pixel height
            float scale = canvas ? canvas.scaleFactor : 1f;
            float hPx = stackBelow.rect.height * scale;

            switch (corner)
            {
                case Corner.TopLeft:
                case Corner.TopRight:
                    sp.y -= (hPx + stackSpacingPx);
                    break;
                case Corner.BottomLeft:
                case Corner.BottomRight:
                    sp.y += (hPx + stackSpacingPx);
                    break;
            }
        }

        // Place
        if (overlay)
        {
            rt.position = sp;
        }
        else
        {
            Vector2 localPoint;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(parentRT, sp, cam, out localPoint);
            rt.localPosition = localPoint;
        }

        rt.localScale = Vector3.one; // scale stays 1; size is up to your layout
    }
}
