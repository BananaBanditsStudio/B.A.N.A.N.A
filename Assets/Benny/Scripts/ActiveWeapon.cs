using UnityEngine;
using UnityEditor.Animations;
using UnityEngine.Animations.Rigging;

public class ActiveWeapon : MonoBehaviour
{
    public Transform crossHairTarget;
    
    [SerializeField] private Transform handIk;
    [SerializeField] private Transform weaponParent;
    [SerializeField] private Transform weaponLeftGrip;
    [SerializeField] private Transform weaponRightGrip;
    
    [Header("Alternative: Direct GameObject Control")]
    [SerializeField] private GameObject handIkGameObject;
    
    [Header("Animation")]
    [SerializeField] private AnimationClip weaponAnimClip;
    [SerializeField] private string weaponAnimStateName = "Empty_anim";

    private RaycastWeapon weapon;
    private RaycastWeapon availableWeapon;
    private Animator animator;
    private AnimatorOverrideController animatorOverrideController;
    private AnimationClip defaultWeaponClip;
    private bool isWeaponEquipped = false;
    private IRigConstraint handIkConstraint;
    private IRigConstraint[] allHandIkConstraints; // Store ALL IK constraints under Hand_IK
    private RigBuilder rigBuilder;

    // Start is called before the first frame update
    void Start()
    {
        animator = GetComponent<Animator>();
        animatorOverrideController = new AnimatorOverrideController(animator.runtimeAnimatorController);
        animator.runtimeAnimatorController = animatorOverrideController;
        
        // Find the clip to override
        FindWeaponAnimationClip();
        
        // Get RigBuilder component
        rigBuilder = GetComponent<RigBuilder>();
        if (rigBuilder == null)
        {
            Debug.LogWarning("No RigBuilder found on this GameObject!");
        }
        
        // Get ALL hand IK constraints under Hand_IK
        if (handIk != null)
        {
            // Find ALL IK constraints in children (this will get both left and right hand IKs)
            allHandIkConstraints = handIk.GetComponentsInChildren<IRigConstraint>(true);
            
            if (allHandIkConstraints != null && allHandIkConstraints.Length > 0)
            {
                Debug.Log($"✓ Found {allHandIkConstraints.Length} Hand IK constraint(s) under '{handIk.name}':");
                foreach (var constraint in allHandIkConstraints)
                {
                    Debug.Log($"  - {constraint.GetType().Name} on {((Component)constraint).gameObject.name} (current weight: {constraint.weight})");
                }
                
                // Store the first one for legacy compatibility
                handIkConstraint = allHandIkConstraints[0];
            }
            else
            {
                Debug.LogWarning($"⚠ No IK constraints found under handIk '{handIk.name}'");
                Debug.LogWarning("Will use GameObject enable/disable method instead.");
            }
        }
        else
        {
            Debug.LogError("❌ handIk transform is not assigned in Inspector!");
        }
        
        // Find weapon but don't equip it yet
        availableWeapon = GetComponentInChildren<RaycastWeapon>();
        
        // Start with weapon unequipped
        if (availableWeapon != null)
        {
            availableWeapon.gameObject.SetActive(false);
        }
        
        // Ensure hand IKs are disabled at start
        if (animator != null)
        {
            animator.SetLayerWeight(1, 0.0f);
            animator.SetBool("1_pressed", false);
        }
        
        // Set hand IK weight to 0 at start
        Debug.Log("=== Setting initial Hand IK weight to 0 ===");
        SetHandIkWeight(0f);
        
        // Verify the weight was set
        if (handIkConstraint != null)
        {
            Debug.Log($"Verified Hand IK weight after setting: {handIkConstraint.weight}");
        }
        
        Debug.Log("Started with weapon unequipped. Hand IK disabled. Press '1' to equip/unequip weapon.");
    }
    
