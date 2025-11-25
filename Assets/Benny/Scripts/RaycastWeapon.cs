using UnityEngine;
using System.Collections;


public class RaycastWeapon : MonoBehaviour
{
    [Header("UI")]
    public Sprite weaponIcon;

    [Header("Animation Recording")]
    [SerializeField] private AnimationClip animationClip;

    [Header("Weapon Mode")]
    public bool useProjectiles = false; // Toggle between raycast and projectile

    [Header("Weapon Stats")]
    public float damage = 10f;
    public float headshotMultiplier = 2f;
    public float range = 100f;
    public float fireRate = 0.1f; // Time between shots

    [Header("Projectile Settings")]
    public GameObject projectilePrefab;
    public float projectileSpeed = 50f;
    public float projectileLifetime = 5f;

    [Header("References")]
    public Transform raycastDestination;
    public Transform bulletSpawnPoint;

    [Header("Hand Grip Positions")]
    [Tooltip("Left hand grip position for this weapon. If not set, will use ActiveWeapon's default.")]
    public Transform leftHandGrip;
    [Tooltip("Right hand grip position for this weapon. If not set, will use ActiveWeapon's default.")]
    public Transform rightHandGrip;

    [Header("Effects")]
    public GameObject gunEffect;
    public GameObject impactEffect;
    public AudioSource shootingSound;
    public LineRenderer bulletTrailPrefab;
    public TrailRenderer dartTrail;
    public ParticleSystem impactParticleSystem;

    [Header("Ammo Settings")]
    public int magazineSize = 30;     // Max bullets in a mag
    public int currentAmmo = 30;      // Current ammo
    public float reloadTime = 1.5f;   // Reload duration
    public bool isReloading = false;

    public AudioClip reloadSound;     // Optional


    [Header("Bullet Trail Settings")]
    public float bulletSpeed = 100f;
    public float bulletTrailTime = 0.05f;

    [Header("Accuracy Settings")]
    [Tooltip("Maximum spread angle in degrees for non-projectile weapons. Higher values = less accurate.")]
    public float bulletSpread = 2f; // Spread in degrees

    // Tap-only firing control
    private float nextFireTime = 0f;
    private bool readyToFire = true;
    
    // Reference to BananaPeelThrower to avoid conflicts
    private BananaPeelThrower bananaPeelThrower;

    // Backwards compatibility property
    public bool isFiring
    {
        get
        {
            // Don't report as firing if BananaPeelThrower is handling input
            if (bananaPeelThrower != null && bananaPeelThrower.enabled)
                return false;
                
            bool pressed = Input.GetMouseButton(0); // holding state
            return pressed && Time.time >= nextFireTime;
        }
        set
        {
            if (!value)
            {
                // Treat "stop firing" as disarm until release
                readyToFire = true;
            }
        }
    }

    public AnimationClip AnimationClip
    {
        get => animationClip;
        set => animationClip = value;
    }


    void Start()
    {
        if (shootingSound == null)
            shootingSound = GetComponent<AudioSource>();

        // Check for BananaPeelThrower to avoid conflicts
        bananaPeelThrower = GetComponent<BananaPeelThrower>();

        // Validate weapon configuration
        ValidateWeaponConfiguration();
    }

    /// <summary>
    /// Validates that the weapon has all required components and settings configured
    /// </summary>
    void ValidateWeaponConfiguration()
    {
        if (useProjectiles)
        {
            if (projectilePrefab == null)
            {
                Debug.LogError($"[{gameObject.name}] Weapon is set to use projectiles but projectilePrefab is not assigned!");
            }

            if (raycastDestination == null)
            {
                Debug.LogWarning($"[{gameObject.name}] Weapon is set to use projectiles but raycastDestination is not assigned! This will be set automatically by ActiveWeapon when equipped.");
            }

            if (bulletSpawnPoint == null)
            {
                Debug.LogWarning($"[{gameObject.name}] Bullet spawn point not set. Projectiles will spawn at weapon position.");
            }
        }
    }

    // Intentionally empty for tap-only fire
    public void StartFiring() { }
    public void StopFiring() { readyToFire = true; }

