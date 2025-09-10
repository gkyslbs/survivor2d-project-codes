using UnityEngine;

[RequireComponent(typeof(Collider2D))]
[RequireComponent(typeof(Rigidbody2D))]
public class BossBullet : MonoBehaviour
{
    public int damage = 1;                 // damage dealt to the player
    public float lifeTime = 5f;

    [Header("Visual")]
    public string sortingLayerName = "Projectiles";
    public int sortingOrder = 100;

    [Header("Collision")]
    public string targetTag = "Player";    // kept for compatibility/use in other setups
    public LayerMask destroyOnLayers;      // destroy if it hits ground etc. (set in Inspector)

    // Which axis the sprite is authored facing
    public enum FacingAxis { Right, Up }
    public FacingAxis spriteAxis = FacingAxis.Right; // set Up if the sprite points upward by default

    // Make the sprite face its velocity
    public bool alignToVelocity = true;

    [Header("Hit Fallback (to avoid missing close contacts)")]
    public bool enableDistanceCheck = true;     // helps catch hits even if matrix/layers are off
    public float distanceHitPadding = 0.05f;    // small overlap allowance

    // Extra tuning for speed (multiplies speed coming from BossShooter)
    [Header("Speed Tuning")]
    public float speedMultiplier = 1f;          // global multiplier (can reduce in Inspector)
    public bool applyMobileSlowdown = true;
    [Range(0.4f, 1f)] public float mobileSpeedMultiplier = 0.7f; // slower on mobile

    Rigidbody2D rb;
    Collider2D bulletCol;
    PlayerHealth playerHP;
    Collider2D playerCol;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.linearDamping = 0f;
        rb.angularDamping = 0f;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        rb.freezeRotation = true;

        bulletCol = GetComponent<Collider2D>();
        bulletCol.isTrigger = true; // using trigger-based collisions

        var sr = GetComponentInChildren<SpriteRenderer>();
        if (sr) { sr.sortingLayerName = sortingLayerName; sr.sortingOrder = sortingOrder; }

        // Z=0 for 2D
        var p = transform.position;
        transform.position = new Vector3(p.x, p.y, 0f);

        // Prepare player references (used by distance fallback)
#if UNITY_2023_1_OR_NEWER
        playerHP = Object.FindFirstObjectByType<PlayerHealth>();
#else
        playerHP = Object.FindObjectOfType<PlayerHealth>();
#endif
        if (playerHP) playerCol = playerHP.GetComponentInChildren<Collider2D>();

        if (lifeTime > 0f) Destroy(gameObject, lifeTime);
    }

    public void Init(Vector2 dir, float speed)
    {
        dir = dir.normalized;

        // Apply multiplier(s) on top of BossShooter's speed
        float s = speed * Mathf.Max(0.01f, speedMultiplier);
#if UNITY_ANDROID || UNITY_IOS
        if (applyMobileSlowdown) s *= mobileSpeedMultiplier;
#endif

#if UNITY_2022_2_OR_NEWER
        rb.linearVelocity = dir * s;
#else
        rb.velocity = dir * s;
#endif
        if (alignToVelocity) ApplyFacing(dir);
    }

    void Update()
    {
#if UNITY_2022_2_OR_NEWER
        Vector2 v = rb.linearVelocity;
#else
        Vector2 v = rb.velocity;
#endif
        if (alignToVelocity && v.sqrMagnitude > 0.0001f)
            ApplyFacing(v.normalized);

        // ---- Fallback: directly check collider distance to catch near-misses ----
        if (enableDistanceCheck && playerCol && bulletCol)
        {
            var d = Physics2D.Distance(bulletCol, playerCol);
            if (d.isOverlapped || d.distance <= distanceHitPadding)
            {
                playerHP?.TakeDamage(damage);
                Destroy(gameObject);
            }
        }
    }

    void ApplyFacing(Vector2 dir)
    {
        if (spriteAxis == FacingAxis.Right) transform.right = dir;
        else transform.up = dir;
    }

    // ---- Regular physics callbacks (when triggers/collisions fire) ----
    void TryHit(GameObject otherGO)
    {
        var hp = otherGO.GetComponentInParent<PlayerHealth>();
        if (hp != null)
        {
            hp.TakeDamage(damage);
            Destroy(gameObject);
            return;
        }

        if (destroyOnLayers.value != 0)
        {
            int ol = otherGO.layer;
            if (((1 << ol) & destroyOnLayers.value) != 0)
                Destroy(gameObject);
        }
    }

    void OnTriggerEnter2D(Collider2D other) => TryHit(other.gameObject);
    void OnCollisionEnter2D(Collision2D c) => TryHit(c.collider.gameObject);
}
