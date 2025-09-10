using UnityEngine;

public class BuffPickup : MonoBehaviour
{
    public float lifeTime = 30f; // how long it stays on the ground
    public bool destroyOnPickup = true;

    void Start()
    {
        Destroy(gameObject, lifeTime);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        var equip = other.GetComponent<PlayerEquip>();
        if (equip == null) equip = other.GetComponentInParent<PlayerEquip>();

        if (equip != null)
        {
            equip.EquipWeapon();
            if (destroyOnPickup) Destroy(gameObject);
        }
    }
}
