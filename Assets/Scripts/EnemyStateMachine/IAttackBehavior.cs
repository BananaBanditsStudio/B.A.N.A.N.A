/// <summary>
/// Interface for different attack behaviors.
/// Allows swapping attack logic without changing the AttackState.
/// </summary>
public interface IAttackBehavior
{
    /// <summary>
    /// Called when entering the attack state
    /// </summary>
    void OnEnter(EnemyWithSM enemy);
    
    /// <summary>
    /// Called when exiting the attack state
    /// </summary>
    void OnExit(EnemyWithSM enemy);
    
    /// <summary>
    /// Called every frame while in attack state
    /// </summary>
    void OnPerform(EnemyWithSM enemy, float deltaTime);
    
    /// <summary>
    /// Checks if the enemy can attack right now
    /// </summary>
    bool CanAttack(EnemyWithSM enemy);
    
    /// <summary>
    /// Executes the attack (called when it's time to attack)
    /// </summary>
    void Attack(EnemyWithSM enemy);
    
    /// <summary>
    /// Updates movement behavior while attacking
    /// </summary>
    void UpdateMovement(EnemyWithSM enemy, float deltaTime);
}

/// <summary>
/// Factory class to create attack behaviors based on type.
/// This allows Unity to serialize the behavior type in the Inspector.
/// </summary>
public static class AttackBehaviorFactory
{
    public static IAttackBehavior Create(AttackBehaviorType type)
    {
        switch (type)
        {
            case AttackBehaviorType.Throw:
                return new ThrowAttackBehavior();
            // Add more attack behavior types here as you create them
            case AttackBehaviorType.Melee:
                return new MeleeAttackBehavior();
            case AttackBehaviorType.Charge:
                return new ChargeAttackBehavior();
            default:
                return new ThrowAttackBehavior();
        }
    }
}

/// <summary>
/// Enum defining available attack behavior types.
/// Add new types here as you create new attack behaviors.
/// </summary>
public enum AttackBehaviorType
{
    Throw,
    Melee,
    Charge,
    // Ranged,
}

