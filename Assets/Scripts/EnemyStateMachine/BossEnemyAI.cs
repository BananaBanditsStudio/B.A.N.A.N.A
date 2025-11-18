using UnityEngine;

public class BossEnemyAI : MonoBehaviour
{
    [Header("Phase 1 - Normal")]
    public AttackBehaviorType normalPhaseAttack = AttackBehaviorType.Melee;
    
    [Header("Phase 2 - Enraged")]
    public float enragedHealthThreshold = 0.5f;
    public AttackBehaviorType[] enragedPhaseAttacks = new AttackBehaviorType[] { 
        AttackBehaviorType.BigJump, 
        AttackBehaviorType.BigMelee 
    };
    public float enragedSpeedMultiplier = 1.5f;
    
    [Header("Enraged Visual Effects")]
    [Tooltip("Enable outline effect when enraged")]
    public bool useEnragedOutline = true;
    [Tooltip("Color of the outline (red/orange for enraged effect)")]
    public Color outlineColor = new Color(1f, 0.3f, 0f, 1f); // Orange-red
    [Tooltip("Width/thickness of the outline")]
    public float outlineWidth = 0.1f;
    [Tooltip("Optional: Particle system prefab for fire/glow effect")]
    public GameObject enragedParticleEffect;
    
    private EnemyWithSM enemyWithSM;
    private EnemyDamage enemyDamage;
    private bool isEnraged = false;
    private float originalChaseSpeed;
    private float originalPatrolSpeed;
    private int lastEnragedAttackIndex = -1;
    private bool wasInAttackAnimation = false; // Track previous frame's attack animation state
    private int[] attackAnimationHashes; // Cache animation hashes for performance
    
    // Visual effect components
    private Renderer[] renderers; // All renderers on the boss
    private GameObject[] outlineObjects; // Outline renderer objects
    private GameObject particleEffectInstance; // Particle effect instance
    
    private void Start()
    {
        enemyWithSM = GetComponent<EnemyWithSM>();
        enemyDamage = GetComponent<EnemyDamage>();
        
        // Cache animation hashes for better performance (avoids string comparisons)
        attackAnimationHashes = new int[]
        {
            Animator.StringToHash("BigJump"),
            Animator.StringToHash("BigMelee"),
            Animator.StringToHash("Melee")
        };
        
        // Get all renderers for visual effects
        renderers = GetComponentsInChildren<Renderer>();
        outlineObjects = new GameObject[renderers != null ? renderers.Length : 0];
        
        if (enemyWithSM != null)
        {
            originalChaseSpeed = enemyWithSM.chaseSpeed;
            originalPatrolSpeed = enemyWithSM.patrolSpeed;
            enemyWithSM.attackBehaviorType = normalPhaseAttack;
        }
    }
    
    private void Update()
    {
        if (!isEnraged)
        {
            CheckEnragedPhase();
        }
        else
        {
            // While enraged, periodically check if we should swap to the next attack
            // This allows the boss to cycle through different attack types
            CheckAndSwapEnragedAttack();
        }
    }
    
    
    public AttackBehaviorType GetNextEnragedAttack()
    {
        if (enragedPhaseAttacks.Length == 0)
            return AttackBehaviorType.Melee;
        
        if (enragedPhaseAttacks.Length == 1)
            return enragedPhaseAttacks[0];
        
        int newIndex;
        do
        {
            newIndex = Random.Range(0, enragedPhaseAttacks.Length);
        } while (newIndex == lastEnragedAttackIndex && enragedPhaseAttacks.Length > 1);
        
        lastEnragedAttackIndex = newIndex;
        return enragedPhaseAttacks[newIndex];
    }
    
    private void CheckEnragedPhase()
    {
        if (enemyDamage == null) return;
        
        float healthPercent = enemyDamage.health / enemyDamage.maxHealth;
        
        if (healthPercent <= enragedHealthThreshold)
        {
            EnterEnragedPhase();
        }
    }
    