    private void FindWeaponAnimationClip()
    {
        var overrides = new System.Collections.Generic.List<System.Collections.Generic.KeyValuePair<AnimationClip, AnimationClip>>();
        animatorOverrideController.GetOverrides(overrides);
        
        Debug.Log($"=== Finding Animation Clips (Total: {overrides.Count}) ===");
        
        for (int i = 0; i < overrides.Count; i++)
        {
            var pair = overrides[i];
            if (pair.Key != null)
            {
                Debug.Log($"[{i}] Key: {pair.Key.name}, Current Value: {(pair.Value != null ? pair.Value.name : "NULL")}");
                
                // Find the clip that matches our weapon animation state name
                if (pair.Key.name == weaponAnimStateName)
                {
                    defaultWeaponClip = pair.Key;
                    Debug.Log($">>> MATCH FOUND! Using clip: {defaultWeaponClip.name}");
                }
            }
            else
            {
                Debug.LogWarning($"[{i}] Key is NULL!");
            }
        }
        
        if (defaultWeaponClip == null && overrides.Count > 0)
        {
            // If not found by name, use the first clip as fallback
            defaultWeaponClip = overrides[0].Key;
            Debug.LogWarning($"Could not find clip named '{weaponAnimStateName}', using first available: {defaultWeaponClip.name}");
        }
        else if (defaultWeaponClip != null)
        {
            Debug.Log($"✓ Successfully found weapon animation clip: {defaultWeaponClip.name}");
        }
        else
        {
            Debug.LogError("❌ No animation clips found in the Animator Controller!");
        }
    }

    // Update is called once per frame
    void Update()
    {
        // Toggle weapon equip/unequip when "1" key is pressed
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            ToggleWeapon();
        }
        
