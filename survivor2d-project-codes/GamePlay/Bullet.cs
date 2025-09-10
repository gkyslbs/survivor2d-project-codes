using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Collider2D))]
public class Bullet : MonoBehaviour
{
    [Header("Stats")]
    public float lifeTime = 2f;
    public int damage = 1;
    public float hitDistance = 0.6f;   // guaranteed hit when close enough to target

    Rigidbody2D rb;
    Collider2D col;
    Transform target;
    Vector2 velocity;
    bool moveByTransform;
    bool dead;
    float lifeLeft;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<Collider2D>();
        if (col) col.isTrigger = true;

        if (rb)
        {
            rb.gravityScale = 0f;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        }
    }

    void OnEnable()
    {
        dead = false;
        lifeLeft = lifeTime;
        if (rb)
        {
#if UNITY_2022_2_OR_NEWER
            rb.linearVelocity = Vector2.zero;
#else
            rb.velocity = Vector2.zero;
#endif
            rb.angularVelocity = 0f;
        }
    }

    public void SetTarget(Transform t) => target = t;
    public void SetLifeTime(float t) { lifeTime = t; lifeLeft = t; }

    // called by the shooter
    public void Init(Vector2 dir, float speed)
    {
        velocity = dir.normalized * Mathf.Max(0.01f, speed);

        if (rb != null && rb.bodyType == RigidbodyType2D.Dynamic)
        {
#if UNITY_2022_2_OR_NEWER
            rb.linearVelocity = velocity;
#else
            rb.velocity = velocity;
#endif
            moveByTransform = false;
        }
        else
        {
            moveByTransform = true;
        }

        if (velocity.sqrMagnitude > 0.0001f)
        {
            float a = Mathf.Atan2(velocity.y, velocity.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.AngleAxis(a, Vector3.forward);
        }

        lifeLeft = lifeTime;
        dead = false;
    }

    void Update()
    {
        if (dead) return;

        // lifetime
        lifeLeft -= Time.deltaTime;
        if (lifeLeft <= 0f) { Despawn(); return; }

        // movement
        if (moveByTransform)
            transform.position += (Vector3)(velocity * Time.deltaTime);

        // soft target check: if close enough, count as a hit
        if (target)
        {
            float hitSq = hitDistance * hitDistance;
            if ((target.position - transform.position).sqrMagnitude <= hitSq)
            {
                var e = target.GetComponentInParent<Enemy>();
                if (e) { HitEnemy(e); return; }
            }
        }

        // cheap proactive scan
        float dist = velocity.magnitude * Time.deltaTime + 0.02f;
        if (dist > 0f)
        {
            float radius = 0.08f;
            if (col is CircleCollider2D cc)
                radius = cc.radius * Mathf.Max(transform.lossyScale.x, transform.lossyScale.y);
            else if (col)
                radius = Mathf.Max(0.05f, Mathf.Min(col.bounds.extents.x, col.bounds.extents.y));

            var hit = Physics2D.CircleCast(transform.position, radius, velocity.normalized, dist);
            if (hit.collider)
            {
                var e = hit.collider.GetComponentInParent<Enemy>();
                if (e) { HitEnemy(e); return; }
            }
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (dead) return;
        var e = other.GetComponentInParent<Enemy>();
        if (e) HitEnemy(e);
    }

    void HitEnemy(Enemy e)
    {
        if (dead) return;
        dead = true;
        e.TakeDamage(damage);
        Despawn();
    }

    void Despawn()
    {
        // using existing pool:
        // Pool.Despawn(GameObject) is expected
        Pool.Despawn(gameObject);
    }
}