    private void EnterEnragedPhase()
    {
        isEnraged = true;
        lastEnragedAttackIndex = -1;
        
        if (enemyWithSM != null)
        {
            enemyWithSM.chaseSpeed = originalChaseSpeed * enragedSpeedMultiplier;
            enemyWithSM.patrolSpeed = originalPatrolSpeed * enragedSpeedMultiplier;
            
            if (enemyWithSM.Agent != null)
            {
                enemyWithSM.Agent.speed = enemyWithSM.chaseSpeed;
            }
            
            enemyWithSM.attackBehaviorType = GetNextEnragedAttack();
        }
        
        // Apply visual enraged effects
        ApplyEnragedVisuals();
    }
    
    /// <summary>
    /// Applies visual effects to make the boss look enraged (outline effect, particles, etc.)
    /// </summary>
    private void ApplyEnragedVisuals()
    {
        if (!useEnragedOutline) return;
        
        // Create outline effect using scaled duplicate meshes
        if (renderers != null && outlineObjects != null)
        {
            for (int i = 0; i < renderers.Length && i < outlineObjects.Length; i++)
            {
                if (renderers[i] == null) continue;
                
                // Skip if outline already exists
                if (outlineObjects[i] != null) continue;
                
                // Get mesh filter and mesh renderer
                MeshFilter meshFilter = renderers[i].GetComponent<MeshFilter>();
                SkinnedMeshRenderer skinnedRenderer = renderers[i] as SkinnedMeshRenderer;
                
                if (meshFilter != null && meshFilter.sharedMesh != null)
                {
                    // Create outline object for regular mesh
                    GameObject outlineObj = new GameObject(renderers[i].name + "_Outline");
                    outlineObj.transform.SetParent(renderers[i].transform, false);
                    outlineObj.transform.localPosition = Vector3.zero;
                    outlineObj.transform.localRotation = Quaternion.identity;
                    outlineObj.transform.localScale = Vector3.one * (1f + outlineWidth);
                    
                    // Copy mesh filter
                    MeshFilter outlineMeshFilter = outlineObj.AddComponent<MeshFilter>();
                    outlineMeshFilter.sharedMesh = meshFilter.sharedMesh;
                    
                    // Add renderer with outline material
                    MeshRenderer outlineRenderer = outlineObj.AddComponent<MeshRenderer>();
                    
                    // Create simple unlit outline material
                    Material outlineMat = new Material(Shader.Find("Unlit/Color"));
                    if (outlineMat.shader.name == "Hidden/InternalErrorShader")
                    {
                        // Fallback to standard shader if unlit not found
                        outlineMat = new Material(Shader.Find("Standard"));
                        outlineMat.SetFloat("_Metallic", 0f);
                        outlineMat.SetFloat("_Glossiness", 0f);
                    }
                    outlineMat.color = outlineColor;
                    outlineRenderer.material = outlineMat;
                    outlineRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                    outlineRenderer.receiveShadows = false;
                    
                    // Set render queue to render before main mesh (creates outline effect)
                    outlineMat.renderQueue = 3000; // Render before geometry
                    
                    outlineObjects[i] = outlineObj;
                }
                else if (skinnedRenderer != null && skinnedRenderer.sharedMesh != null)
                {
                    // Create outline object for skinned mesh
                    GameObject outlineObj = new GameObject(renderers[i].name + "_Outline");
                    outlineObj.transform.SetParent(renderers[i].transform, false);
                    outlineObj.transform.localPosition = Vector3.zero;
                    outlineObj.transform.localRotation = Quaternion.identity;
                    outlineObj.transform.localScale = Vector3.one * (1f + outlineWidth);
                    
                    // Copy skinned mesh renderer
                    SkinnedMeshRenderer outlineSkinned = outlineObj.AddComponent<SkinnedMeshRenderer>();
                    outlineSkinned.sharedMesh = skinnedRenderer.sharedMesh;
                    outlineSkinned.bones = skinnedRenderer.bones;
                    outlineSkinned.rootBone = skinnedRenderer.rootBone;
                    
                    // Create simple unlit outline material
                    Material outlineMat = new Material(Shader.Find("Unlit/Color"));
                    if (outlineMat.shader.name == "Hidden/InternalErrorShader")
                    {
                        // Fallback to standard shader if unlit not found
                        outlineMat = new Material(Shader.Find("Standard"));
                        outlineMat.SetFloat("_Metallic", 0f);
                        outlineMat.SetFloat("_Glossiness", 0f);
                    }
                    outlineMat.color = outlineColor;
                    outlineSkinned.material = outlineMat;
                    outlineSkinned.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                    outlineSkinned.receiveShadows = false;
                    
                    outlineObjects[i] = outlineObj;
                }
            }
        }
        
        // Spawn particle effect if provided
        if (enragedParticleEffect != null && particleEffectInstance == null)
        {
            particleEffectInstance = Instantiate(enragedParticleEffect, transform);
            particleEffectInstance.transform.localPosition = Vector3.zero;
        }
    }
    
