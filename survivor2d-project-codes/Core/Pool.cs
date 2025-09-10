using System.Collections.Generic;
using UnityEngine;

public static class Pool
{
    // Simple object pooling utility.
    // NOTE: This is intentionally minimal. Good enough for our use-case.

    class Bucket
    {
        public readonly Stack<GameObject> stack = new Stack<GameObject>(64);
        public readonly Transform root;

        public Bucket(string name)
        {
            root = new GameObject("[Pool] " + name).transform;
            Object.DontDestroyOnLoad(root.gameObject); // keep pool across scenes
        }
    }

    static readonly Dictionary<GameObject, Bucket> buckets = new();
    static readonly Dictionary<GameObject, GameObject> spawnedToPrefab = new(); // instance -> prefab

    static Bucket GetBucket(GameObject prefab)
    {
        if (!buckets.TryGetValue(prefab, out var b))
        {
            b = new Bucket(prefab.name);
            buckets[prefab] = b;
        }
        return b;
    }

    public static void Prewarm(GameObject prefab, int count)
    {
        if (!prefab || count <= 0) return;

        var b = GetBucket(prefab);
        for (int i = 0; i < count; i++)
        {
            var go = Object.Instantiate(prefab);
            go.SetActive(false);
            go.transform.SetParent(b.root, false);
            b.stack.Push(go);
        }
    }

    public static GameObject Spawn(GameObject prefab, Vector3 pos, Quaternion rot)
    {
        if (!prefab) return null;

        var b = GetBucket(prefab);
        GameObject go = b.stack.Count > 0 ? b.stack.Pop() : Object.Instantiate(prefab);
        spawnedToPrefab[go] = prefab;

        var t = go.transform;
        t.SetParent(null, false); // detach from pool root
        t.position = pos;
        t.rotation = rot;

        if (!go.activeSelf) go.SetActive(true);
        return go;
    }

    public static void Despawn(GameObject instance, float delay = 0f)
    {
        if (!instance) return;
        instance.GetComponent<MonoBehaviour>()?.StartCoroutine(CoDespawn(instance, delay));
        // NOTE: We rely on any MonoBehaviour attached to run the coroutine. Works fine here.
    }

    static System.Collections.IEnumerator CoDespawn(GameObject instance, float delay)
    {
        if (delay > 0f) yield return new WaitForSeconds(delay);

        if (!spawnedToPrefab.TryGetValue(instance, out var prefab) || prefab == null)
        {
            // not tracked by the pool -> just destroy it normally
            Object.Destroy(instance);
            yield break;
        }

        var b = GetBucket(prefab);
        instance.SetActive(false);
        instance.transform.SetParent(b.root, false);
        b.stack.Push(instance);
        spawnedToPrefab.Remove(instance);
    }
}