        if (weapon && isWeaponEquipped)
        {
            if (Input.GetButtonDown("Fire1"))
            {
                weapon.StartFiring();
            }
            if (weapon.isFiring)
            {
                weapon.UpdateFiring(Time.deltaTime);
            }
            weapon.UpdateBullets(Time.deltaTime);
            if (Input.GetButtonUp("Fire1"))
            {
                weapon.StopFiring();
            }
            
            HandleWeaponAiming();
        }
    }
    
    private void ToggleWeapon()
    {
        if (availableWeapon == null)
        {
            Debug.LogWarning("No weapon available to equip!");
            return;
        }
        
        isWeaponEquipped = !isWeaponEquipped;
        
        if (isWeaponEquipped)
        {
            // Equip weapon
            Equip(availableWeapon);
            availableWeapon.gameObject.SetActive(true);
            SetHandIkWeight(1f);
            Debug.Log("✓ Weapon EQUIPPED");
            Debug.Log("✓ Hand IK weight set to 1");
        }
        else
        {
            // Unequip weapon
            Unequip();
            availableWeapon.gameObject.SetActive(false);
            SetHandIkWeight(0f);
            Debug.Log("✓ Weapon UNEQUIPPED");
            Debug.Log("✓ Hand IK weight set to 0");
        }
    }
    
    private void SetHandIkWeight(float weight)
    {
        bool success = false;
        
        // Method 1: Set weight on ALL IK constraints found (both left and right hands)
        if (allHandIkConstraints != null && allHandIkConstraints.Length > 0)
        {
            Debug.Log($"Setting weight to {weight} on {allHandIkConstraints.Length} Hand IK constraint(s):");
            foreach (var constraint in allHandIkConstraints)
            {
                if (constraint != null)
                {
                    constraint.weight = weight;
                    Debug.Log($"  ✓ {((Component)constraint).gameObject.name}: {constraint.GetType().Name} weight = {weight}");
                }
            }
            
            // Force the rig to rebuild/update
            if (rigBuilder != null)
            {
                rigBuilder.Build();
                Debug.Log("✓ RigBuilder rebuilt");
            }
            success = true;
        }
        
        // Method 2: If no constraints, try enabling/disabling the GameObject directly
        if (!success && handIkGameObject != null)
        {
            bool shouldBeActive = weight > 0.5f; // If weight > 0.5, enable; otherwise disable
            handIkGameObject.SetActive(shouldBeActive);
            Debug.Log($"Setting Hand IK GameObject active state to: {shouldBeActive}");
            success = true;
        }
        
        // Method 3: Try enabling/disabling the handIk transform's GameObject
        if (!success && handIk != null)
        {
            bool shouldBeActive = weight > 0.5f;
            handIk.gameObject.SetActive(shouldBeActive);
            Debug.Log($"Setting handIk GameObject '{handIk.name}' active state to: {shouldBeActive}");
            success = true;
        }
        
        if (!success)
        {
            Debug.LogWarning("Cannot set Hand IK weight - no valid IK constraint or GameObject found!");
            Debug.LogWarning("Please assign either:");
            Debug.LogWarning("  1. A GameObject with IK constraints (Hand_IK) to 'handIk'");
            Debug.LogWarning("  2. The Hand IK GameObject to 'handIkGameObject'");
        }
    }

    private void HandleWeaponAiming()
    {
        if (crossHairTarget != null && weaponParent != null)
        {
            // Rotate weapon parent to look at crosshair target
            Vector3 direction = (crossHairTarget.position - weaponParent.position).normalized;
            Quaternion lookRotation = Quaternion.LookRotation(direction);
            weaponParent.rotation = lookRotation;
        }
    }

    private void OnAnimatorIK(int layerIndex)
    {
        if (animator == null)
            animator = GetComponent<Animator>();
        
        if (animator == null) return;

        // Only apply IK when weapon is equipped
        if (isWeaponEquipped && weapon != null)
        {
            // Set left hand IK to weapon grip position
            if (weaponLeftGrip != null)
            {
                animator.SetIKPositionWeight(AvatarIKGoal.LeftHand, 1f);
                animator.SetIKRotationWeight(AvatarIKGoal.LeftHand, 1f);
                animator.SetIKPosition(AvatarIKGoal.LeftHand, weaponLeftGrip.position);
                animator.SetIKRotation(AvatarIKGoal.LeftHand, weaponLeftGrip.rotation);
            }

            // Set right hand IK to weapon grip position
            if (weaponRightGrip != null)
            {
                animator.SetIKPositionWeight(AvatarIKGoal.RightHand, 1f);
                animator.SetIKRotationWeight(AvatarIKGoal.RightHand, 1f);
                animator.SetIKPosition(AvatarIKGoal.RightHand, weaponRightGrip.position);
                animator.SetIKRotation(AvatarIKGoal.RightHand, weaponRightGrip.rotation);
            }
        }
        else
        {
            // Disable IK when weapon is not equipped
            animator.SetIKPositionWeight(AvatarIKGoal.LeftHand, 0f);
            animator.SetIKRotationWeight(AvatarIKGoal.LeftHand, 0f);
            animator.SetIKPositionWeight(AvatarIKGoal.RightHand, 0f);
            animator.SetIKRotationWeight(AvatarIKGoal.RightHand, 0f);
        }
    }

    public void Equip(RaycastWeapon newWeapon)
    {
        weapon = newWeapon;
        weapon.raycastDestination = crossHairTarget;
        
        Debug.Log("=== EQUIPPING WEAPON ===");
        Debug.Log($"Weapon: {newWeapon.name}");
        Debug.Log($"Weapon AnimationClip: {(newWeapon.AnimationClip != null ? newWeapon.AnimationClip.name : "NULL")}");
        Debug.Log($"Override Controller: {(animatorOverrideController != null ? "OK" : "NULL")}");
        Debug.Log($"Default Weapon Clip: {(defaultWeaponClip != null ? defaultWeaponClip.name : "NULL")}");
        
        // Set animation overrides if weapon has animation clip
        if (newWeapon.AnimationClip != null && animatorOverrideController != null && defaultWeaponClip != null)
        {
            // Apply the override
            animatorOverrideController[defaultWeaponClip] = newWeapon.AnimationClip;
            
            // Force animator to update
            animator.runtimeAnimatorController = animatorOverrideController;
            animator.Rebind();
            animator.Update(0f);
            
            // Enable weapon layer and animation
            animator.SetLayerWeight(1, 1.0f);
            animator.SetBool("1_pressed", true);
            
            Debug.Log($"✓ Animation override applied: {defaultWeaponClip.name} -> {newWeapon.AnimationClip.name}");
            Debug.Log($"✓ Layer 1 weight: {animator.GetLayerWeight(1)}");
            Debug.Log($"✓ 1_pressed: true");
            Debug.Log($"✓ Animator rebound and updated");
            
            // Verify the override was applied
            var currentOverride = animatorOverrideController[defaultWeaponClip];
            Debug.Log($"✓ Verification - Current override: {(currentOverride != null ? currentOverride.name : "NULL")}");
        }
        else
        {
            Debug.LogError("=== EQUIP FAILED ===");
            if (defaultWeaponClip == null)
                Debug.LogError("❌ defaultWeaponClip is null!");
            if (newWeapon.AnimationClip == null)
                Debug.LogError("❌ Weapon AnimationClip is null!");
            if (animatorOverrideController == null)
                Debug.LogError("❌ animatorOverrideController is null!");
        }
    }
    
    public void Unequip()
    {
        Debug.Log("=== UNEQUIPPING WEAPON ===");
        
        // Disable weapon layer and animation
        if (animator != null)
        {
            animator.SetLayerWeight(1, 0.0f);
            animator.SetBool("1_pressed", false);
            Debug.Log($"✓ Layer 1 weight: 0");
            Debug.Log($"✓ 1_pressed: false");
        }
        
        weapon = null;
        Debug.Log("✓ Weapon reference cleared");
    }

    // Public methods for external control
    public void SetCrossHairTarget(Transform target)
    {
        crossHairTarget = target;
    }

    [ContextMenu("Save Weapon Pose")]
    public void SaveWeaponPose()
    {
        GameObjectRecorder recorder = new GameObjectRecorder(gameObject);
        
        if (weaponParent != null)
            recorder.BindComponentsOfType<Transform>(weaponParent.gameObject, false);
        
        if (weaponLeftGrip != null)
            recorder.BindComponentsOfType<Transform>(weaponLeftGrip.gameObject, false);
        
        if (weaponRightGrip != null)
            recorder.BindComponentsOfType<Transform>(weaponRightGrip.gameObject, false);
        
        recorder.TakeSnapshot(0.0f);
        recorder.SaveToClip(weaponAnimClip);
        
        Debug.Log("Weapon pose saved to clip!");
    }
    
    [ContextMenu("Debug: List All Animation Clips")]
    public void DebugListAnimationClips()
    {
        if (animatorOverrideController == null)
        {
            Debug.LogError("AnimatorOverrideController is null!");
            return;
        }
        
        var overrides = new System.Collections.Generic.List<System.Collections.Generic.KeyValuePair<AnimationClip, AnimationClip>>();
        animatorOverrideController.GetOverrides(overrides);
        
        Debug.Log("=== All Available Animation Clips ===");
        for (int i = 0; i < overrides.Count; i++)
        {
            var pair = overrides[i];
            if (pair.Key != null)
            {
                Debug.Log($"{i}: {pair.Key.name} (Current: {(pair.Value != null ? pair.Value.name : "None")})");
            }
        }
        
        if (defaultWeaponClip != null)
        {
            Debug.Log($"\nCurrently using clip: {defaultWeaponClip.name}");
        }
        else
        {
            Debug.LogWarning("\nNo default weapon clip is set!");
        }
    }
    
    [ContextMenu("Debug: Test Animation Override")]
    public void DebugTestOverride()
    {
        Debug.Log("=== MANUAL OVERRIDE TEST ===");
        
        if (weapon != null && weapon.AnimationClip != null)
        {
            Debug.Log($"Weapon found: {weapon.name}");
            Debug.Log($"Weapon clip: {weapon.AnimationClip.name}");
            
            if (defaultWeaponClip != null)
            {
                Debug.Log($"Attempting to override {defaultWeaponClip.name} with {weapon.AnimationClip.name}");
                animatorOverrideController[defaultWeaponClip] = weapon.AnimationClip;
                
                // Force update
                animator.runtimeAnimatorController = animatorOverrideController;
                animator.Rebind();
                
                Debug.Log("✓ Override applied and animator rebound");
            }
            else
            {
                Debug.LogError("❌ defaultWeaponClip is null!");
            }
        }
        else
        {
            Debug.LogError("❌ No weapon equipped or weapon has no AnimationClip!");
            if (weapon == null) Debug.LogError("weapon is null");
            if (weapon != null && weapon.AnimationClip == null) Debug.LogError("weapon.AnimationClip is null");
        }
    }
    
    [ContextMenu("Debug: Set Hand IK Weight to 0")]
    public void DebugSetHandIkToZero()
    {
        Debug.Log("=== MANUALLY SETTING HAND IK TO 0 ===");
        SetHandIkWeight(0f);
        if (handIkConstraint != null)
        {
            Debug.Log($"Current weight after setting: {handIkConstraint.weight}");
        }
    }
    
    [ContextMenu("Debug: Set Hand IK Weight to 1")]
    public void DebugSetHandIkToOne()
    {
        Debug.Log("=== MANUALLY SETTING HAND IK TO 1 ===");
        SetHandIkWeight(1f);
        if (handIkConstraint != null)
        {
            Debug.Log($"Current weight after setting: {handIkConstraint.weight}");
        }
    }
    
    [ContextMenu("Debug: Check Hand IK Status")]
    public void DebugCheckHandIkStatus()
    {
        Debug.Log("=== HAND IK STATUS ===");
        Debug.Log($"handIk Transform: {(handIk != null ? handIk.name : "NULL")}");
        Debug.Log($"RigBuilder: {(rigBuilder != null ? "Found" : "NULL")}");
        Debug.Log($"Weapon equipped: {isWeaponEquipped}");
        
        // Show ALL constraints
        if (allHandIkConstraints != null && allHandIkConstraints.Length > 0)
        {
            Debug.Log($"\n=== All Hand IK Constraints ({allHandIkConstraints.Length}) ===");
            foreach (var constraint in allHandIkConstraints)
            {
                if (constraint != null)
                {
                    Debug.Log($"  - {constraint.GetType().Name} on '{((Component)constraint).gameObject.name}' (weight: {constraint.weight})");
                }
            }
        }
        else
        {
            Debug.Log("\n=== No IK Constraints Found ===");
        }
        
        // List all components on handIk if it exists
        if (handIk != null)
        {
            Debug.Log("\n=== Components on handIk GameObject ===");
            var components = handIk.GetComponents<Component>();
            foreach (var comp in components)
            {
                Debug.Log($"  - {comp.GetType().Name}");
            }
        }
    }
    
    [ContextMenu("Debug: Find All IK Constraints")]
    public void DebugFindAllIKConstraints()
    {
        Debug.Log("=== SEARCHING FOR ALL IK CONSTRAINTS ===");
        var allConstraints = GetComponentsInChildren<IRigConstraint>(true);
        
        if (allConstraints.Length > 0)
        {
            Debug.Log($"Found {allConstraints.Length} IK constraint(s):");
            foreach (var constraint in allConstraints)
            {
                Debug.Log($"  - {constraint.GetType().Name} on '{((Component)constraint).gameObject.name}' (weight: {constraint.weight})");
            }
        }
        else
        {
            Debug.LogWarning("No IK constraints found anywhere in children!");
        }
    }
}
