using UnityEngine;
using System.Collections;

public class RaycastWeapon : MonoBehaviour
{
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

    [Header("Bullet Trail Settings")]
    public float bulletSpeed = 100f;
    public float bulletTrailTime = 0.05f;

    // Tap-only firing control
    private float nextFireTime = 0f;
    private bool readyToFire = true;

    // Backwards compatibility property
    public bool isFiring
    {
        get
        {
            bool pressed = Input.GetMouseButton(0); // holding state
            return pressed && Time.time >= nextFireTime;
        }
        set
        {
            if (!value)
            {
                // Treat “stop firing” as disarm until release
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
    }

    // Intentionally empty for tap-only fire
    public void StartFiring() { }
    public void StopFiring() { readyToFire = true; }

    // Call this from your update loop
    public void UpdateFiring(float deltaTime)
    {
        // Fire once on tap only; ignores holding
        if (readyToFire && Time.time >= nextFireTime && Input.GetMouseButtonDown(0))
        {
            nextFireTime = Time.time + fireRate;
            readyToFire = false;

            if (useProjectiles) ShootProjectile();
            else Shoot();
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
            Debug.LogWarning("Projectile prefab not assigned!");
            return;
        }

        if (raycastDestination == null)
        {
            Debug.LogWarning("Raycast destination not set!");
            return;
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

        Debug.Log("Projectile fired!");
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
        Vector3 shootDirection = (raycastDestination.position - shootOrigin).normalized;
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
}
