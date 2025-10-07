using UnityEngine;

public class Throwable : MonoBehaviour
{
    [SerializeField]
    float delay = 3f;
    [SerializeField]
    float damageRadius = 20f;
    [SerializeField]
    float explosionForce = 1200f;
    [SerializeField]
    GameObject explosionEffect;

    float countdown;
    bool hasExploded = false;
    public bool hasBeenThrown = false;


    public enum ThrowableType { Grenade, Rock }
    public ThrowableType throwableType;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        countdown = delay;
    }

    // Update is called once per frame
    void Update()
    {
        if (!hasBeenThrown || hasExploded) return;

        countdown -= Time.deltaTime;
        if (countdown <= 0f)
        {
            hasExploded = true;
            Explode();
        }

    }

    private void Explode()
    {
        // Show explosion effect
        GetThrowableEffect();
        Destroy(gameObject);
    }

    private void GetThrowableEffect()
    {
        switch (throwableType)
        {
            case ThrowableType.Grenade:
                Debug.Log("Grenade exploded with radius " + damageRadius + " and force " + explosionForce);
                // Here you would typically instantiate an explosion effect prefabb 
                // and apply damage/force to nearby objects.
                GrenadeEffect();
                break;
            case ThrowableType.Rock:
                Debug.Log("Rock impact with radius " + damageRadius + " and force " + explosionForce);
                // Here you would typically instantiate an impact effect prefab
                // and apply damage/force to nearby objects.
                break;
            default:
                Debug.Log("Unknown throwable type");
                break;
        }
    }

    private void GrenadeEffect()
    {
        // GameObject explosionEffect = GlobalReferences.Instance.explosionEffect;
        Instantiate(explosionEffect, transform.position, transform.rotation);


        Collider[] colliders = Physics.OverlapSphere(transform.position, damageRadius);
        foreach (Collider nearbyObject in colliders)
        {
            Rigidbody rb = nearbyObject.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.AddExplosionForce(explosionForce, transform.position, damageRadius);
            }
        }
    }
}
