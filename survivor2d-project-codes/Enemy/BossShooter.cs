using UnityEngine;

public class BossShooter : MonoBehaviour
{
    public Transform muzzle;
    public GameObject bulletPrefab;
    public float fireInterval = 1.2f;
    public float bulletSpeed = 6f;

    [Header("Spawn")]
    public float spawnOffset = 0.35f; // spawn slightly in front of the muzzle

    Transform player;
    float t;
    Collider2D[] ownerCols;

    void Start()
    {
        var p = GameObject.FindGameObjectWithTag("Player");
        if (p) player = p.transform;

        // safe fallback
        if (!muzzle) muzzle = transform;

        // cache boss colliders so we can ignore bullet collisions with the owner
        ownerCols = GetComponentsInChildren<Collider2D>();
    }

    void Update()
    {
        if (!player || !bulletPrefab) return;

        t += Time.deltaTime;
        if (t < fireInterval) return;
        t = 0f;

        // compute direction and spawn point
        Vector3 basePos = muzzle ? muzzle.position : transform.position;
        Vector2 dir = ((Vector2)player.position - (Vector2)basePos).normalized;
        Vector3 spawn = basePos + (Vector3)(dir * spawnOffset); // prevents colliding with self

        // instantiate bullet
        var go = Instantiate(bulletPrefab, spawn, Quaternion.identity);
        go.transform.position = new Vector3(go.transform.position.x, go.transform.position.y, 0f);

        // avoid owner ↔ bullet collisions
        var bulletCols = go.GetComponentsInChildren<Collider2D>();
        if (ownerCols != null && bulletCols != null)
        {
            foreach (var a in ownerCols)
                foreach (var b in bulletCols)
                    if (a && b) Physics2D.IgnoreCollision(a, b, true);
        }

        // sorting (so projectiles render above characters/ground)
        var sr = go.GetComponentInChildren<SpriteRenderer>();
        if (sr)
        {
            // Add "Projectiles" sorting layer in Project Settings if needed
            sr.sortingLayerName = "Projectiles";
            sr.sortingOrder = 100;
        }

        // give it movement (prefer BossBullet if present)
        var bb = go.GetComponent<BossBullet>();
        if (bb)
        {
            // 2-arg call to match the Init signature here
            bb.Init(dir, bulletSpeed);
        }
        else
        {
            var rb = go.GetComponent<Rigidbody2D>();
            if (rb)
            {
                rb.gravityScale = 0f;
                rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
#if UNITY_2022_2_OR_NEWER
                rb.linearVelocity = dir * bulletSpeed;
#else
                rb.velocity = dir * bulletSpeed;
#endif
            }
            // orient sprite toward movement (right axis by default)
            go.transform.right = dir;
        }
    }
}
