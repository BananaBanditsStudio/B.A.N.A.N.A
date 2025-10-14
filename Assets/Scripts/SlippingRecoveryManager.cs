using UnityEngine;

public class SlippingRecoveryManager : MonoBehaviour
{
    public static SlippingRecoveryManager Instance { get; private set; }
    
    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    // Force recovery of all slipping enemies in the scene
    public void ForceRecoverAllSlippingEnemies()
    {
        Debug.Log("Forcing recovery of all slipping enemies");
        
        // Find all enemies with EnemyDamage component
        EnemyDamage[] allEnemies = FindObjectsByType<EnemyDamage>(FindObjectsSortMode.None);
        
        foreach (EnemyDamage enemy in allEnemies)
        {
            if (enemy.IsSlipping() && enemy.health > 0)
            {
                Debug.Log($"Forcing recovery for enemy: {enemy.name}");
                enemy.ForceSlippingRecovery();
            }
        }
    }
    
    // Check if any enemies are currently slipping
    public bool HasSlippingEnemies()
    {
        EnemyDamage[] allEnemies = FindObjectsByType<EnemyDamage>(FindObjectsSortMode.None);
        
        foreach (EnemyDamage enemy in allEnemies)
        {
            if (enemy.IsSlipping() && enemy.health > 0)
            {
                return true;
            }
        }
        
        return false;
    }
    
    // Get count of slipping enemies
    public int GetSlippingEnemyCount()
    {
        int count = 0;
        EnemyDamage[] allEnemies = FindObjectsByType<EnemyDamage>(FindObjectsSortMode.None);
        
        foreach (EnemyDamage enemy in allEnemies)
        {
            if (enemy.IsSlipping() && enemy.health > 0)
            {
                count++;
            }
        }
        
        return count;
    }
}
