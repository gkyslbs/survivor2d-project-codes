using UnityEngine;
using System;
using UnityEngine.SceneManagement;
using System.Collections.Generic;  // for de-dupe set

public class KillManager : MonoBehaviour
{
    public static KillManager I { get; private set; }

    [Header("Kills / Level")]
    public int kills = 0;

    [Header("Boss Rules (Base)")]
    public int bossAtKills = 50;  // default 50
    public GameObject bossPrefab;

    [Tooltip("Boss spawns at this distance from player (or just off-screen).")]
    public float bossSpawnRadius = 12f;
    public float sideMargin = 2f;

    [Header("State (legacy-compat)")]
    public bool bossSpawned = false;

    public static event Action<int> OnKillsChanged;
    public event Action OnBossSpawn;
    public event Action OnLevelComplete;

    [Header("Per-Scene Override")]
    public bool usePerSceneThreshold = true;
    public string level1SceneName = "Level1";
    public string level2SceneName = "Level2";
    public int level1BossAtKills = 50;
    public int level2BossAtKills = 30;

    // ---- De-dupe helpers ----
    // block counting the same victim twice
    readonly HashSet<int> _processedVictims = new HashSet<int>(512);
    float _lastKillTime;
    [Tooltip("If the old RegisterKill() fires twice rapidly, ignore the second one within this window.")]
    public float duplicateBlockWindow = 0.04f; // 40ms

    Transform player;
    Camera cam;
    int nextBossAt;

    void Awake()
    {
        if (I != null && I != this) { Destroy(gameObject); return; }
        I = this;

        if (usePerSceneThreshold)
        {
            string scn = SceneManager.GetActiveScene().name;
            if (!string.IsNullOrEmpty(level2SceneName) && scn == level2SceneName)
                bossAtKills = Mathf.Max(1, level2BossAtKills);
            else if (!string.IsNullOrEmpty(level1SceneName) && scn == level1SceneName)
                bossAtKills = Mathf.Max(1, level1BossAtKills);
        }

        var p = GameObject.FindGameObjectWithTag("Player");
        if (p) player = p.transform;

        cam = Camera.main;
#if UNITY_2023_1_OR_NEWER
        if (!cam)
        {
            var anyCam = UnityEngine.Object.FindFirstObjectByType<Camera>();
            if (anyCam != null) cam = anyCam;
        }
#else
        if (!cam)
        {
            var anyCam = UnityEngine.Object.FindObjectOfType<Camera>();
            if (anyCam != null) cam = anyCam;
        }
#endif

        nextBossAt = Mathf.Max(1, bossAtKills);
        OnKillsChanged?.Invoke(kills);
    }

    // preferred API: with victim source
    public void RegisterKillFrom(GameObject victim)
    {
        int id = victim ? victim.GetInstanceID() : 0;
        if (id != 0)
        {
            if (_processedVictims.Contains(id)) return; // don't count same enemy twice
            _processedVictims.Add(id);
        }
        AddKillOnce();
    }

    // legacy API: no victim provided
    public void RegisterKill()
    {
        // if two calls happen back to back, ignore the second (safety)
        if (Time.unscaledTime - _lastKillTime < duplicateBlockWindow) return;
        AddKillOnce();
    }

    void AddKillOnce()
    {
        _lastKillTime = Time.unscaledTime;

        kills++;
        OnKillsChanged?.Invoke(kills);

        if (kills >= nextBossAt)
        {
            SpawnBoss();
            nextBossAt += Mathf.Max(1, bossAtKills);
        }
    }

    public void RegisterBossSpawned()
    {
        bossSpawned = true;
        OnBossSpawn?.Invoke();
    }

    public void RegisterBossDeath()
    {
        bossSpawned = false;
        OnLevelComplete?.Invoke();
    }

    void SpawnBoss()
    {
        if (!bossPrefab)
        {
            Debug.LogWarning("[KillManager] bossPrefab is not assigned!");
            return;
        }

        Vector3 pos;
        if (cam != null)
        {
            float halfH = cam.orthographicSize;
            float halfW = halfH * cam.aspect;
            Vector3 c = cam.transform.position;

            int side = UnityEngine.Random.Range(0, 4);
            switch (side)
            {
                case 0: pos = new Vector3(c.x - halfW - sideMargin, UnityEngine.Random.Range(c.y - halfH, c.y + halfH), 0f); break;
                case 1: pos = new Vector3(c.x + halfW + sideMargin, UnityEngine.Random.Range(c.y - halfH, c.y + halfH), 0f); break;
                case 2: pos = new Vector3(UnityEngine.Random.Range(c.x - halfW, c.x + halfW), c.y + halfH + sideMargin, 0f); break;
                default: pos = new Vector3(UnityEngine.Random.Range(c.x - halfW, c.x + halfW), c.y - halfH - sideMargin, 0f); break;
            }
        }
        else
        {
            Vector3 center = player ? player.position : Vector3.zero;
            float ang = UnityEngine.Random.Range(0f, Mathf.PI * 2f);
            pos = center + new Vector3(Mathf.Cos(ang), Mathf.Sin(ang), 0f) * bossSpawnRadius;
        }

        var boss = Instantiate(bossPrefab, pos, Quaternion.identity);
        bossSpawned = true;
        OnBossSpawn?.Invoke();
        Debug.Log($"[KillManager] BOSS SPAWN at kills={kills} (threshold={bossAtKills})");

        var hook = boss.AddComponent<BossDestroyHook>();
        hook.onDestroyed = () => RegisterBossDeath();
    }

    class BossDestroyHook : MonoBehaviour
    {
        public Action onDestroyed;
        void OnDestroy() { onDestroyed?.Invoke(); }
    }
}