    // Call this from your update loop
    public void UpdateFiring(float deltaTime)
    {
        // Skip firing logic if BananaPeelThrower is present and enabled (it handles firing)
        if (bananaPeelThrower != null && bananaPeelThrower.enabled)
        {
            // Still allow reload logic to work
            if (isReloading)
                return;

            // Press R to reload
            if (Input.GetKeyDown(KeyCode.R) && currentAmmo < magazineSize)
            {
                StartCoroutine(Reload());
                return;
            }

            // Out of ammo? Auto reload
            if (currentAmmo <= 0)
            {
                StartCoroutine(Reload());
                return;
            }
            
            // Don't process firing input - BananaPeelThrower handles it
            return;
        }

        if (Input.GetKeyDown(KeyCode.R))
        {
            Debug.Log("R pressed | ammo = " + currentAmmo + "/" + magazineSize);
        }

        if (isReloading)
            return;

        // Press R to reload
        if (Input.GetKeyDown(KeyCode.R) && currentAmmo < magazineSize)
        {
            StartCoroutine(Reload());
            return;
        }

        // Out of ammo? Auto reload
        if (currentAmmo <= 0)
        {
            StartCoroutine(Reload());
            return;
        }
        // Fire once on tap only; ignores holding
        if (readyToFire && Time.time >= nextFireTime && Input.GetMouseButtonDown(0))
        {
            nextFireTime = Time.time + fireRate;
            readyToFire = false;

            if (useProjectiles) ShootProjectile();
            else Shoot();
            currentAmmo--;
        }

        // Re-arm after button fully released so next tap can register
        if (!Input.GetMouseButton(0))
        {
            readyToFire = true;
        }
    }

    public void UpdateBullets(float deltaTime)
    {
        // For projectile-based weapons if needed
    }

    void ShootProjectile()
    {
        if (projectilePrefab == null)
        {
            Debug.LogWarning($"[{gameObject.name}] Projectile prefab not assigned! Cannot fire.");
            return;
        }

        if (raycastDestination == null)
        {
            Debug.LogWarning($"[{gameObject.name}] Raycast destination not set! Cannot fire. Please assign raycastDestination in the Inspector.");
            return;
        }

        if (bulletSpawnPoint == null)
        {
            Debug.LogWarning($"[{gameObject.name}] Bullet spawn point not set! Using weapon position as fallback.");
        }

        Vector3 spawnPosition = GetBulletStartPosition();
        Vector3 direction = (raycastDestination.position - spawnPosition).normalized;

        // Play effects
        if (shootingSound != null)
            shootingSound.Play();

        if (gunEffect != null)
            Instantiate(gunEffect, spawnPosition, transform.rotation);

        // Spawn projectile
        GameObject projectile = Instantiate(projectilePrefab, spawnPosition, Quaternion.LookRotation(direction));

        // Add velocity to projectile
        Rigidbody rb = projectile.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.linearVelocity = direction * projectileSpeed;
        }
        else
        {
            Debug.LogWarning("Projectile prefab needs a Rigidbody component!");
        }

        // Ignore collisions with player model and weapon model
        IgnorePlayerAndWeaponCollisions(projectile);

        // Add Projectile component if it doesn't exist
        Projectile proj = projectile.GetComponent<Projectile>();
        if (proj == null)
        {
            proj = projectile.AddComponent<Projectile>();
        }

        // Set projectile damage and properties
        proj.damage = damage;
        proj.headshotMultiplier = headshotMultiplier;
        proj.impactEffect = impactEffect;

        // Destroy projectile after lifetime
        Destroy(projectile, projectileLifetime);

