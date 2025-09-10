using UnityEngine;

public class AutoAimShooter : MonoBehaviour
{
    [Header("Refs")]
    public GameObject bulletPrefab;   // put Bullet prefab here
    public Transform firePoint;       // muzzle; if null, uses object center

    [Header("Shoot")]
    public float fireInterval = 0.25f;
    public float bulletSpeed = 18f;
    public float detectRadius = 15f;

    [Header("Targeting Filter")]
    public bool onlyIfOnScreen = true;                  // ignore if target is off-screen
    [Range(0f, 0.25f)] public float screenEdgePadding = 0.05f; // viewport padding near edges

    [Header("Safety")]
    public float bulletMaxLifetime = 3f;   // passed to Bullet if it has one; else Destroy fallback
    public bool debugLogs = false;

    [Header("Audio Throttle")]
    public float shootSfxMinInterval = 0.06f;   // avoid spamming shoot SFX too fast
    static float _lastShootSfxTime = -999f;

    float timer;

    void OnEnable() { timer = fireInterval; } // can shoot immediately on enable

    void Update()
    {
        timer += Time.deltaTime;
        if (timer >= fireInterval)
        {
            Transform t = FindNearestEnemyFiltered();
            if (t != null)
            {
                Shoot(t);   // target Transform
                timer = 0f;
            }
        }
    }

    // screen + radius filter
    Transform FindNearestEnemyFiltered()
    {
        // 1) try EnemyRegistry if available
        var e = EnemyRegistry.GetNearest(transform.position, detectRadius);
        if (IsValidTarget(e ? e.transform : null)) return e.transform;

        // 2) fallback: scan by Tag
        var arr = GameObject.FindGameObjectsWithTag("Enemy");
        if (arr == null || arr.Length == 0) return null;

        Vector3 pos = transform.position;
        float bestSq = Mathf.Infinity;
        float r2 = detectRadius * detectRadius;
        Transform best = null;

        for (int i = 0; i < arr.Length; i++)
        {
            var t = arr[i].transform;
            float sq = (t.position - pos).sqrMagnitude;
            if (sq > r2) continue;
            if (onlyIfOnScreen && !IsOnScreen(t.position)) continue;

            if (sq < bestSq) { bestSq = sq; best = t; }
        }
        return best;
    }

    bool IsValidTarget(Transform t)
    {
        if (t == null) return false;
        if ((t.position - transform.position).sqrMagnitude > detectRadius * detectRadius) return false;
        if (onlyIfOnScreen && !IsOnScreen(t.position)) return false;
        return true;
    }

    bool IsOnScreen(Vector3 worldPos)
    {
        var cam = Camera.main;
        if (!cam) return true; // if no camera, don't block
        Vector3 v = cam.WorldToViewportPoint(worldPos);
        return v.z > 0f &&
               v.x >= -screenEdgePadding && v.x <= 1f + screenEdgePadding &&
               v.y >= -screenEdgePadding && v.y <= 1f + screenEdgePadding;
    }

    // --- version that takes a Transform target ---
    void Shoot(Transform target)
    {
        if (bulletPrefab == null || target == null) return;

        Vector3 spawnPos = firePoint ? firePoint.position : transform.position;
        spawnPos.z = 0f;

        Vector2 dir = ((Vector2)target.position - (Vector2)spawnPos).normalized;

        // pooling: uses shared Pool.cs (no local mini-pool)
        var go = Pool.Spawn(bulletPrefab, spawnPos, Quaternion.identity);

        // throttle shoot SFX a bit
        if (Time.unscaledTime - _lastShootSfxTime >= shootSfxMinInterval)
        {
            SfxManager.PlayAt(SfxKey.Shoot, (firePoint ? firePoint.position : transform.position));
            _lastShootSfxTime = Time.unscaledTime;
        }

        // fix Z just in case
        var tr = go.transform;
        tr.position = new Vector3(tr.position.x, tr.position.y, 0f);

        var b = go.GetComponent<Bullet>();
        if (b != null)
        {
            // order: Init -> SetTarget -> SetLifeTime
            b.Init(dir, bulletSpeed);
            b.SetTarget(target);
            if (bulletMaxLifetime > 0f)
                b.SetLifeTime(bulletMaxLifetime);
        }
        else
        {
            // if no Bullet component, do a simple physics fallback
            var rb = go.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
#if UNITY_2022_2_OR_NEWER
                rb.linearVelocity = dir * bulletSpeed;
#else
                rb.velocity = dir * bulletSpeed;
#endif
            }
            float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            tr.rotation = Quaternion.AngleAxis(angle, Vector3.forward);

            if (bulletMaxLifetime > 0f) Destroy(go, bulletMaxLifetime);
            if (debugLogs) Debug.LogWarning("Bullet prefab has no Bullet.cs; used Destroy fallback.", go);
        }

        if (debugLogs)
            Debug.Log($"Shoot -> target:{target.name} speed:{bulletSpeed} life:{bulletMaxLifetime}s", go);
    }

    // legacy signature kept so old calls won’t break (optional)
    void Shoot(Vector3 targetPos)
    {
        Transform t = FindNearestEnemyFiltered();
        if (t != null) Shoot(t);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectRadius);
    }
}
