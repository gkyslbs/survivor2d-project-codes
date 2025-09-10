using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class EnemyContactDamage : MonoBehaviour
{
    [Header("Damage")]
    public int damagePerTick = 1;      // damage dealt each tick while in contact
    public float tickInterval = 0.45f; // seconds between ticks while touching
    float timer;

    void Reset()
    {
        var col = GetComponent<Collider2D>();
        if (col) col.isTrigger = true; // triggers are easier for contact damage
    }

    void OnTriggerStay2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        timer += Time.deltaTime;
        if (timer >= tickInterval)
        {
            // deliver damage in spaced ticks: 1,1,1,…
            timer -= tickInterval;
            var hp = other.GetComponent<PlayerHealth>();
            if (hp) hp.TakeDamage(damagePerTick);
        }
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player")) timer = 0f; // reset tick timer on exit
    }
}
