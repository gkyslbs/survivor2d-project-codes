using UnityEngine;

[RequireComponent(typeof(Camera))]
public class CameraFollowClamp : MonoBehaviour
{
    [Header("Mode")]
    public bool followOnly = true; // NEW: no bounds, just follow

    [Header("Follow")]
    public Transform target;                    // Player
    public Vector3 offset = new Vector3(0, 0, -10);
    [Range(0f, 0.5f)] public float smoothTime = 0.15f;
    public float maxSpeed = 100f;

    public enum BoundsMode { FromSpriteRenderer, FromCollider2D, Manual }

    [Header("Bounds (optional; used if followOnly=false)")]
    public BoundsMode boundsMode = BoundsMode.FromCollider2D;
    public SpriteRenderer boundsSprite;   // big background SpriteRenderer
    public Collider2D boundsCollider;     // Box/Polygon/CompositeCollider2D works
    public Vector2 manualMin = new Vector2(-20, -20);
    public Vector2 manualMax = new Vector2(20, 20);
    public float padding = 0.25f;

    private Camera cam;
    private Vector3 vel;

    void Awake()
    {
        cam = GetComponent<Camera>();
        if (cam == null) cam = Camera.main;
    }

    void LateUpdate()
    {
        if (!target || !cam) return;

        // desired follow position
        Vector3 desired = target.position + offset;
        desired.z = offset.z; // keep camera z fixed

        // no-bounds mode: just follow
        if (followOnly)
        {
            transform.position = Vector3.SmoothDamp(transform.position, desired, ref vel, smoothTime, maxSpeed);
            return;
        }

        // Below: clamp to bounds (followOnly=false)
        Bounds wb = GetWorldBounds();

        // invalid bounds → don't clamp, just follow
        if (wb.size.x < 0.01f || wb.size.y < 0.01f)
        {
            transform.position = Vector3.SmoothDamp(transform.position, desired, ref vel, smoothTime, maxSpeed);
            return;
        }

        if (padding > 0f) wb.Expand(new Vector3(-2f * padding, -2f * padding, 0f));

        float halfH = cam.orthographicSize;
        float halfW = halfH * cam.aspect;

        // if map is smaller than the view, center it
        if (wb.size.x < halfW * 2f) desired.x = wb.center.x;
        else desired.x = Mathf.Clamp(desired.x, wb.min.x + halfW, wb.max.x - halfW);

        if (wb.size.y < halfH * 2f) desired.y = wb.center.y;
        else desired.y = Mathf.Clamp(desired.y, wb.min.y + halfH, wb.max.y - halfH);

        transform.position = Vector3.SmoothDamp(transform.position, desired, ref vel, smoothTime, maxSpeed);
    }

    Bounds GetWorldBounds()
    {
        switch (boundsMode)
        {
            case BoundsMode.FromSpriteRenderer:
                if (boundsSprite != null) return boundsSprite.bounds;
                break;
            case BoundsMode.FromCollider2D:
                if (boundsCollider != null) return boundsCollider.bounds;
                break;
        }
        // Manual fallback
        Vector3 center = new Vector3((manualMin.x + manualMax.x) * 0.5f, (manualMin.y + manualMax.y) * 0.5f, 0);
        Vector3 size = new Vector3(Mathf.Abs(manualMax.x - manualMin.x), Mathf.Abs(manualMax.y - manualMin.y), 0);
        return new Bounds(center, size);
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        if (followOnly) return; // skip drawing in no-bounds mode
        Gizmos.color = new Color(0.2f, 0.8f, 1f, 0.35f);
        Bounds wb = GetWorldBounds();
        if (wb.size.x > 0.01f && wb.size.y > 0.01f)
            Gizmos.DrawWireCube(wb.center, wb.size);
    }
#endif
}
