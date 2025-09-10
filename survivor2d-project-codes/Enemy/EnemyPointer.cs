using UnityEngine;
using UnityEngine.UI;

public class EnemyPointer : MonoBehaviour
{
    [Header("Basic")]
    public string enemyTag = "Enemy";   // enemy Tag
    public float edgePadding = 48f;     // pixel padding from screen edges
    public float angleOffset = 0f;      // depends on sprite forward (0 for right-facing, 90 for up)

    [Header("Smooth")]
    public float searchInterval = 0.2f; // how often we look for the nearest off-screen enemy
    public float posSmooth = 20f;       // higher → smoother position lerp
    public float rotSpeed = 720f;       // deg/sec rotation speed

    Canvas canvas;
    RectTransform rt, parentRt;
    Camera cam;
    CanvasGroup cg;
    Transform target;
    float searchTimer;

    void Awake()
    {
        cam = Camera.main;
        rt = GetComponent<RectTransform>();
        parentRt = rt ? rt.parent as RectTransform : null;
        canvas = GetComponentInParent<Canvas>();

        cg = GetComponent<CanvasGroup>();
        if (!cg) cg = gameObject.AddComponent<CanvasGroup>();
        SetVisible(false); // keep object enabled, just alpha=0
    }

    void OnEnable()
    {
        searchTimer = 0f;
        target = null;
        SetVisible(false);
    }

    void Update()
    {
        if (!cam || !rt || !parentRt) return;

        // 1) Periodically find the nearest off-screen enemy
        searchTimer -= Time.unscaledDeltaTime;
        if (searchTimer <= 0f)
        {
            searchTimer = Mathf.Max(0.05f, searchInterval);
            target = FindNearestOffscreenEnemy();
        }

        if (!target) { SetVisible(false); return; }

        // 2) Is the target inside the screen? (if yes → hide)
        Vector3 vp3 = cam.WorldToViewportPoint(target.position);
        Vector2 vp = new Vector2(vp3.x, vp3.y);
        bool behind = vp3.z < 0f;

        if (!behind && vp.x >= 0f && vp.x <= 1f && vp.y >= 0f && vp.y <= 1f)
        {
            SetVisible(false);
            return;
        }

        // 3) Direction from center to edge (respect padding)
        Vector2 center = new Vector2(0.5f, 0.5f);
        Vector2 dir = vp - center;
        if (behind) dir = -dir;
        if (dir.sqrMagnitude < 1e-6f) dir = Vector2.up;

        float padXv = edgePadding / parentRt.rect.width;
        float padYv = edgePadding / parentRt.rect.height;
        float bx = Mathf.Max(0.01f, 0.5f - padXv);
        float by = Mathf.Max(0.01f, 0.5f - padYv);
        float scale = Mathf.Max(Mathf.Abs(dir.x) / bx, Mathf.Abs(dir.y) / by);
        Vector2 vpEdge = center + dir / scale;

        // 4) Viewport → local position (depends on Canvas render mode)
        Vector2 screen = new Vector2(vpEdge.x * Screen.width, vpEdge.y * Screen.height);
        Vector2 targetLocal;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            parentRt, screen,
            (canvas && canvas.renderMode == RenderMode.ScreenSpaceOverlay) ? null : cam,
            out targetLocal
        );

        // 5) Smooth position
        float k = 1f - Mathf.Exp(-posSmooth * Time.unscaledDeltaTime);
        rt.localPosition = Vector2.Lerp(rt.localPosition, targetLocal, k);

        // 6) Smooth rotation
        float ang = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg + angleOffset;
        Quaternion want = Quaternion.Euler(0, 0, ang);
        rt.localRotation = Quaternion.RotateTowards(rt.localRotation, want, rotSpeed * Time.unscaledDeltaTime);

        SetVisible(true);
    }

    void SetVisible(bool v)
    {
        if (cg) cg.alpha = v ? 1f : 0f; // alpha only
    }

    Transform FindNearestOffscreenEnemy()
    {
        GameObject[] arr = GameObject.FindGameObjectsWithTag(enemyTag);
        if (arr == null || arr.Length == 0) return null;

        Transform best = null;
        float bestSq = float.PositiveInfinity;
        Vector3 camPos = cam.transform.position;

        for (int i = 0; i < arr.Length; i++)
        {
            Transform t = arr[i].transform;
            Vector3 vp3 = cam.WorldToViewportPoint(t.position);
            bool off = (vp3.z < 0f) || (vp3.x < 0f || vp3.x > 1f || vp3.y < 0f || vp3.y > 1f);
            if (!off) continue;

            float sq = (t.position - camPos).sqrMagnitude;
            if (sq < bestSq) { bestSq = sq; best = t; }
        }
        return best;
    }
}
