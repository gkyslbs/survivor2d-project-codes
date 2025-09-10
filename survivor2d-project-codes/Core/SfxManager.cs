using UnityEngine;

public enum SfxKey { Coin, Shoot, EnemyDie, BuffEquip, Hit }

public class SfxManager : MonoBehaviour
{
    public static SfxManager I { get; private set; }

    [System.Serializable]
    public class SfxEntry
    {
        public SfxKey key;
        public AudioClip[] clips;     // if you add multiple, one is picked at random
        [Range(0f, 1f)] public float volume = 1f;
        public Vector2 pitchJitter = new Vector2(1f, 1f); // e.g., (0.95, 1.05)
        public bool spatial = false;  // true → 3D at a position, false → 2D on UI
        public float minDistance = 5f, maxDistance = 20f; // used when spatial
    }

    [Header("Table")]
    public SfxEntry[] table;

    AudioSource oneShot2D;

    void Awake()
    {
        if (I != null && I != this) { Destroy(gameObject); return; }
        I = this;

        // a dedicated 2D one-shot audio source for quick UI sounds
        oneShot2D = gameObject.AddComponent<AudioSource>();
        oneShot2D.playOnAwake = false;
        oneShot2D.spatialBlend = 0f; // 2D
    }

    // Simple static helpers
    public static void Play(SfxKey key) { I?.PlayInternal(key, Vector3.zero, false); }
    public static void PlayAt(SfxKey key, Vector3 pos) { I?.PlayInternal(key, pos, true); }

    SfxEntry Find(SfxKey key)
    {
        for (int i = 0; i < table.Length; i++)
            if (table[i].key == key) return table[i];
        return null;
    }

    void PlayInternal(SfxKey key, Vector3 pos, bool forcedSpatial)
    {
        var e = Find(key);
        if (e == null || e.clips == null || e.clips.Length == 0) return;

        var clip = e.clips[Random.Range(0, e.clips.Length)];
        float pitch = Random.Range(e.pitchJitter.x, e.pitchJitter.y);
        bool doSpatial = e.spatial || forcedSpatial;

        if (!doSpatial)
        {
            // 2D one-shot (UI/global)
            oneShot2D.pitch = pitch;
            oneShot2D.PlayOneShot(clip, e.volume);
        }
        else
        {
            // 3D one-shot at world position (auto-destroy after playback)
            var go = new GameObject("SFX_" + key);
            go.transform.position = pos;
            var a = go.AddComponent<AudioSource>();
            a.clip = clip;
            a.volume = e.volume;
            a.pitch = pitch;
            a.spatialBlend = 1f;
            a.minDistance = e.minDistance;
            a.maxDistance = e.maxDistance;
            a.rolloffMode = AudioRolloffMode.Linear;
            a.Play();
            Destroy(go, clip.length / Mathf.Max(0.01f, Mathf.Abs(pitch)));
        }
    }
}
