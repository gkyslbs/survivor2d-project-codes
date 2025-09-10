using UnityEngine;

public class Enemy : MonoBehaviour
{
    [Header("Stats")]
    public float moveSpeed = 2.5f;
    public int maxHP = 3;

    [Header("Boss")]
    public bool isBoss = false;   // set true on BossEnemy.prefab
    public float bossCoinMultiplier = 4f; // bosses drop more coins

    [Header("Coin Drop")]
    public GameObject coinPrefab;      // Coin.prefab
    public int coinMin = 1;
    public int coinMax = 2;
    public float scatterSpeed = 2f;

    Transform player;
    int hp;

    // Track PlayerProjectile layer so we only take damage from player bullets
    int L_PlayerProj = -1;

    // Optional: smoother physics-based movement
    Rigidbody2D rb;
    bool useRB = false;

    void Start()
    {
        if (isBoss) KillManager.I?.RegisterBossSpawned();
    }

    void OnEnable() { EnemyRegistry.All.Add(this); }
    void OnDisable() { EnemyRegistry.All.Remove(this); }

    void Awake()
    {
        hp = maxHP;
        var p = GameObject.FindGameObjectWithTag("Player");
        if (p) player = p.transform;

        // Cache player projectile layer (returns -1 if not found → we fallback below)
        L_PlayerProj = LayerMask.NameToLayer("PlayerProjectile");

        // Setup RB if available (for smoother movement/rotation)
        rb = GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.gravityScale = 0f;
            rb.interpolation = RigidbodyInterpolation2D.Interpolate; // looks smoother
            // Kinematic is fine; Dynamic also works if you want pushes/collisions
            useRB = true;
        }
    }

    void Update()
    {
        if (!player) return;

        // If no RB, keep simple transform-based movement
        if (!useRB)
        {
            Vector2 dir = ((Vector2)player.position - (Vector2)transform.position).normalized;
            transform.position += (Vector3)(dir * moveSpeed * Time.deltaTime);

            float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);
        }
        // If RB exists, movement/rotation happens in FixedUpdate
    }

    // Physics movement path (when using RB)
    void FixedUpdate()
    {
        if (!useRB || !player) return;

        Vector2 pos = rb.position;
        Vector2 dir = ((Vector2)player.position - pos).normalized;

        rb.MovePosition(pos + dir * moveSpeed * Time.fixedDeltaTime);

        float ang = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        rb.MoveRotation(ang);
    }

    public void TakeDamage(int dmg)
    {
        hp -= dmg;
        GetComponent<HitFlash2D>()?.Flash();
        if (hp <= 0) Die();
    }

    void Die()
    {
        SpawnCoins();

        // Count the kill for wave/boss logic
        KillManager.I?.RegisterKill();

        // If this was a boss, notify completion tracker
        if (isBoss)
            KillManager.I?.RegisterBossDeath();

        // Play SFX if configured
        SfxManager.Play(SfxKey.EnemyDie);

        Destroy(gameObject);
    }

    void SpawnCoins()
    {
        if (coinPrefab == null) return;

        int count = Mathf.Max(0, Random.Range(coinMin, coinMax + 1));

        if (isBoss)
            count = Mathf.Max(1, Mathf.RoundToInt(count * bossCoinMultiplier));

        for (int i = 0; i < count; i++)
        {
            var go = Instantiate(coinPrefab, transform.position, Quaternion.identity);
            var rb = go.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                Vector2 rnd = Random.insideUnitCircle.normalized * scatterSpeed;
                rb.linearVelocity = rnd;
            }
        }
    }

    // Trigger path (bullet has trigger collider)
    void OnTriggerEnter2D(Collider2D other)
    {
        // If we know the PlayerProjectile layer, ignore everything else
        if (L_PlayerProj != -1 && other.gameObject.layer != L_PlayerProj) return;

        var b = other.GetComponentInParent<Bullet>();
        if (b != null) { TakeDamage(b.damage); Destroy(b.gameObject); }
    }

    // Non-trigger path
    void OnCollisionEnter2D(Collision2D c)
    {
        // If we know the PlayerProjectile layer, ignore everything else
        if (L_PlayerProj != -1 && c.collider.gameObject.layer != L_PlayerProj) return;

        var b = c.collider.GetComponentInParent<Bullet>();
        if (b != null) { TakeDamage(b.damage); Destroy(b.gameObject); }
    }
}
