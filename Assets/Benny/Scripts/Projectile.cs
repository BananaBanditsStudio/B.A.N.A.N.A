using UnityEngine;

public class Projectile : MonoBehaviour
{
    public float damage = 10f;
    public float headshotMultiplier = 2f;
    public GameObject impactEffect;
    public float stickLifetime = 10f; // How long projectile stays stuck before disappearing
    
    private bool hasHit = false;

    void OnCollisionEnter(Collision collision)
    {
        if (hasHit) return;
        
        hasHit = true;
        
        // Check what we hit - prioritize enemies
        bool isHeadshot = collision.collider.CompareTag("Head");
        
        // Find enemy - check collision object and parent hierarchy
        EnemyDamage target = collision.collider.GetComponent<EnemyDamage>();
        if (target == null)
        {
            // Search parent hierarchy
            Transform enemyTransform = collision.transform;
            while (enemyTransform != null && target == null)
            {
                target = enemyTransform.GetComponent<EnemyDamage>();
                if (target == null)
                    enemyTransform = enemyTransform.parent;
                else
                    break;
            }
        }
        
        // Deal damage to enemy if hit
        if (target != null)
        {
            float finalDamage = damage;
            
            if (isHeadshot)
            {
                finalDamage = damage * headshotMultiplier;
                Debug.Log("HEADSHOT! Damage: " + finalDamage);
            }
            
            target.TakeDamage(finalDamage, isHeadshot);
            Debug.Log("Projectile hit enemy: " + collision.collider.name + (isHeadshot ? " [HEADSHOT]" : ""));
            
            // Create impact effect on enemy
            if (impactEffect != null && collision.contacts.Length > 0)
            {
                ContactPoint contact = collision.contacts[0];
                Instantiate(impactEffect, contact.point, Quaternion.LookRotation(contact.normal));
            }
            
            // Stick the projectile to the enemy
            StickToSurface(collision);
            return; // Exit early since we hit an enemy
        }
        
        // If we didn't hit an enemy, check for other surfaces (Ground layer, Obstacles, etc.)
        // Only hit objects on the "Ground" layer or tagged as "Obstacle"
        bool isGroundLayer = collision.gameObject.layer == LayerMask.NameToLayer("Ground");
        bool isObstacle = collision.collider.CompareTag("Obstacle");
        
        if (!isGroundLayer && !isObstacle)
        {
            Debug.Log($"Projectile ignored collision with {collision.gameObject.name} (Layer: {LayerMask.LayerToName(collision.gameObject.layer)})");
            hasHit = false; // Allow projectile to continue
            return;
        }
        
        Debug.Log($"Projectile hit surface: {collision.gameObject.name}");
        
        // Create impact effect
        if (impactEffect != null && collision.contacts.Length > 0)
        {
            ContactPoint contact = collision.contacts[0];
            Instantiate(impactEffect, contact.point, Quaternion.LookRotation(contact.normal));
        }
        
        // Create bullet hole on obstacles
        if (isObstacle && collision.contacts.Length > 0)
        {
            ContactPoint contact = collision.contacts[0];
            if (GlobalReferences.Instance != null && GlobalReferences.Instance.bulletImpactEffectPrefab != null)
            {
                GameObject hole = Instantiate(GlobalReferences.Instance.bulletImpactEffectPrefab, contact.point, Quaternion.LookRotation(contact.normal));
                hole.transform.SetParent(collision.collider.transform);
            }
        }
        
        // Stick the projectile to the surface
        StickToSurface(collision);
    }
    
    void StickToSurface(Collision collision)
    {
        // Stop physics
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.isKinematic = true;
        }
        
        // Parent to the hit object so it moves with it
        transform.SetParent(collision.transform);
        
        // Position at contact point and align with surface normal
        if (collision.contacts.Length > 0)
        {
            ContactPoint contact = collision.contacts[0];
            transform.position = contact.point;
            transform.rotation = Quaternion.LookRotation(contact.normal);
        }
        
        // Destroy after some time
        Destroy(gameObject, stickLifetime);
        
        Debug.Log($"Projectile stuck to {collision.gameObject.name} for {stickLifetime} seconds");
    }
}

