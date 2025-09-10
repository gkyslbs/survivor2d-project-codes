using System.Collections.Generic;
using UnityEngine;

public static class EnemyRegistry
{
    public static readonly List<Enemy> All = new List<Enemy>();

    // Find the nearest enemy (single pass; keeps it light)
    public static Enemy GetNearest(Vector2 from, float maxRadius = Mathf.Infinity)
    {
        Enemy best = null;
        float bestSq = maxRadius * maxRadius;
        for (int i = 0; i < All.Count; i++)
        {
            var e = All[i];
            if (!e || !e.gameObject.activeInHierarchy) continue;
            float sq = ((Vector2)e.transform.position - from).sqrMagnitude;
            if (sq < bestSq) { bestSq = sq; best = e; }
        }
        return best;
    }
}
