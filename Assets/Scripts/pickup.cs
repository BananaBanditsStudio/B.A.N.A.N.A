using UnityEngine;

public class EnemyWeaponHandler : MonoBehaviour
{
    public Transform weaponHolder;     // assign the WeaponHolder
    public GameObject bat;             // assign the bat object
    public bool isHolding = true;

    void Start()
    {
        // make sure bat starts in hand
        EquipBat();
    }

    public void EquipBat()
    {
        bat.transform.SetParent(weaponHolder);
        bat.transform.localPosition = Vector3.zero;
        bat.transform.localRotation = Quaternion.identity;

        if (bat.TryGetComponent<Rigidbody>(out var rb))
            rb.isKinematic = true;
        if (bat.TryGetComponent<Collider>(out var col))
            col.enabled = false;

        isHolding = true;
    }

    public void DropBat()
    {
        bat.transform.SetParent(null);
        if (bat.TryGetComponent<Rigidbody>(out var rb))
            rb.isKinematic = false;
        if (bat.TryGetComponent<Collider>(out var col))
            col.enabled = true;

        isHolding = false;
    }
}
