using UnityEngine;

/// <summary>
/// Simple enemy spawner that instantiates an EnemyWithSM and makes it
/// immediately very aggressive (huge sight range and 360 FOV).
/// 
/// Hook the public SpawnEnemy() method up to an EventOnlyInteractable
/// via an InteractionEvent → OnInteract UnityEvent.
/// </summary>
public class EnemySpawner : MonoBehaviour
{
    [Header("Spawn Settings")]
    [Tooltip("EnemyWithSM prefab to spawn")]
    public EnemyWithSM enemyPrefab;

    [Tooltip("Optional custom spawn point. If null, uses this GameObject's transform.")]
    public Transform spawnPoint;

    [Tooltip("How many enemies to spawn when triggered")]
    public int spawnCount = 5;

    [Header("Aggression Settings")]
    [Tooltip("Sight distance to set on spawned enemies")]
    public float aggressiveSightDistance = 1000f;

    [Tooltip("Field of view (degrees) to set on spawned enemies (360 = all directions)")]
    public float aggressiveFieldOfView = 360f;

    /// <summary>
    /// Public method to be called from EventOnlyInteractable (UnityEvent).
    /// Spawns aggressive enemies that will chase the player.
    /// </summary>
    public void SpawnEnemy()
    {
        if (enemyPrefab == null)
        {
            Debug.LogWarning("EnemySpawner: enemyPrefab is not assigned.");
            return;
        }

        Transform point = spawnPoint != null ? spawnPoint : transform;

        for (int i = 0; i < spawnCount; i++)
        {
            // Small random offset so multiple enemies don't stack
            Vector3 offset = Random.insideUnitSphere;
            offset.y = 0f;
            offset *= 1.5f;

            EnemyWithSM enemy = Instantiate(
                enemyPrefab,
                point.position + offset,
                point.rotation
            );

            if (enemy != null)
            {
                // Ensure the enemy knows about the player
                GameObject player = GameObject.FindGameObjectWithTag("Player");
                if (player != null)
                {
                    enemy.SetPlayerReference(player);
                }

                // Make enemy extremely alert/aggressive
                enemy.sightDistance = aggressiveSightDistance;
                enemy.fieldOfView = aggressiveFieldOfView;

                // Optionally, immediately alert to player (reuses existing logic)
                enemy.AlertToPlayer();
            }
        }
    }

    /// <summary>
    /// Public method that simply destroys the GameObject passed in.
    /// Use this with EventOnlyInteractable by assigning the target
    /// GameObject in the UnityEvent and selecting this method.
    /// </summary>
    /// <param name="target">The GameObject to destroy.</param>
    public void DestroyAssignedObject(GameObject target)
    {
        if (target == null)
        {
            Debug.LogWarning("EnemySpawner.DestroyAssignedObject called with null target.");
            return;
        }

        Destroy(target);
    }
}