    /// <summary>
    /// Removes enraged visual effects (if needed for cleanup)
    /// </summary>
    private void RemoveEnragedVisuals()
    {
        // Remove outline objects
        if (outlineObjects != null)
        {
            for (int i = 0; i < outlineObjects.Length; i++)
            {
                if (outlineObjects[i] != null)
                {
                    Destroy(outlineObjects[i]);
                    outlineObjects[i] = null;
                }
            }
        }
        
        // Remove particle effect
        if (particleEffectInstance != null)
        {
            Destroy(particleEffectInstance);
            particleEffectInstance = null;
        }
    }
    
    private void OnDestroy()
    {
        // Cleanup outline objects
        RemoveEnragedVisuals();
    }
    
    public bool IsEnraged()
    {
        return isEnraged;
    }
    
    /// <summary>
    /// Checks if the boss should swap to the next enraged attack.
    /// Swaps after each attack completes (detects transition from attack animation to idle/movement).
    /// </summary>
    private void CheckAndSwapEnragedAttack()
    {
        if (enemyWithSM == null || enragedPhaseAttacks.Length <= 1) return;
        
        // Check if enemy is in AttackState
        if (enemyWithSM.StateMachine != null && 
            enemyWithSM.StateMachine.activeState is AttackState)
        {
            if (enemyWithSM.Animator != null)
            {
                AnimatorStateInfo stateInfo = enemyWithSM.Animator.GetCurrentAnimatorStateInfo(0);
                int currentStateHash = stateInfo.shortNameHash;
                // Use cached hashes instead of string comparisons (much faster)
                bool isInAttackAnimation = currentStateHash == attackAnimationHashes[0] || 
                                          currentStateHash == attackAnimationHashes[1] || 
                                          currentStateHash == attackAnimationHashes[2];
                
                // Detect transition from attack animation to non-attack (attack just completed)
                if (wasInAttackAnimation && !isInAttackAnimation)
                {
                    // Attack just completed, swap to next attack type
                    AttackBehaviorType currentType = enemyWithSM.attackBehaviorType;
                    // Use Array.Exists instead of IndexOf for better performance
                    bool isCurrentEnragedAttack = System.Array.Exists(enragedPhaseAttacks, x => x == currentType);
                    
                    if (isCurrentEnragedAttack)
                    {
                        AttackBehaviorType nextAttack = GetNextEnragedAttack();
                        if (nextAttack != currentType)
                        {
                            enemyWithSM.attackBehaviorType = nextAttack;
                            Debug.Log($"BossEnemyAI: Attack completed, swapped to next enraged attack: {nextAttack}");
                        }
                    }
                }
                
                // Update tracking for next frame
                wasInAttackAnimation = isInAttackAnimation;
            }
        }
        else
        {
            // Not in attack state, reset tracking
            wasInAttackAnimation = false;
        }
    }
}