        Debug.Log($"[{gameObject.name}] Projectile fired from {spawnPosition} towards {raycastDestination.position}!");
    }
    public IEnumerator Reload()
    {
        if (isReloading) yield break;

        isReloading = true;
        Debug.Log("Reloading...");

        // Play reload audio
        if (reloadSound != null && shootingSound != null)
        {
            shootingSound.PlayOneShot(reloadSound);
        }

        // Wait for reload animation/time
        yield return new WaitForSeconds(reloadTime);

        currentAmmo = magazineSize;
        isReloading = false;

        Debug.Log("Reload complete!");
    }
    void OnEnable()
    {
        readyToFire = true;
        isReloading = false;
        StopAllCoroutines();
    }



    void Shoot()
    {
        if (raycastDestination == null)
        {
            Debug.LogWarning("Raycast destination not set!");
            return;
        }

        RaycastHit[] hits;
        RaycastHit hit = default(RaycastHit);
        Vector3 shootOrigin = GetBulletStartPosition();
        Vector3 baseDirection = (raycastDestination.position - shootOrigin).normalized;
        
        // Apply random spread to the bullet direction
        Vector3 shootDirection = ApplyBulletSpread(baseDirection);
        Vector3 bulletEndPoint = shootOrigin + shootDirection * range;

        // Play effects
        if (shootingSound != null)
            shootingSound.Play();

        if (gunEffect != null)
            Instantiate(gunEffect, shootOrigin, transform.rotation);

        // Raycast to find hits
        hits = Physics.RaycastAll(shootOrigin, shootDirection, range);

        if (hits.Length > 0)
        {
            // Sort hits by distance
            System.Array.Sort(hits, (x, y) => x.distance.CompareTo(y.distance));

            hit = hits[0];
            bulletEndPoint = hit.point;

            // Check for headshot first
            bool isHeadshot = false;
            RaycastHit headshotHit = default(RaycastHit);
            EnemyDamage target = null;

            foreach (RaycastHit h in hits)
            {
                if (h.collider.CompareTag("Head") && !isHeadshot)
                {
                    isHeadshot = true;
                    headshotHit = h;

                    Transform enemyTransform = h.transform;
                    while (enemyTransform != null && target == null)
                    {
                        target = enemyTransform.GetComponent<EnemyDamage>();
                        if (target == null)
                            enemyTransform = enemyTransform.parent;
                        else
                            break;
                    }

                    hit = headshotHit;
                    bulletEndPoint = hit.point;
                    break;
                }
            }

            // If no headshot, find first enemy hit
            if (!isHeadshot)
            {
                foreach (RaycastHit h in hits)
                {
                    target = h.transform.GetComponent<EnemyDamage>();
                    if (target != null)
                    {
                        hit = h;
                        bulletEndPoint = hit.point;
                        break;
                    }
                }
            }

            Debug.Log("Hit: " + hit.collider.name + (isHeadshot ? " [HEADSHOT]" : ""));

            // Bullet trail
            if (dartTrail != null)
            {
                TrailRenderer trail = Instantiate(dartTrail, GetBulletStartPosition(), Quaternion.identity);
                StartCoroutine(SpawnDartTrail(trail, hit));
            }

            // Impact on obstacles
            if (hit.collider.CompareTag("Obstacle"))
            {
                CreateBulletImpactEffect(hit);
            }

            // Deal damage to target
            if (target != null)
            {
                if (impactEffect != null)
                    Instantiate(impactEffect, hit.point, Quaternion.LookRotation(hit.normal));

                float finalDamage = damage;

                if (isHeadshot)
                {
                    finalDamage = damage * headshotMultiplier;
                    Debug.Log("HEADSHOT! Damage: " + finalDamage);
                }

                if (target.IsSlipping())
                {
                    target.TakeDamage(finalDamage, isHeadshot);
                }
                else
                {
                    target.TakeDamage(finalDamage, isHeadshot);
                }
            }
        }

        // Create bullet trail effect
        StartCoroutine(SpawnBulletTrail(GetBulletStartPosition(), bulletEndPoint));
    }

    void CreateBulletImpactEffect(RaycastHit hit)
    {
        if (GlobalReferences.Instance != null && GlobalReferences.Instance.bulletImpactEffectPrefab != null)
        {
            GameObject hole = Instantiate(GlobalReferences.Instance.bulletImpactEffectPrefab, hit.point, Quaternion.LookRotation(hit.normal));
            hole.transform.SetParent(hit.collider.transform);
        }
    }

    Vector3 GetBulletStartPosition()
    {
        return bulletSpawnPoint != null ? bulletSpawnPoint.position : transform.position;
    }

    /// <summary>
    /// Applies random spread to the bullet direction for non-projectile weapons
    /// </summary>
    Vector3 ApplyBulletSpread(Vector3 direction)
    {
        if (bulletSpread <= 0f)
            return direction;

        // Generate random angles within the spread range
        float spreadAngle = Random.Range(0f, bulletSpread);
        float spreadRotation = Random.Range(0f, 360f); // Random rotation around the axis

        // Create a random direction perpendicular to the base direction
        Vector3 perpendicular = Vector3.Cross(direction, Vector3.up).normalized;
        if (perpendicular.magnitude < 0.1f) // If direction is too close to up/down, use forward
            perpendicular = Vector3.Cross(direction, Vector3.forward).normalized;

        // Rotate the perpendicular vector around the base direction
        Quaternion rotation = Quaternion.AngleAxis(spreadRotation, direction);
        Vector3 spreadDirection = rotation * perpendicular;

        // Apply the spread angle
        Quaternion spreadRotationQuat = Quaternion.AngleAxis(spreadAngle, spreadDirection);
        return spreadRotationQuat * direction;
    }

    IEnumerator SpawnBulletTrail(Vector3 startPoint, Vector3 endPoint)
    {
        if (bulletTrailPrefab != null)
        {
            LineRenderer trail = Instantiate(bulletTrailPrefab, startPoint, Quaternion.identity);
            trail.positionCount = 2;
            trail.SetPosition(0, startPoint);
            trail.SetPosition(1, startPoint);

            float distance = Vector3.Distance(startPoint, endPoint);
            float travelTime = distance / bulletSpeed;
            float elapsedTime = 0f;

            while (elapsedTime < travelTime)
            {
                elapsedTime += Time.deltaTime;
                float t = elapsedTime / travelTime;

                Vector3 currentPosition = Vector3.Lerp(startPoint, endPoint, t);
                trail.SetPosition(1, currentPosition);

                yield return null;
            }

            trail.SetPosition(1, endPoint);
            yield return new WaitForSeconds(bulletTrailTime);
            Destroy(trail.gameObject);
        }
    }

    IEnumerator SpawnDartTrail(TrailRenderer trail, RaycastHit hit)
    {
        float time = 0;
        Vector3 startPosition = trail.transform.position;

        while (time < 1)
        {
            trail.transform.position = Vector3.Lerp(startPosition, hit.point, time);
            time += Time.deltaTime / trail.time;
            yield return null;
        }

        trail.transform.position = hit.point;
        Destroy(trail.gameObject, trail.time);
    }

    /// <summary>
    /// Ignores collisions between the projectile and player model/weapon model
    /// </summary>
    void IgnorePlayerAndWeaponCollisions(GameObject projectile)
    {
        Collider projectileCollider = projectile.GetComponent<Collider>();
        if (projectileCollider == null)
        {
            Debug.LogWarning("Projectile prefab needs a Collider component for collision ignoring!");
            return;
        }

        // Find player by tag
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            // Get all colliders on player and its children
            Collider[] playerColliders = player.GetComponentsInChildren<Collider>();
            foreach (Collider playerCollider in playerColliders)
            {
                Physics.IgnoreCollision(projectileCollider, playerCollider, true);
            }
        }

        // Find weapon (this weapon's GameObject and its children)
        if (gameObject != null)
        {
            Collider[] weaponColliders = gameObject.GetComponentsInChildren<Collider>();
            foreach (Collider weaponCollider in weaponColliders)
            {
                Physics.IgnoreCollision(projectileCollider, weaponCollider, true);
            }
        }

        // Also check for PlayerHealth component (in case player isn't tagged)
        PlayerHealth playerHealth = FindFirstObjectByType<PlayerHealth>();
        if (playerHealth != null && playerHealth.gameObject != player)
        {
            Collider[] healthColliders = playerHealth.GetComponentsInChildren<Collider>();
            foreach (Collider healthCollider in healthColliders)
            {
                Physics.IgnoreCollision(projectileCollider, healthCollider, true);
            }
        }
    }
}
