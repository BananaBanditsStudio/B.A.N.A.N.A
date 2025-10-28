using UnityEngine;

public class Throwable : MonoBehaviour
{
    [SerializeField]
    float delay = 2f;
    [SerializeField]
    float damageRadius = 8f; // tighter stun/explosion radius (tweak in inspector)
    [SerializeField]
    float explosionForce = 1200f;
    [SerializeField]
    GameObject explosionEffect;
    [SerializeField]
    float stunDuration = 1.5f; // how long to keep enemies stunned
    [SerializeField]
    AudioSource audioSource;
    float countdown;
    bool hasExploded = false;
    public bool hasBeenThrown = false;


    public enum ThrowableType { Grenade, Rock }
    public ThrowableType throwableType;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        countdown = delay;
        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();
    }

    // Update is called once per frame
    void Update()
    {
        if (!hasBeenThrown || hasExploded) return;

        countdown -= Time.deltaTime;
        // Play audio source when 1s remaining, once
        if (audioSource != null && countdown <= 0.3f && !audioSource.isPlaying)
        {
            audioSource.Play();
        }
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
        // Spawn VFX
        Instantiate(explosionEffect, transform.position, transform.rotation);

        // Affect nearby objects
        Collider[] colliders = Physics.OverlapSphere(transform.position, damageRadius);
        foreach (Collider nearbyObject in colliders)
        {
            // Physics impulse
            Rigidbody rb = nearbyObject.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.AddExplosionForce(explosionForce, transform.position, damageRadius);
            }

            // Simple: call enemy's own stun if present (robust parent search)
            var patrol = nearbyObject.GetComponentInParent<SimplePatrol>();
            if (patrol != null) 
            {
                // Apply stun to the enemy
                patrol.ApplyStun(stunDuration);
            }
        }
        
    } 
}
