using UnityEngine;
using UnityEngine.Animations.Rigging;
using System.Collections;
#if UNITY_EDITOR
using UnityEditor.Animations;
#endif
using System;

public class ActiveWeapon : MonoBehaviour
{
    public Transform crossHairTarget;
    public WeaponUI weaponUI;


    [SerializeField] private Transform handIk;
    [SerializeField] private Transform weaponParent;
    [SerializeField] private Transform weaponLeftGrip;
    [SerializeField] private Transform weaponRightGrip;

    [Header("Alternative: Direct GameObject Control")]
    [SerializeField] private GameObject handIkGameObject;

    [Header("Animation")]
    [SerializeField] private AnimationClip weaponAnimClip;
    [SerializeField] private string weaponAnimStateName = "Empty_anim";

    [Header("Multiple Weapons")]
    [SerializeField] private RaycastWeapon[] availableWeapons; // Array of all available weapons

    public RaycastWeapon weapon;
    private RaycastWeapon availableWeapon; // Kept for backward compatibility
    private int currentWeaponIndex = -1; // -1 means no weapon equipped
    private Animator animator;
    private AnimatorOverrideController animatorOverrideController;
    private AnimationClip defaultWeaponClip;
    private bool isWeaponEquipped = false;
    private IRigConstraint handIkConstraint;
    private IRigConstraint[] allHandIkConstraints; // Store ALL IK constraints under Hand_IK
    private RigBuilder rigBuilder;

    // Store left and right hand IK constraints separately for easier targeting
    private Component leftHandIKConstraint;
    private Component rightHandIKConstraint;

    // Reference to InputManager to check crouch state
    private UnityTutorial.Manager.InputManager inputManager;

    // Start is called before the first frame update

