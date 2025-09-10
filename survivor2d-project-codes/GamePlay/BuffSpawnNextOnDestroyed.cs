using UnityEngine;
using System.Linq;

public class BuffSpawnNextOnDestroyed : MonoBehaviour
{
    [Header("Next Buff")]
    public GameObject nextBuffPrefab;         // e.g., SubmachineBuff prefab

    [Header("Spawn Points")]
    public Transform[] spawnPoints;           // can be left empty
    public bool autoFindByTag = true;
    public string spawnPointTag = "BuffSpawnPoint";

    [Header("Options")]
    public bool chooseRandom = true;
    public bool keepRotation = false;
    public Vector3 extraOffset;

    void OnDestroy()
    {
        if (!Application.isPlaying) return;
        if (!nextBuffPrefab) return;

        Transform[] points = spawnPoints;
        if ((points == null || points.Length == 0) && autoFindByTag)
        {
            var gos = GameObject.FindGameObjectsWithTag(spawnPointTag);
            points = gos != null ? gos.Select(g => g.transform).ToArray() : null;
        }

        if (points == null || points.Length == 0)
        {
            // no points → spawn at current position
            Instantiate(nextBuffPrefab, transform.position + extraOffset,
                        keepRotation ? transform.rotation : Quaternion.identity);
            return;
        }

        Transform t = chooseRandom ? points[Random.Range(0, points.Length)] : points[0];
        Quaternion rot = keepRotation ? transform.rotation : Quaternion.identity;
        Instantiate(nextBuffPrefab, t.position + extraOffset, rot);
    }
}
