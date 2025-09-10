using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Collider2D))]
public class Coin : MonoBehaviour
{
    [Header("Value")]
    public int value = 1;

    [Header("Lifetime")]
    public float lifeTime = 7f;   // auto-despawn after 7s
    public float fadeOut = 0.4f;  // fade during the last 0.4s (set 0 to disable)

    [Header("Magnet")]
    public bool enableMagnet = true;
    public float magnetRadius = 4f;
    public float magnetSpeed = 8f;
    public float magnetAccel = 30f;
    public float magnetSnapDistance = 0.15f;

    SpriteRenderer[] srs;
    Coroutine despawnCo;

    Rigidbody2D rb;
    Transform player;

    // SFX throttle so multiple pickups don't spam the sound
    static float _lastCoinSfxTime = -999f;
    public static float coinSfxMinInterval = 0.07f; // shared for all coins

    void Awake()
    {
        srs = GetComponentsInChildren<SpriteRenderer>(true);

        var col = GetComponent<Collider2D>();
        if (col) col.isTrigger = true;

        rb = GetComponent<Rigidbody2D>();
        if (rb)
        {
            rb.gravityScale = 0f;
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        }
    }

    void OnEnable()
    {
        if (despawnCo != null) StopCoroutine(despawnCo);
        despawnCo = StartCoroutine(DespawnRoutine());

        var p = GameObject.FindGameObjectWithTag("Player");
        if (p) player = p.transform;
    }

    IEnumerator DespawnRoutine()
    {
        if (lifeTime <= 0f) yield break;

        float wait = Mathf.Max(0f, lifeTime - Mathf.Max(0f, fadeOut));
        if (wait > 0f) yield return new WaitForSeconds(wait);

        if (fadeOut > 0f && srs != null && srs.Length > 0)
        {
            float t = 0f;
            while (t < fadeOut)
            {
                t += Time.deltaTime;
                float a = Mathf.Lerp(1f, 0f, t / fadeOut);
                for (int i = 0; i < srs.Length; i++)
                {
                    var c = srs[i].color;
                    c.a = a;
                    srs[i].color = c;
                }
                yield return null;
            }
        }

        Destroy(gameObject);
    }

    void Update()
    {
        if (!enableMagnet || player == null) return;

        Vector2 pos = transform.position;
        Vector2 to = (Vector2)player.position - pos;
        float dist = to.magnitude;
        if (dist > magnetRadius) return;

        if (dist <= magnetSnapDistance)
        {
            transform.position = Vector2.MoveTowards(pos, player.position, magnetSpeed * Time.deltaTime);
            return;
        }

        Vector2 dir = to / Mathf.Max(0.0001f, dist);

        if (rb)
        {
#if UNITY_2022_2_OR_NEWER
            rb.linearVelocity = Vector2.MoveTowards(rb.linearVelocity, dir * magnetSpeed, magnetAccel * Time.deltaTime);
#else
            rb.velocity = Vector2.MoveTowards(rb.velocity, dir * magnetSpeed, magnetAccel * Time.deltaTime);
#endif
        }
        else
        {
            float step = magnetSpeed * Time.deltaTime;
            transform.position = (Vector2)transform.position + dir * step;
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        if (GameManager.I != null)
        {
            GameManager.I.AddCoins(value);
        }

        // single coin sound for clusters
        if (Time.unscaledTime - _lastCoinSfxTime >= coinSfxMinInterval)
        {
            SfxManager.Play(SfxKey.Coin);
            _lastCoinSfxTime = Time.unscaledTime;
        }

        Destroy(gameObject);
    }
}