    void Start()
    {
        animator = GetComponent<Animator>();
        animatorOverrideController = new AnimatorOverrideController(animator.runtimeAnimatorController);
        animator.runtimeAnimatorController = animatorOverrideController;

        // Get InputManager component to check crouch state
        inputManager = GetComponent<UnityTutorial.Manager.InputManager>();
        if (inputManager == null)
        {
            Debug.LogWarning("InputManager not found on ActiveWeapon GameObject. Crouch conflict detection may not work.");
        }

        // Find the clip to override
        FindWeaponAnimationClip();

        // Get RigBuilder component and ensure it's enabled
        rigBuilder = GetComponent<RigBuilder>();
        if (rigBuilder == null)
        {
            Debug.LogWarning("No RigBuilder found on this GameObject!");
        }
        else
        {
            // Ensure RigBuilder is enabled for runtime (critical for builds)
            rigBuilder.enabled = true;
            Debug.Log("✓ RigBuilder found and enabled");
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
                    Component comp = (Component)constraint;
                    Debug.Log($"  - {constraint.GetType().Name} on {comp.gameObject.name} (current weight: {constraint.weight})");

                    // Try to identify left and right hand constraints by name
                    string constraintName = comp.gameObject.name.ToLower();
                    if (constraintName.Contains("left") && leftHandIKConstraint == null)
                    {
                        leftHandIKConstraint = comp;
                        Debug.Log($"  → Identified as LEFT hand constraint");
                    }
                    else if (constraintName.Contains("right") && rightHandIKConstraint == null)
                    {
                        rightHandIKConstraint = comp;
                        Debug.Log($"  → Identified as RIGHT hand constraint");
                    }
                }

                // If we couldn't identify by name, try to assign by order (first = left, second = right)
                if (leftHandIKConstraint == null && allHandIkConstraints.Length >= 1)
                {
                    leftHandIKConstraint = (Component)allHandIkConstraints[0];
                    Debug.Log($"  → Assigned first constraint as LEFT hand (by order)");
                }
                if (rightHandIKConstraint == null && allHandIkConstraints.Length >= 2)
                {
                    rightHandIKConstraint = (Component)allHandIkConstraints[1];
                    Debug.Log($"  → Assigned second constraint as RIGHT hand (by order)");
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

        // Initialize weapons array
        InitializeWeapons();

        // Start with weapon unequipped
        if (availableWeapons != null && availableWeapons.Length > 0)
        {
            foreach (var w in availableWeapons)
            {
                if (w != null)
                {
                    w.gameObject.SetActive(false);
                }
            }
        }

        // Backward compatibility: if no array assigned, try to find weapon automatically
        if (availableWeapons == null || availableWeapons.Length == 0)
        {
            availableWeapon = GetComponentInChildren<RaycastWeapon>();
            if (availableWeapon != null)
            {
                availableWeapon.gameObject.SetActive(false);
            }
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

    private void InitializeWeapons()
    {
        // If weapons array is not assigned in Inspector, try to find all weapons automatically
        if (availableWeapons == null || availableWeapons.Length == 0)
        {
            RaycastWeapon[] foundWeapons = GetComponentsInChildren<RaycastWeapon>(true);
            if (foundWeapons != null && foundWeapons.Length > 0)
            {
                availableWeapons = foundWeapons;
                Debug.Log($"Auto-found {availableWeapons.Length} weapon(s) in children");
            }
        }

        // Validate weapons array
        if (availableWeapons != null && availableWeapons.Length > 0)
        {
            Debug.Log($"Initialized {availableWeapons.Length} weapon(s)");
            for (int i = 0; i < availableWeapons.Length; i++)
            {
                if (availableWeapons[i] != null)
                {
                    Debug.Log($"  Weapon {i + 1}: {availableWeapons[i].name}");
                }
                else
                {
                    Debug.LogWarning($"  Weapon {i + 1}: NULL (not assigned)");
                }
            }
        }
        else
        {
            Debug.LogWarning("No weapons found! Assign weapons in Inspector or ensure RaycastWeapon components exist in children.");
        }
    }

    // Update is called once per frame
    void Update()
    {
        // Weapon switching with number keys (1-9)
        if (availableWeapons != null && availableWeapons.Length > 0)
        {
            // Check number keys 1-9 for direct weapon switching
            for (int i = 0; i < Mathf.Min(9, availableWeapons.Length); i++)
            {
                if (Input.GetKeyDown(KeyCode.Alpha1 + i))
                {
                    SwitchToWeapon(i);
                    
                    // Update WeaponUI
                    if (weaponUI != null)
                    {
                        weaponUI.SetWeapon(weapon);
                    }

                    return; // Exit early to avoid multiple switches
                }
            }
        }

        // Toggle weapon equip/unequip when "1" key is pressed (only if using old single-weapon system)
        if (Input.GetKeyDown(KeyCode.Alpha1) && (availableWeapons == null || availableWeapons.Length == 0))
        {
            ToggleWeapon();
        }

        // Mouse scroll wheel for cycling weapons
        if (availableWeapons != null && availableWeapons.Length > 1 && isWeaponEquipped)
        {
            float scroll = Input.GetAxis("Mouse ScrollWheel");
            if (scroll > 0f)
            {
                // Scroll up - next weapon
                int nextIndex = (currentWeaponIndex + 1) % availableWeapons.Length;
                SwitchToWeapon(nextIndex);
                
                // Update WeaponUI
                if (weaponUI != null)
                {
                    weaponUI.SetWeapon(weapon);
                }
            }
            else if (scroll < 0f)
            {
                // Scroll down - previous weapon
                int prevIndex = currentWeaponIndex - 1;
                if (prevIndex < 0) prevIndex = availableWeapons.Length - 1;
                SwitchToWeapon(prevIndex);
                
                // Update WeaponUI
                if (weaponUI != null)
                {
                    weaponUI.SetWeapon(weapon);
                }
            }
        }

        // Handle E key interaction with DistractionInteractable when no weapon is equipped
        if (!isWeaponEquipped && Input.GetKeyDown(KeyCode.E))
        {
            HandleDistractionInteraction();
        }

        if (weapon && isWeaponEquipped)
        {
            // ⭐ ALWAYS call UpdateFiring so reload + ammo logic works
            weapon.UpdateFiring(Time.deltaTime);

            // Check if crouch is active FIRST (prevents firing while crouching)
            // Since Fire1 is mapped to Ctrl (same as crouch), we need to check crouch state first
            bool isCrouching = false;
            if (inputManager != null)
            {
                isCrouching = inputManager.Crouch;
            }
            else
            {
                // Fallback: check for common crouch keys
                isCrouching = Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl) ||
                              Input.GetKey(KeyCode.C) || Input.GetKey(KeyCode.LeftShift);
            }

            // If crouching, completely ignore Fire1 input (since Fire1 = Ctrl = crouch key)
            if (!isCrouching)
            {
                // Check if Fire1 is pressed (only process if not crouching)
                if (Input.GetButtonDown("Fire1"))
                {
                    // Additional check: make sure crouch keys aren't being held
                    if (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl) ||
                        Input.GetKey(KeyCode.C) || Input.GetKey(KeyCode.LeftShift))
                    {
                        Debug.LogWarning("Fire1 triggered while crouch keys pressed - ignoring");
                    }
                    else
                    {
                        weapon.StartFiring();
                        Debug.Log("Fire1 pressed - Starting to fire");
                    }
                }

                // Continue firing
                if (weapon.isFiring)
                {
                    // (We no longer call UpdateFiring here — it's now always called above)
                }

                if (Input.GetButtonUp("Fire1"))
                {
                    weapon.StopFiring();
                    Debug.Log("Fire1 released - Stopped firing");
                }
            }
            else
            {
                // If crouching, stop any active firing
                if (weapon.isFiring)
                {
                    weapon.StopFiring();
                }
            }

            weapon.UpdateBullets(Time.deltaTime);
            HandleWeaponAiming();
        }
    }


    /// <summary>
    /// Handles E key interaction with DistractionInteractable when no weapon is equipped.
    /// Casts a ray from the camera center (crosshair) to detect interactables.
    /// </summary>
    private void HandleDistractionInteraction()
    {
        // Get camera - try to get from crossHairTarget first, fallback to Camera.main
        Camera cam = null;
        if (crossHairTarget != null)
        {
            cam = crossHairTarget.GetComponent<Camera>();
            if (cam == null && crossHairTarget.parent != null)
            {
                cam = crossHairTarget.parent.GetComponent<Camera>();
            }
        }
        if (cam == null)
        {
            cam = Camera.main;
        }

        if (cam == null)
        {
            return; // No camera found
        }

        // Cast ray from camera center (crosshair position)
        float interactionDistance = 30f; // Increased distance for far interaction
        Ray ray = new Ray(cam.transform.position, cam.transform.forward);

        // Use RaycastAll to check all hits along the ray
        // This allows interaction even if there are objects in between
        RaycastHit[] hits = Physics.RaycastAll(ray, interactionDistance);

        // Check all hits to find a DistractionInteractable
        foreach (RaycastHit hitInfo in hits)
        {
            // Check if we hit a DistractionInteractable
            DistractionInteractable distraction = hitInfo.collider.GetComponent<DistractionInteractable>();
            if (distraction != null)
            {
                // Interact with the distraction
                distraction.BaseInteract();
                return; // Only interact with the first one found
            }
        }

        // Alternative: If raycast doesn't work due to colliders, try direct distance/angle check
        // Find all DistractionInteractables in scene and check if player is looking at them
        DistractionInteractable[] allDistractions = FindObjectsOfType<DistractionInteractable>();
        foreach (DistractionInteractable distraction in allDistractions)
        {
            if (distraction == null) continue;

            Vector3 toDistraction = distraction.transform.position - cam.transform.position;
            float distance = toDistraction.magnitude;

            // Check if within interaction distance
            if (distance > interactionDistance) continue;

            // Check if player is looking at it (within reasonable angle)
            float angle = Vector3.Angle(cam.transform.forward, toDistraction.normalized);
            if (angle < 10f) // 10 degree cone
            {
                // Double check with raycast to ensure line of sight
                RaycastHit losHit;
                if (Physics.Raycast(cam.transform.position, toDistraction.normalized, out losHit, distance))
                {
                    if (losHit.collider.GetComponent<DistractionInteractable>() == distraction)
                    {
                        distraction.BaseInteract();
                        return;
                    }
                }
            }
        }
    }

    private void SwitchToWeapon(int weaponIndex)
    {
        // Validate index
        if (availableWeapons == null || weaponIndex < 0 || weaponIndex >= availableWeapons.Length)
        {
            Debug.LogWarning($"Invalid weapon index: {weaponIndex}");
            return;
        }

        if (availableWeapons[weaponIndex] == null)
        {
            Debug.LogWarning($"Weapon at index {weaponIndex} is null!");
            return;
        }

        // If switching to the same weapon, toggle it off
        if (currentWeaponIndex == weaponIndex && isWeaponEquipped)
        {
            UnequipCurrentWeapon();
            return;
        }

        // Unequip current weapon if one is equipped
        if (isWeaponEquipped && currentWeaponIndex >= 0)
        {
            UnequipCurrentWeapon();
        }

        // Equip new weapon
        currentWeaponIndex = weaponIndex;
        Equip(availableWeapons[weaponIndex]);
        availableWeapons[weaponIndex].gameObject.SetActive(true);
        isWeaponEquipped = true;

        // Ensure RigBuilder is enabled before equipping (critical for builds)
        if (rigBuilder != null && !rigBuilder.enabled)
        {
            rigBuilder.enabled = true;
            Debug.LogWarning("RigBuilder was disabled! Enabled it for IK to work.");
        }

        // Update IK constraint targets to use weapon-specific grips
        // Use coroutine to ensure weapon is fully active and grips are accessible
        StartCoroutine(DelayedIKUpdate());
        Debug.Log($"✓ Weapon {weaponIndex + 1} EQUIPPED: {availableWeapons[weaponIndex].name}");
    }

    private void UnequipCurrentWeapon()
    {
        if (currentWeaponIndex >= 0 && currentWeaponIndex < availableWeapons.Length && availableWeapons[currentWeaponIndex] != null)
        {
            availableWeapons[currentWeaponIndex].gameObject.SetActive(false);
        }

        Unequip();
        SetHandIkWeight(0f);
        Debug.Log($"✓ Weapon {currentWeaponIndex + 1} UNEQUIPPED");
        currentWeaponIndex = -1;
        isWeaponEquipped = false;
    }

    private void ToggleWeapon()
    {
        // Legacy method for single weapon system
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

        // Ensure RigBuilder is enabled (critical for runtime builds)
        if (rigBuilder != null && !rigBuilder.enabled)
        {
            rigBuilder.enabled = true;
            Debug.LogWarning("RigBuilder was disabled! Enabled it for IK to work.");
        }

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

            // Note: rigBuilder.Build() is typically not needed at runtime
            // Animation Rigging updates automatically, but we can force an update if needed
            if (rigBuilder != null && rigBuilder.enabled)
            {
                // Only rebuild if RigBuilder is enabled
                // Build() is mainly for editor-time, but can help force updates
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
            // Get grip positions - prefer weapon-specific grips, fallback to shared grips
            Transform leftGrip = GetLeftHandGrip();
            Transform rightGrip = GetRightHandGrip();

            // Set left hand IK to weapon grip position
            if (leftGrip != null)
            {
                animator.SetIKPositionWeight(AvatarIKGoal.LeftHand, 1f);
                animator.SetIKRotationWeight(AvatarIKGoal.LeftHand, 1f);
                animator.SetIKPosition(AvatarIKGoal.LeftHand, leftGrip.position);
                animator.SetIKRotation(AvatarIKGoal.LeftHand, leftGrip.rotation);
            }

            // Set right hand IK to weapon grip position
            if (rightGrip != null)
            {
                animator.SetIKPositionWeight(AvatarIKGoal.RightHand, 1f);
                animator.SetIKRotationWeight(AvatarIKGoal.RightHand, 1f);
                animator.SetIKPosition(AvatarIKGoal.RightHand, rightGrip.position);
                animator.SetIKRotation(AvatarIKGoal.RightHand, rightGrip.rotation);
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

    /// <summary>
    /// Gets the left hand grip transform for the current weapon.
    /// Returns weapon-specific grip if available, otherwise falls back to shared grip.
    /// </summary>
    private Transform GetLeftHandGrip()
    {
        if (weapon != null && weapon.leftHandGrip != null)
        {
            return weapon.leftHandGrip;
        }
        return weaponLeftGrip; // Fallback to shared grip
    }

    /// <summary>
    /// Gets the right hand grip transform for the current weapon.
    /// Returns weapon-specific grip if available, otherwise falls back to shared grip.
    /// </summary>
    private Transform GetRightHandGrip()
    {
        if (weapon != null && weapon.rightHandGrip != null)
        {
            return weapon.rightHandGrip;
        }
        return weaponRightGrip; // Fallback to shared grip
    }

    /// <summary>
    /// Updates the Animation Rigging IK constraint targets to use the current weapon's grip positions.
    /// This ensures that when switching weapons, the IK constraints point to the correct grip transforms.
    /// </summary>
    private void UpdateIKConstraintTargets()
    {
        Transform leftGrip = GetLeftHandGrip();
        Transform rightGrip = GetRightHandGrip();

        Debug.Log($"[UpdateIKConstraintTargets] Weapon: {(weapon != null ? weapon.name : "NULL")}, " +
                  $"Left grip: {(leftGrip != null ? leftGrip.name : "NULL")}, " +
                  $"Right grip: {(rightGrip != null ? rightGrip.name : "NULL")}");

        // Update left hand IK constraint target
        if (leftHandIKConstraint != null && leftGrip != null)
        {
            UpdateConstraintTarget(leftHandIKConstraint, leftGrip, "Left");
        }
        else
        {
            if (leftHandIKConstraint == null) Debug.LogWarning("[UpdateIKConstraintTargets] Left hand IK constraint is NULL!");
            if (leftGrip == null) Debug.LogWarning("[UpdateIKConstraintTargets] Left grip is NULL!");
        }

        // Update right hand IK constraint target
        if (rightHandIKConstraint != null && rightGrip != null)
        {
            UpdateConstraintTarget(rightHandIKConstraint, rightGrip, "Right");
        }
        else
        {
            if (rightHandIKConstraint == null) Debug.LogWarning("[UpdateIKConstraintTargets] Right hand IK constraint is NULL!");
            if (rightGrip == null) Debug.LogWarning("[UpdateIKConstraintTargets] Right grip is NULL!");
        }

        // Force rig rebuild to apply changes (if RigBuilder is enabled)
        if (rigBuilder != null && rigBuilder.enabled)
        {
            rigBuilder.Build();
            Debug.Log("[UpdateIKConstraintTargets] RigBuilder rebuilt after target update");
        }
        else
        {
            if (rigBuilder == null) Debug.LogWarning("[UpdateIKConstraintTargets] RigBuilder is NULL!");
            else if (!rigBuilder.enabled) Debug.LogWarning("[UpdateIKConstraintTargets] RigBuilder is disabled!");
        }
    }

    /// <summary>
    /// Updates a single IK constraint's target transform using Unity's SerializedObject API.
    /// Works with TwoBoneIKConstraint, ChainIKConstraint, and other common IK constraint types.
    /// </summary>
    private void UpdateConstraintTarget(Component constraint, Transform target, string handName)
    {
        if (constraint == null || target == null) return;

        // Try direct type casting first (most reliable for runtime)
        try
        {
            // Try TwoBoneIKConstraint
            var twoBoneIK = constraint as TwoBoneIKConstraint;
            if (twoBoneIK != null)
            {
                var data = twoBoneIK.data;
                data.target = target;
                twoBoneIK.data = data;
                Debug.Log($"✓ [RUNTIME] Updated {handName} hand TwoBoneIKConstraint target directly: {target.name}");
                return;
            }

            // Try ChainIKConstraint
            var chainIK = constraint as ChainIKConstraint;
            if (chainIK != null)
            {
                var data = chainIK.data;
                data.target = target;
                chainIK.data = data;
                Debug.Log($"✓ [RUNTIME] Updated {handName} hand ChainIKConstraint target directly: {target.name}");
                return;
            }

            // Try MultiAimConstraint (if used for hand IK)
            var multiAim = constraint as MultiAimConstraint;
            if (multiAim != null)
            {
                // MultiAimConstraint uses a different structure, but let's try
                var data = multiAim.data;
                // MultiAimConstraint doesn't have a single target, so skip it
                Debug.LogWarning($"[RUNTIME] MultiAimConstraint doesn't support single target updates for {handName} hand");
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[RUNTIME] Direct type casting failed: {e.Message}");
        }

#if UNITY_EDITOR
        try
        {
            UnityEditor.SerializedObject so = new UnityEditor.SerializedObject(constraint);
            
            // Try common property names for target
            UnityEditor.SerializedProperty targetProperty = so.FindProperty("m_Target") ?? 
                                                           so.FindProperty("m_Data.m_Target") ??
                                                           so.FindProperty("target") ??
                                                           so.FindProperty("data.target");
            
            if (targetProperty != null)
            {
                targetProperty.objectReferenceValue = target;
                so.ApplyModifiedProperties();
                Debug.Log($"✓ Updated {handName} hand IK constraint target: {target.name}");
                return;
            }
            
            // Try to find any Transform property that might be the target
            UnityEditor.SerializedProperty iterator = so.GetIterator();
            bool found = false;
            while (iterator.NextVisible(true))
            {
                if (iterator.propertyType == UnityEditor.SerializedPropertyType.ObjectReference &&
                    iterator.objectReferenceValue != null &&
                    iterator.objectReferenceValue is Transform)
                {
                    // Check if this looks like a target (common names)
                    string propName = iterator.name.ToLower();
                    if (propName.Contains("target") || propName.Contains("tip") || propName.Contains("end"))
                    {
                        iterator.objectReferenceValue = target;
                        found = true;
                        break;
                    }
                }
            }
            
            if (found)
            {
                so.ApplyModifiedProperties();
                Debug.Log($"✓ Updated {handName} hand IK constraint via property search: {target.name}");
            }
            else
            {
                Debug.LogWarning($"⚠ Could not find target property for {handName} hand IK constraint. Constraint type: {constraint.GetType().Name}");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"Failed to update {handName} hand IK constraint via SerializedObject: {e.Message}");
        }
#else
        // Runtime fallback: Use reflection to update constraint targets
        // CRITICAL: Animation Rigging constraints (TwoBoneIKConstraint, etc.) store target in data.m_Target field
        // The data struct is a VALUE TYPE, so we must get it, modify it, and set it back
        System.Type constraintType = constraint.GetType();
        Debug.Log($"[RUNTIME] Updating {handName} hand IK constraint target. Type: {constraintType.Name}, Target: {target.name}");

        bool success = false;

        // Method 1: Try data.m_Target field (most common in TwoBoneIKConstraint, ChainIKConstraint, etc.)
        // Animation Rigging constraints store target in a data struct as a field
        System.Reflection.PropertyInfo dataProp = constraintType.GetProperty("data", 
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
        
        if (dataProp != null)
        {
            try
            {
                // Get the data struct (value type, so we get a copy)
                object data = dataProp.GetValue(constraint);
                if (data != null)
                {
                    System.Type dataType = data.GetType();
                    
                    // Try m_Target field first (most common in Animation Rigging - TwoBoneIKConstraint uses this)
                    System.Reflection.FieldInfo dataTargetField = dataType.GetField("m_Target", 
                        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);
                    
                    if (dataTargetField != null && dataTargetField.FieldType == typeof(Transform))
                    {
                        // Modify the struct copy
                        dataTargetField.SetValue(data, target);
                        // Set the modified struct back to the constraint
                        dataProp.SetValue(constraint, data);
                        Debug.Log($"✓ [RUNTIME] Updated {handName} hand IK constraint via data.m_Target field: {target.name}");
                        success = true;
                    }
                    else
                    {
                        // Try m_Target property
                        System.Reflection.PropertyInfo dataTargetProp = dataType.GetProperty("m_Target", 
                            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);
                        
                        if (dataTargetProp != null && dataTargetProp.CanWrite && dataTargetProp.PropertyType == typeof(Transform))
                        {
                            dataTargetProp.SetValue(data, target);
                            dataProp.SetValue(constraint, data);
                            Debug.Log($"✓ [RUNTIME] Updated {handName} hand IK constraint via data.m_Target property: {target.name}");
                            success = true;
                        }
                        else
                        {
                            // Try target property (without m_ prefix)
                            System.Reflection.PropertyInfo targetProp = dataType.GetProperty("target", 
                                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.IgnoreCase);
                            
                            if (targetProp != null && targetProp.CanWrite && targetProp.PropertyType == typeof(Transform))
                            {
                                targetProp.SetValue(data, target);
                                dataProp.SetValue(constraint, data);
                                Debug.Log($"✓ [RUNTIME] Updated {handName} hand IK constraint via data.target property: {target.name}");
                                success = true;
                            }
                        }
                    }
                }
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[RUNTIME] Failed to set data.m_Target: {e.Message}\nStackTrace: {e.StackTrace}");
            }
        }

        // Method 2: Try direct m_Target field on constraint (fallback)
        if (!success)
        {
            System.Reflection.FieldInfo targetField = constraintType.GetField("m_Target", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);
            if (targetField != null && targetField.FieldType == typeof(Transform))
            {
                try
                {
                    targetField.SetValue(constraint, target);
                    Debug.Log($"✓ [RUNTIME] Updated {handName} hand IK constraint via m_Target field: {target.name}");
                    success = true;
                }
                catch (System.Exception e)
                {
                    Debug.LogWarning($"[RUNTIME] Failed to set m_Target field: {e.Message}");
                }
            }
        }

        // Method 3: Try direct target property (fallback)
        if (!success)
        {
            System.Reflection.PropertyInfo directTargetProp = constraintType.GetProperty("target", 
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.IgnoreCase);
            if (directTargetProp != null && directTargetProp.CanWrite && directTargetProp.PropertyType == typeof(Transform))
            {
                try
                {
                    directTargetProp.SetValue(constraint, target);
                    Debug.Log($"✓ [RUNTIME] Updated {handName} hand IK constraint via target property: {target.name}");
                    success = true;
                }
                catch (System.Exception e)
                {
                    Debug.LogWarning($"[RUNTIME] Failed to set target property: {e.Message}");
                }
            }
        }

        if (!success)
        {
            // Log all available fields and properties for debugging
            Debug.LogError($"❌ [RUNTIME] Could not update {handName} hand IK constraint target! Constraint type: {constraintType.Name}");
            Debug.LogError($"Available properties in {constraintType.Name}:");
            foreach (var prop in constraintType.GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic))
            {
                Debug.LogError($"  - {prop.Name} ({prop.PropertyType.Name})");
            }
            Debug.LogError($"Available fields in {constraintType.Name}:");
            foreach (var field in constraintType.GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic))
            {
                Debug.LogError($"  - {field.Name} ({field.FieldType.Name})");
            }
        }
        else
        {
            // Force constraint to update by toggling weight (helps RigBuilder recognize the change)
            IRigConstraint rigConstraint = constraint as IRigConstraint;
            if (rigConstraint != null)
            {
                float currentWeight = rigConstraint.weight;
                rigConstraint.weight = 0f;
                rigConstraint.weight = currentWeight;
            }
        }
#endif
    }

    public void Equip(RaycastWeapon newWeapon)
    {
        weapon = newWeapon;

        // Set raycast destination if not already set
        if (weapon.raycastDestination == null)
        {
            weapon.raycastDestination = crossHairTarget;
            Debug.Log($"✓ Set raycastDestination for {newWeapon.name} to crossHairTarget");
        }
        else if (weapon.raycastDestination != crossHairTarget)
        {
            Debug.LogWarning($"⚠ {newWeapon.name} has a different raycastDestination assigned. Using assigned value instead of crossHairTarget.");
        }

        Debug.Log("=== EQUIPPING WEAPON ===");
        Debug.Log($"Weapon: {newWeapon.name}");
        Debug.Log($"Weapon AnimationClip: {(newWeapon.AnimationClip != null ? newWeapon.AnimationClip.name : "NULL")}");
        Debug.Log($"Override Controller: {(animatorOverrideController != null ? "OK" : "NULL")}");
        Debug.Log($"Default Weapon Clip: {(defaultWeaponClip != null ? defaultWeaponClip.name : "NULL")}");
        Debug.Log($"Raycast Destination: {(weapon.raycastDestination != null ? weapon.raycastDestination.name : "NULL - THIS WILL PREVENT FIRING!")}");
        Debug.Log($"Bullet Spawn Point: {(weapon.bulletSpawnPoint != null ? weapon.bulletSpawnPoint.name : "NULL - Will use weapon position")}");
        Debug.Log($"Use Projectiles: {weapon.useProjectiles}");
        Debug.Log($"Projectile Prefab: {(weapon.projectilePrefab != null ? weapon.projectilePrefab.name : "NULL - REQUIRED FOR PROJECTILE MODE!")}");

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
#if UNITY_EDITOR
        UnityEditor.Animations.GameObjectRecorder recorder = new UnityEditor.Animations.GameObjectRecorder(gameObject);

        if (weaponParent != null)
            recorder.BindComponentsOfType<Transform>(weaponParent.gameObject, false);

        // Use current weapon's grips if available, otherwise use shared grips
        Transform leftGrip = GetLeftHandGrip();
        Transform rightGrip = GetRightHandGrip();

        if (leftGrip != null)
            recorder.BindComponentsOfType<Transform>(leftGrip.gameObject, false);

        if (rightGrip != null)
            recorder.BindComponentsOfType<Transform>(rightGrip.gameObject, false);

        recorder.TakeSnapshot(0.0f);
        recorder.SaveToClip(weaponAnimClip);

        Debug.Log("Weapon pose saved to clip!");
#else
        Debug.LogWarning("SaveWeaponPose() is only available in the Unity Editor.");
#endif
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

    [ContextMenu("Debug: Update IK Constraint Targets")]
    public void DebugUpdateIKConstraintTargets()
    {
        Debug.Log("=== MANUALLY UPDATING IK CONSTRAINT TARGETS ===");
        Debug.Log($"Left Hand Constraint: {(leftHandIKConstraint != null ? leftHandIKConstraint.name : "NULL")}");
        Debug.Log($"Right Hand Constraint: {(rightHandIKConstraint != null ? rightHandIKConstraint.name : "NULL")}");

        Transform leftGrip = GetLeftHandGrip();
        Transform rightGrip = GetRightHandGrip();

        Debug.Log($"Left Hand Grip: {(leftGrip != null ? leftGrip.name : "NULL")}");
        Debug.Log($"Right Hand Grip: {(rightGrip != null ? rightGrip.name : "NULL")}");

        UpdateIKConstraintTargets();

        Debug.Log("✓ IK constraint targets updated!");
    }

    [ContextMenu("Debug: Show Current Weapon Grips")]
    public void DebugShowCurrentWeaponGrips()
    {
        Debug.Log("=== CURRENT WEAPON GRIP INFO ===");
        if (weapon == null)
        {
            Debug.LogWarning("No weapon currently equipped!");
            return;
        }

        Debug.Log($"Current Weapon: {weapon.name}");
        Debug.Log($"Weapon Left Hand Grip: {(weapon.leftHandGrip != null ? weapon.leftHandGrip.name : "NULL (using shared)")}");
        Debug.Log($"Weapon Right Hand Grip: {(weapon.rightHandGrip != null ? weapon.rightHandGrip.name : "NULL (using shared)")}");

        Transform leftGrip = GetLeftHandGrip();
        Transform rightGrip = GetRightHandGrip();

        Debug.Log($"Active Left Grip: {(leftGrip != null ? leftGrip.name : "NULL")}");
        Debug.Log($"Active Right Grip: {(rightGrip != null ? rightGrip.name : "NULL")}");

        if (leftGrip != null)
        {
            Debug.Log($"  Left Grip Position: {leftGrip.position}");
            Debug.Log($"  Left Grip Rotation: {leftGrip.rotation.eulerAngles}");
        }
        if (rightGrip != null)
        {
            Debug.Log($"  Right Grip Position: {rightGrip.position}");
            Debug.Log($"  Right Grip Rotation: {rightGrip.rotation.eulerAngles}");
        }
    }
    public RaycastWeapon GetCurrentWeapon()
    {
        return weapon; // or whatever variable stores the ACTIVE gun
    }

    /// <summary>
    /// Coroutine to delay IK update, ensuring weapon is fully active and grips are accessible.
    /// This is especially important in builds where timing can differ from the editor.
    /// </summary>
    private IEnumerator DelayedIKUpdate()
    {
        // Wait one frame to ensure weapon GameObject is fully active
        yield return null;
        
        // Update IK constraint targets to use weapon-specific grips
        UpdateIKConstraintTargets();
        
        // Wait another frame to ensure targets are set
        yield return null;
        
        // Set IK weight after targets are updated
        SetHandIkWeight(1f);
        
        // Force one more update after weight is set
        yield return null;
        UpdateIKConstraintTargets();
        
        Debug.Log("[DelayedIKUpdate] IK targets and weight updated for weapon");
    }

}
