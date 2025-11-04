using UnityEngine;

public class EnemyAttackEvents : MonoBehaviour
{
    // Cached references for performance - avoid GetComponent calls in hot path
    private EnemyWithSM cachedEnemy;
    private StateMachine cachedStateMachine;
    private bool referencesCached = false;
    
    // Track which throw cycle we're on to prevent cross-cycle firing
    private int currentThrowCycle = 0;
    private int lastFiredCycle = -1;
    
    // Reduced cooldown for faster response
    private float lastFireTime = -1f;
    private const float FIRE_COOLDOWN = 0.05f; // Reduced from 0.1s to 0.05s

    void Awake()
    {
        CacheReferences();
    }

    void Start()
    {
        // Ensure references are cached
        if (!referencesCached)
        {
            CacheReferences();
        }
    }

    private void CacheReferences()
    {
        cachedEnemy = GetComponentInParent<EnemyWithSM>();
        if (cachedEnemy != null)
        {
            cachedStateMachine = cachedEnemy.GetComponent<StateMachine>();
        }
        referencesCached = true;
    }

    // Called by the animation event on the "release" frame
    // OPTIMIZED: Minimal checks, cached references, fast path
    public void AnimEvent_ThrowRelease()
    {
        // CRITICAL: Fire immediately when animation event is called - no blocking checks!
        // The animation event should fire exactly at the release frame
        
        // Ensure references are cached (fallback)
        if (!referencesCached || cachedStateMachine == null || cachedStateMachine.activeState == null)
        {
            CacheReferences();
            if (cachedStateMachine == null || cachedStateMachine.activeState == null)
            {
                Debug.LogError("EnemyAttackEvents: Cannot fire - missing references!");
                return;
            }
        }
        
        // Fast path: Direct cast and fire immediately
        if (cachedStateMachine.activeState is AttackState attackState)
        {
            // Check for duplicate only AFTER we know we can fire
            float currentTime = Time.time;
            if (currentTime - lastFireTime < FIRE_COOLDOWN && lastFiredCycle == currentThrowCycle)
            {
                Debug.LogWarning($"EnemyAttackEvents: Duplicate fire prevented! Cycle: {currentThrowCycle}, Time since last: {currentTime - lastFireTime:F3}s");
                return; // Already fired for this cycle
            }
            
            // FIRE IMMEDIATELY - no more checks!
            attackState.Shoot();
            lastFireTime = currentTime;
            lastFiredCycle = currentThrowCycle;
            
            Debug.Log($"EnemyAttackEvents: FIRED at {currentTime:F3}s, Cycle: {currentThrowCycle}");
        }
        else
        {
            Debug.LogWarning($"EnemyAttackEvents: Cannot fire - not in AttackState! Current state: {cachedStateMachine.activeState?.GetType().Name}");
        }
    }

    // Reset when entering Throw so the event can fire again next time
    public void AnimEvent_ThrowEnter()
    {
        currentThrowCycle++;
    }
    
    // Public method to start a new throw cycle (called from AttackState when triggering)
    public void StartNewThrowCycle()
    {
        currentThrowCycle++;
    }
}
