using UnityEngine;

public class BananaPeelProjectile : MonoBehaviour
{
    [Header("Projectile Settings")]
    public float impactRadius = 2f; // Radius to check for enemy impact
    public LayerMask enemyLayer = -1; // Layer mask for enemies
    
    private EnemyDamage targetEnemy;
    private Vector3 hitNormal;
    private BananaPeelThrower thrower;
    private bool hasHit = false;

    public void SetTarget(EnemyDamage enemy, Vector3 normal, BananaPeelThrower bananaThrower)
    {
        targetEnemy = enemy;
        hitNormal = normal;
        thrower = bananaThrower;
    }

    void Update()
    {
        // Check if we're close enough to the target enemy to trigger the effect
        if (!hasHit && targetEnemy != null)
        {
            float distanceToTarget = Vector3.Distance(transform.position, targetEnemy.transform.position);
            
            // Debug distance
            if (distanceToTarget <= impactRadius * 2f) // Show debug when getting close
            {
                Debug.Log($"Banana peel distance to enemy: {distanceToTarget}");
            }
            
            if (distanceToTarget <= impactRadius)
            {
                Debug.Log("Banana peel hit enemy!");
                TriggerBananaPeelEffect();
            }
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        // Check if we hit the target enemy or any enemy
        EnemyDamage enemy = collision.gameObject.GetComponent<EnemyDamage>();
        
        if (enemy != null && !hasHit)
        {
            TriggerBananaPeelEffect();
        }
        else if (!hasHit)
        {
            // Hit something else, just stick to the surface
            StickToSurface(collision);
        }
    }

    void TriggerBananaPeelEffect()
    {
        if (hasHit) return;
        
        hasHit = true;
        
        // Stop the projectile
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.isKinematic = true;
        }

        // Position the banana peel at the enemy's feet
        Vector3 spawnPosition = targetEnemy.transform.position;
        spawnPosition.y = 0.09f;
        transform.position = spawnPosition;
        transform.rotation = Quaternion.LookRotation(hitNormal);

        // Destroy the projectile after a delay (optional)
        Destroy(gameObject, 5f);
    }

    void StickToSurface(Collision collision)
    {
        // Make the banana peel stick to the surface it hit
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.isKinematic = true;
        }

        // Orient the banana peel to the surface normal
        if (collision.contacts.Length > 0)
        {
            Vector3 surfaceNormal = collision.contacts[0].normal;
            transform.rotation = Quaternion.LookRotation(surfaceNormal);
        }

        // Destroy after some time if it doesn't hit an enemy
        Destroy(gameObject, 10f);
    }
}
