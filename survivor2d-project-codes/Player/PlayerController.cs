using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [Header("Move")]
    public float moveSpeed = 12f;          // max follow speed
    public float smoothness = 20f;         // 10–30 feels good; higher = stickier follow
    public float stopDistance = 0.01f;     // cuts tiny oscillations

    [Header("Facing (General)")]
    public float rotationOffsetDeg = 0f;   // right-facing sprite = 0; up-facing sprite = -90
    public Transform gfx;                  // child that holds SpriteRenderer (recommended). If null, rotate root.

    [Header("Facing Nearest Enemy")]
    public bool faceNearestEnemy = true;   // always look at nearest enemy
    public float aimDetectRadius = 999f;   // set a limit if you want; 999 ≈ unlimited
    public bool smoothAim = true;          // smooth turning
    public float turnSpeed = 720f;         // deg/sec (used when smoothAim=true)

    // Wider touch support
    [Header("Input")]
    public bool requireTouchOnPlayer = false; // FALSE: start from anywhere (recommended). TRUE: only if touching the character.

    // Mobile tuning (only has effect on mobile builds)
    [Header("Mobile Tuning")]
    public bool applyMobileSlowdown = true;
    [Range(0.4f, 1f)] public float mobileSpeedMultiplier = 0.75f;   // ~25% slower
    [Range(0.5f, 1f)] public float mobileTurnMultiplier = 0.85f;   // slightly calmer turning

    Rigidbody2D rb;
    Collider2D col;

    bool dragging = false;
    Vector2 dragOffset;    // finger point minus player center
    Vector2 targetPos;     // where we want to go (updated in Update)
    Vector2 desiredPos;    // smoothed target (advanced in FixedUpdate)

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<Collider2D>();
        rb.gravityScale = 0f;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate; // important for smoothness
        targetPos = rb.position;
        desiredPos = rb.position;

        // if gfx is empty, auto-pick a SpriteRenderer child
        if (gfx == null)
        {
            var sr = GetComponentInChildren<SpriteRenderer>();
            if (sr) gfx = sr.transform;
        }

        // auto-slowdown on mobile
#if UNITY_ANDROID || UNITY_IOS
        if (applyMobileSlowdown)
        {
            moveSpeed *= mobileSpeedMultiplier;
            turnSpeed *= mobileTurnMultiplier;
        }
#endif
    }

    void Update()
    {
        // --- EDITOR / PC: mouse test ---
#if UNITY_EDITOR || UNITY_STANDALONE
        if (Input.GetMouseButtonDown(0))
        {
            Vector2 p = Camera.main.ScreenToWorldPoint(Input.mousePosition);

            if (requireTouchOnPlayer)
            {
                if (col == null || col.OverlapPoint(p))
                {
                    dragging = true;
                    dragOffset = rb.position - p; // keep an offset
                }
            }
            else
            {
                // start from anywhere on the screen
                dragging = true;
                dragOffset = rb.position - p;
            }
        }
        if (dragging && Input.GetMouseButton(0))
        {
            Vector2 p = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            targetPos = p + dragOffset;     // only update in Update()
        }
        if (Input.GetMouseButtonUp(0))
        {
            dragging = false;
        }
#endif

        // --- MOBILE: touch ---
        if (Input.touchCount > 0)
        {
            Touch t = Input.GetTouch(0);
            Vector2 p = Camera.main.ScreenToWorldPoint(t.position);

            if (t.phase == TouchPhase.Began)
            {
                if (requireTouchOnPlayer)
                {
                    if (col == null || col.OverlapPoint(p))
                    {
                        dragging = true;
                        dragOffset = rb.position - p;
                    }
                }
                else
                {
                    // start from anywhere on the screen
                    dragging = true;
                    dragOffset = rb.position - p;
                }
            }
            else if ((t.phase == TouchPhase.Moved || t.phase == TouchPhase.Stationary) && dragging)
            {
                targetPos = p + dragOffset;
            }
            else if (t.phase == TouchPhase.Ended || t.phase == TouchPhase.Canceled)
            {
                dragging = false;
            }
        }

        // --- always face nearest enemy (independent of movement) ---
        if (faceNearestEnemy)
        {
            FaceToNearestEnemy();
        }
    }

    void FixedUpdate()
    {
        // Exponential smoothing towards target
        float t = 1f - Mathf.Exp(-smoothness * Time.fixedDeltaTime);
        desiredPos = Vector2.Lerp(desiredPos, targetPos, t);

        Vector2 pos = rb.position;
        Vector2 to = desiredPos - pos;
        float dist = to.magnitude;

        if (dragging && dist > stopDistance)
        {
            // speed-limited follow (avoids big jumps)
            Vector2 step = Vector2.ClampMagnitude(to, moveSpeed * Time.fixedDeltaTime);
            rb.MovePosition(pos + step);

            // --- face movement direction ---
            // If "always face enemy" is OFF, look where we move.
            if (!faceNearestEnemy && step.sqrMagnitude > 0.000001f)
            {
                float angle = Mathf.Atan2(step.y, step.x) * Mathf.Rad2Deg + rotationOffsetDeg;
                ApplyRotation(angle);
            }
        }
        else
        {
#if UNITY_2022_2_OR_NEWER
            rb.linearVelocity = Vector2.zero; // new API
#else
            rb.velocity = Vector2.zero;
#endif
        }
    }

    // === Helpers ===

    void FaceToNearestEnemy()
    {
        Transform enemy = FindNearestEnemy();
        if (enemy == null) return;

        Vector3 origin = (gfx ? gfx.position : transform.position);
        Vector2 dir = (Vector2)(enemy.position - origin);

        if (aimDetectRadius < 998f) // using ~999 as "unlimited" sentinel
        {
            if (dir.sqrMagnitude > aimDetectRadius * aimDetectRadius) return;
        }

        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg + rotationOffsetDeg;

        if (!smoothAim)
        {
            ApplyRotation(angle);
        }
        else
        {
            var targetRot = Quaternion.AngleAxis(angle, Vector3.forward);
            if (gfx)
                gfx.rotation = Quaternion.RotateTowards(gfx.rotation, targetRot, turnSpeed * Time.deltaTime);
            else
                transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRot, turnSpeed * Time.deltaTime);
        }
    }

    void ApplyRotation(float angleDeg)
    {
        if (gfx)
            gfx.rotation = Quaternion.AngleAxis(angleDeg, Vector3.forward);
        else
            transform.rotation = Quaternion.AngleAxis(angleDeg, Vector3.forward);
    }

    Transform FindNearestEnemy()
    {
        var enemies = GameObject.FindGameObjectsWithTag("Enemy");
        if (enemies == null || enemies.Length == 0) return null;

        Transform best = null;
        float bestSq = Mathf.Infinity;
        Vector3 p = transform.position;

        for (int i = 0; i < enemies.Length; i++)
        {
            float sq = (enemies[i].transform.position - p).sqrMagnitude;
            if (sq < bestSq) { bestSq = sq; best = enemies[i].transform; }
        }
        return best;
    }
}
