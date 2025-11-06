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
        
        // Only hit objects on the "Ground" layer
        if (collision.gameObject.layer != LayerMask.NameToLayer("Ground"))
        {
            Debug.Log($"Projectile ignored collision with {collision.gameObject.name} (Layer: {LayerMask.LayerToName(collision.gameObject.layer)})");
            return;
        }
        
        hasHit = true;
        Debug.Log($"Projectile hit Ground layer object: {collision.gameObject.name}");
        
        // Check what we hit
        bool isHeadshot = collision.collider.CompareTag("Head");
        
        // Find enemy
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
        
        // Deal damage
        if (target != null)
        {
            float finalDamage = damage;
            
            if (isHeadshot)
            {
                finalDamage = damage * headshotMultiplier;
                Debug.Log("HEADSHOT! Damage: " + finalDamage);
            }
            
            target.TakeDamage(finalDamage, isHeadshot);
        }
        
        // Create impact effect
        if (impactEffect != null && collision.contacts.Length > 0)
        {
            ContactPoint contact = collision.contacts[0];
            Instantiate(impactEffect, contact.point, Quaternion.LookRotation(contact.normal));
        }
        
        // Create bullet hole on obstacles
        if (collision.collider.CompareTag("Obstacle") && collision.contacts.Length > 0)
        {
            ContactPoint contact = collision.contacts[0];
            if (GlobalReferences.Instance != null && GlobalReferences.Instance.bulletImpactEffectPrefab != null)
            {
                GameObject hole = Instantiate(GlobalReferences.Instance.bulletImpactEffectPrefab, contact.point, Quaternion.LookRotation(contact.normal));
                hole.transform.SetParent(collision.collider.transform);
            }
        }
        
        Debug.Log("Projectile hit: " + collision.collider.name + (isHeadshot ? " [HEADSHOT]" : ""));
        
        // Stick the projectile to the wall
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

