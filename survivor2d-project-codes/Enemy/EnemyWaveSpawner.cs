using System.Collections;
using UnityEngine;

public class EnemyWaveSpawner : MonoBehaviour
{
    [Header("Prefab")]
    public GameObject enemyPrefab;   // small zombie prefab

    [Header("Boss")]
    public GameObject bossPrefab;           // boss prefab
    public int bossEveryMinionKills = 50;   // Level1 = 50, Level2 = 30
    public bool ignoreAliveCapForBoss = true;

    [Header("Start")]
    public float initialSilence = 0f;   // Level1 uses 3f (no spawns for the first 3s)

    [Header("Waves")]
    public float timeBetweenWaves = 3f;
    public int startCount = 4;
    public int addPerWave = 2;
    public float spawnIntervalInWave = 0.2f;
    public int maxAliveCap = 60;

    [Header("Spawn From Screen Edges")]
    public bool spawnFromLeft = true;
    public bool spawnFromRight = true;
    public bool spawnFromTop = true;
    public bool spawnFromBottom = true;
    public float sideMargin = 2f;
    public float topBottomPadding = 0.5f;

    [Header("Center Fallback")]
    public Transform player;
    public float circleRadius = 12f;

    [Header("Debug")]
    public bool logWaves = false;
    public Color gizmoColor = new Color(1f, 0.3f, 0.2f, 0.35f);

    Camera cam;
    int waveIndex;
    int alive;

    // --- Boss counters ---
    int _minionKillsSinceBoss = 0;

    // avoid spawning while scene is closing/disabled
    bool _running = false;

    void Start()
    {
        cam = Camera.main;
        if (cam == null)
        {
#if UNITY_2023_1_OR_NEWER
            var anyCam = UnityEngine.Object.FindFirstObjectByType<Camera>();
#else
            var anyCam = UnityEngine.Object.FindObjectOfType<Camera>();
#endif
            if (anyCam != null) cam = anyCam;
        }

        if (enemyPrefab == null)
        {
            Debug.LogError("[Spawner] enemyPrefab is not assigned! Set it on EnemyWaveSpawner.");
            enabled = false;
            return;
        }

        _running = true;
        StartCoroutine(SpawnLoop());
    }

    void OnDisable()
    {
        _running = false;
        StopAllCoroutines();
    }

    IEnumerator SpawnLoop()
    {
        // --- initial silence (no spawns) ---
        if (initialSilence > 0f)
            yield return new WaitForSeconds(initialSilence);

        while (true)
        {
            // if alive cap is full, wait
            while (alive >= maxAliveCap) yield return new WaitForSeconds(0.25f);

            int enemiesThisWave = startCount + waveIndex * addPerWave;
            if (logWaves) Debug.Log($"[Spawner] Wave {waveIndex + 1} -> {enemiesThisWave} enemy");

            yield return StartCoroutine(SpawnWave(enemiesThisWave));
            waveIndex++;

            yield return new WaitForSeconds(timeBetweenWaves);
        }
    }

    IEnumerator SpawnWave(int count)
    {
        for (int i = 0; i < count; i++)
        {
            if (alive < maxAliveCap) SpawnOne(enemyPrefab, isBoss: false);
            yield return new WaitForSeconds(spawnIntervalInWave);
        }
    }

    void SpawnOne(GameObject prefab, bool isBoss)
    {
        if (prefab == null) return;

        Vector3 pos = GetSpawnPosition();
        var go = Instantiate(prefab, pos, Quaternion.identity);
        alive++;

        // when this enemy dies, decrement alive, notify KillManager, and handle boss threshold
        var hook = go.AddComponent<OnDestroyHook>();
        hook.isBoss = isBoss;
        hook.onDestroyed = () =>
        {
            alive = Mathf.Max(0, alive - 1);

            // still notify KillManager if present (keeps existing behavior)
            if (KillManager.I != null) KillManager.I.RegisterKill();

            // only minion kills count toward boss threshold
            if (!hook.isBoss)
            {
                _minionKillsSinceBoss++;

                // spawn a boss when threshold reached
                if (_running && bossPrefab != null && bossEveryMinionKills > 0 &&
                    _minionKillsSinceBoss >= bossEveryMinionKills)
                {
                    // ignore cap for boss if allowed
                    if (ignoreAliveCapForBoss || alive < maxAliveCap)
                    {
                        SpawnOne(bossPrefab, isBoss: true);
                        if (logWaves) Debug.Log($"[Spawner] Boss spawned after {_minionKillsSinceBoss} minion kills.");
                        _minionKillsSinceBoss = 0;
                    }
                }
            }
        };
    }

    Vector3 GetSpawnPosition()
    {
        Vector3 pos;

        if (cam != null && (spawnFromLeft || spawnFromRight || spawnFromTop || spawnFromBottom))
        {
            float halfH = cam.orthographicSize;
            float halfW = halfH * cam.aspect;
            Vector3 c = cam.transform.position;

            while (true)
            {
                int side = Random.Range(0, 4); // 0=L,1=R,2=T,3=B
                if (side == 0 && !spawnFromLeft) continue;
                if (side == 1 && !spawnFromRight) continue;
                if (side == 2 && !spawnFromTop) continue;
                if (side == 3 && !spawnFromBottom) continue;

                switch (side)
                {
                    case 0: // Left
                        pos = new Vector3(c.x - halfW - sideMargin,
                                          Random.Range(c.y - halfH + topBottomPadding, c.y + halfH - topBottomPadding),
                                          0f);
                        break;
                    case 1: // Right
                        pos = new Vector3(c.x + halfW + sideMargin,
                                          Random.Range(c.y - halfH + topBottomPadding, c.y + halfH - topBottomPadding),
                                          0f);
                        break;
                    case 2: // Top
                        pos = new Vector3(Random.Range(c.x - halfW + topBottomPadding, c.x + halfW - topBottomPadding),
                                          c.y + halfH + sideMargin,
                                          0f);
                        break;
                    default: // Bottom
                        pos = new Vector3(Random.Range(c.x - halfW + topBottomPadding, c.x + halfW - topBottomPadding),
                                          c.y - halfH - sideMargin,
                                          0f);
                        break;
                }
                break;
            }
        }
        else
        {
            // fallback: spawn around player in a circle
            Vector3 center = player ? player.position : Vector3.zero;
            float ang = Random.Range(0f, Mathf.PI * 2f);
            pos = center + new Vector3(Mathf.Cos(ang), Mathf.Sin(ang), 0f) * circleRadius;
        }

        return pos;
    }

    void OnDrawGizmosSelected()
    {
        if (cam == null) cam = Camera.main;
        Gizmos.color = gizmoColor;

        if (cam != null)
        {
            float halfH = cam.orthographicSize;
            float halfW = halfH * cam.aspect;
            Vector3 c = cam.transform.position;

            Vector3 a = new Vector3(c.x - halfW - sideMargin, c.y - halfH - sideMargin, 0f);
            Vector3 b = new Vector3(c.x + halfW + sideMargin, c.y + halfH + sideMargin, 0f);
            Gizmos.DrawWireCube((a + b) * 0.5f, b - a);
        }
        else if (player != null)
        {
            Gizmos.DrawWireSphere(player.position, circleRadius);
        }
    }

    class OnDestroyHook : MonoBehaviour
    {
        public System.Action onDestroyed;
        public bool isBoss;
        void OnDestroy() { onDestroyed?.Invoke(); }
    }
}
