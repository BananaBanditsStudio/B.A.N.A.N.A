using UnityEngine;
using System.Collections.Generic;

public class DistractionInteractable : Interactable
{
    [Header("Distraction Settings")]
    [Tooltip("Maximum distance for enemies to be considered 'in vicinity'")]
    public float detectionRadius = 30f;
    
    [Tooltip("Tag to filter enemies (leave empty to find all enemies)")]
    public string enemyTag = "";
    
    [Tooltip("Should this work only once, or can it be triggered multiple times?")]
    public bool oneTimeUse = false;
    
    [Tooltip("Visualize the detection radius in the editor")]
    public bool showDetectionRadius = true;
    
    [Header("Explosion Settings")]
    [Tooltip("Explosion effect prefab to spawn when interacted with (optional)")]
    public GameObject explosionEffect;
    
    [Tooltip("Explosion force to apply to nearby rigidbodies (0 = no force)")]
    public float explosionForce = 0f;
    
    [Tooltip("Radius of the explosion effect/force")]
    public float explosionRadius = 5f;
    
    [Tooltip("Should this object be destroyed after explosion?")]
    public bool destroyOnExplode = true;
    
    [Tooltip("Delay before destroying the object (allows explosion to play)")]
    public float destroyDelay = 0.1f;
    
    private bool hasBeenUsed = false;

    protected override void Interact()
    {
        // Check if this is a one-time use and has already been used
        if (oneTimeUse && hasBeenUsed)
        {
            return;
        }
        
        // Trigger explosion effect first
        TriggerExplosion();
        
        // Find all nearby enemies
        List<EnemyWithSM> nearbyEnemies = FindNearbyEnemies();
        
        if (nearbyEnemies.Count > 0)
        {
            Debug.Log($"DistractionInteractable: Triggering distraction for {nearbyEnemies.Count} enemy/enemies");
            
            // Trigger distraction for each nearby enemy
            foreach (EnemyWithSM enemy in nearbyEnemies)
            {
                if (enemy != null)
                {
                    enemy.TriggerDistraction(transform.position);
                }
            }
        }
        else
        {
            Debug.Log("DistractionInteractable: No enemies found in vicinity");
        }
        
        // Mark as used
        hasBeenUsed = true;
        
        // Destroy or hide the object if configured
        if (destroyOnExplode)
        {
            if (destroyDelay > 0f)
            {
                Destroy(gameObject, destroyDelay);
            }
            else
            {
                Destroy(gameObject);
            }
        }
    }
    
    /// <summary>
    /// Finds all EnemyWithSM components within the detection radius
    /// </summary>
    private List<EnemyWithSM> FindNearbyEnemies()
    {
        List<EnemyWithSM> nearbyEnemies = new List<EnemyWithSM>();
        
        // Get all enemies in the scene
        EnemyWithSM[] allEnemies = FindObjectsOfType<EnemyWithSM>();
        
        foreach (EnemyWithSM enemy in allEnemies)
        {
            if (enemy == null) continue;
            
            // Check distance
            float distance = Vector3.Distance(transform.position, enemy.transform.position);
            if (distance > detectionRadius) continue;
            
            // Check tag filter if specified
            if (!string.IsNullOrEmpty(enemyTag) && !enemy.gameObject.CompareTag(enemyTag))
            {
                continue;
            }
            
            nearbyEnemies.Add(enemy);
        }
        
        return nearbyEnemies;
    }
    
    /// <summary>
    /// Triggers the explosion effect and applies physics forces
    /// </summary>
    private void TriggerExplosion()
    {
        // Spawn explosion effect if assigned
        if (explosionEffect != null)
        {
            Instantiate(explosionEffect, transform.position, transform.rotation);
        }
        
        // Apply explosion force to nearby rigidbodies if configured
        if (explosionForce > 0f && explosionRadius > 0f)
        {
            Collider[] colliders = Physics.OverlapSphere(transform.position, explosionRadius);
            foreach (Collider nearbyObject in colliders)
            {
                // Skip self
                if (nearbyObject.gameObject == gameObject) continue;
                
                // Apply physics force
                Rigidbody rb = nearbyObject.GetComponent<Rigidbody>();
                if (rb != null)
                {
                    rb.AddExplosionForce(explosionForce, transform.position, explosionRadius);
                }
            }
        }
    }
    
    /// <summary>
    /// Draw the detection radius and explosion radius in the editor
    /// </summary>
    private void OnDrawGizmosSelected()
    {
        if (showDetectionRadius)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, detectionRadius);
        }
        
        if (explosionForce > 0f || explosionEffect != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, explosionRadius);
        }
    }
}

