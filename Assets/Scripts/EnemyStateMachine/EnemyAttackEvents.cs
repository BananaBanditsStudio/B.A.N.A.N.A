using UnityEngine;

public class EnemyAttackEvents : MonoBehaviour
{
    // Cached references
    private EnemyWithSM enemy;
    private StateMachine stateMachine;
    private bool refsCached = false;

    // Ranged attack cycle
    private int currentThrowCycle = 0;
    private int lastFiredCycle = -1;
    private float lastFireTime = -1f;
    private const float FIRE_COOLDOWN = 0.05f;

    void Awake()
    {
        CacheRefs();
    }

    void Start()
    {
        if (!refsCached)
            CacheRefs();
    }

    private void CacheRefs()
    {
        enemy = GetComponentInParent<EnemyWithSM>();

        if (enemy != null)
            stateMachine = enemy.GetComponent<StateMachine>();

        refsCached = true;
    }

    // -------------------------------------------------------------------
    // 🔥 RANGED ATTACK (Throw)
    // -------------------------------------------------------------------

    // Called at the START of the animation cycle
    public void AnimEvent_ThrowEnter()
    {
        currentThrowCycle++;
    }

    // Called exactly at the projectile release frame
    public void AnimEvent_ThrowRelease()
    {
        if (!refsCached || stateMachine == null || stateMachine.activeState == null)
            CacheRefs();

        if (stateMachine == null || stateMachine.activeState == null)
        {
            Debug.LogError("EnemyAttackEvents: Missing state machine.");
            return;
        }

        if (stateMachine.activeState is AttackState attackState)
        {
            float t = Time.time;

            // Prevent double-firing
            if (t - lastFireTime < FIRE_COOLDOWN && lastFiredCycle == currentThrowCycle)
                return;

            attackState.Shoot(); // FIRE PROJECTILE

            lastFireTime = t;
            lastFiredCycle = currentThrowCycle;
        }
    }

    // -------------------------------------------------------------------
    // 🔊 SOUND EVENTS (Forwarded to EnemyWithSM)
    // -------------------------------------------------------------------

    public void AnimEvent_PlayMeleeAttackSound()
    {
        enemy?.PlayMeleeAttackSound();
    }

    public void AnimEvent_PlayRangedAttackSound()
    {
        enemy?.PlayRangedAttackSound();
    }

    public void AnimEvent_PlayChargeAttackSound()
    {
        enemy?.PlayChargeAttackSound();
    }

    public void AnimEvent_PlayBigJumpAttackSound()
    {
        enemy?.PlayBigJumpAttackSound();
    }

    public void AnimEvent_PlayBigMeleeAttackSound()
    {
        enemy?.PlayBigMeleeAttackSound();
    }
    // Called by ThrowAttackBehavior to begin a new throw cycle
    public void StartNewThrowCycle()
    {
        currentThrowCycle++;
    }


    // -------------------------------------------------------------------
    // ⚡ DAMAGE & SPECIAL ATTACK EVENTS (Optional Expansion)
    // -------------------------------------------------------------------

    // If later you want to sync damage, explosion triggers, etc.
    // Just add forwarder calls here:
    //
    // public void AnimEvent_ChargeExplode()
    // {
    //     enemy?.ExplodeChargeAttack();
    // }
    //
    // public void AnimEvent_MeleeApplyDamage()
    // {
    //     enemy?.ApplyMeleeDamageNow();
    // }

    // This architecture now supports ANY additional attack events cleanly.
}
