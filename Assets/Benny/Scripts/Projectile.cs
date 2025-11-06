using UnityEngine;

public class Projectile : MonoBehaviour
{
    public float damage = 10f;
    public float headshotMultiplier = 2f;
    public GameObject impactEffect;
    public float stickLifetime = 10f; // How long projectile stays stuck before disappearing
    
    private bool hasHit = false;
    private Vector3 previousPosition;
    private Rigidbody rb;
    
    void Start()
    {
        rb = GetComponent<Rigidbody>();
        previousPosition = transform.position;
        
        // Ensure continuous collision detection
        if (rb != null)
        {
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        }
    }
    
    void FixedUpdate()
    {
        // Raycast backup check to prevent tunneling
        if (!hasHit && rb != null)
        {
            Vector3 direction = transform.position - previousPosition;
            float distance = direction.magnitude;
            
            if (distance > 0.01f)
            {
                RaycastHit hit;
                // Only check Ground layer
                int groundLayer = LayerMask.GetMask("Ground");
                
                if (Physics.Raycast(previousPosition, direction.normalized, out hit, distance, groundLayer))
                {
                    Debug.Log($"Raycast caught bullet tunneling! Hit: {hit.collider.name}");
                    
                    // Create a fake collision to process the hit
                    ContactPoint contact = new ContactPoint
                    {
                        point = hit.point,
                        normal = hit.normal
                    };
                    
                    ProcessHit(hit.collider, hit.point, hit.normal);
                }
            }
            
            previousPosition = transform.position;
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        if (hasHit) return;
        
        // Only hit objects on the "Ground" layer
        if (collision.gameObject.layer != LayerMask.NameToLayer("Ground"))
        {
            Debug.Log($"Projectile ignored collision with {collision.gameObject.name} (Layer: {LayerMask.LayerToName(collision.gameObject.layer)})");
            return;
        }
        
        Debug.Log($"OnCollisionEnter: Projectile hit Ground layer object: {collision.gameObject.name}");
        
        // Get contact point
        Vector3 hitPoint = collision.contacts.Length > 0 ? collision.contacts[0].point : transform.position;
        Vector3 hitNormal = collision.contacts.Length > 0 ? collision.contacts[0].normal : -transform.forward;
        
        ProcessHit(collision.collider, hitPoint, hitNormal, collision.transform);
    }
    
    void ProcessHit(Collider hitCollider, Vector3 hitPoint, Vector3 hitNormal, Transform hitTransform = null)
    {
        if (hasHit) return;
        hasHit = true;
        
        // Check what we hit
        bool isHeadshot = hitCollider.CompareTag("Head");
        
        // Find enemy
        EnemyDamage target = hitCollider.GetComponent<EnemyDamage>();
        if (target == null && hitTransform != null)
        {
            // Search parent hierarchy
            Transform enemyTransform = hitTransform;
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
        if (impactEffect != null)
        {
            Instantiate(impactEffect, hitPoint, Quaternion.LookRotation(hitNormal));
        }
        
        // Create bullet hole on obstacles
        if (hitCollider.CompareTag("Obstacle"))
        {
            if (GlobalReferences.Instance != null && GlobalReferences.Instance.bulletImpactEffectPrefab != null)
            {
                GameObject hole = Instantiate(GlobalReferences.Instance.bulletImpactEffectPrefab, hitPoint, Quaternion.LookRotation(hitNormal));
                hole.transform.SetParent(hitCollider.transform);
            }
        }
        
        Debug.Log("Projectile hit: " + hitCollider.name + (isHeadshot ? " [HEADSHOT]" : ""));
        
        // Stick the projectile to the surface
        StickToSurface(hitCollider.transform, hitPoint, hitNormal);
    }
    
    void StickToSurface(Transform surfaceTransform, Vector3 hitPoint, Vector3 hitNormal)
    {
        // Stop physics
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.isKinematic = true;
        }
        
        // Parent to the hit object so it moves with it
        if (surfaceTransform != null)
        {
            transform.SetParent(surfaceTransform);
        }
        
        // Position at contact point and align with surface normal
        transform.position = hitPoint;
        transform.rotation = Quaternion.LookRotation(hitNormal);
        
        // Destroy after some time
        Destroy(gameObject, stickLifetime);
        
        Debug.Log($"Projectile stuck to {(surfaceTransform != null ? surfaceTransform.gameObject.name : "surface")} for {stickLifetime} seconds");
    }
}

