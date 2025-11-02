using UnityEngine;
using System.Collections;


public class gun : MonoBehaviour
{
    public float damage = 10f;
    public float headshotMultiplier = 2f; // 2x damage for headshots
    public float range = 100f;
    public Camera fpsCam;

    public GameObject gunEffect;
    public GameObject impactEffect;
    public AudioSource m_shootingSound;

    // Bullet trail settings
    public LineRenderer bulletTrailPrefab;
    public Transform bulletSpawnPoint; // Point where bullet originates (gun barrel)
    public float bulletSpeed = 100f;
    public float bulletTrailTime = 0.05f; // How long the trail lasts


    public ParticleSystem impactParticleSystem;
    public TrailRenderer dartTrail;


    // Update is called once per frame

    void Start()
    {
        m_shootingSound = GetComponent<AudioSource>();
    }
    void Update()
    {
        // Check if input is allowed (not paused or game over)
        if (Input.GetButtonDown("Fire1") && GameStateManager.CanShootStatic())
        {
            m_shootingSound.Play();
            Shoot();
        }
    }

    void Shoot()
    {
        RaycastHit[] hits;
        
        // Initialize variables with default values
        RaycastHit hit = default(RaycastHit);
        Vector3 bulletEndPoint = fpsCam.transform.position + fpsCam.transform.forward * range;

        Instantiate(gunEffect, transform.position, transform.rotation);
        
        // Use RaycastAll to get all colliders hit along the ray
        hits = Physics.RaycastAll(fpsCam.transform.position, fpsCam.transform.forward, range);
        
        if (hits.Length > 0)
        {
            // Sort hits by distance
            System.Array.Sort(hits, (x, y) => x.distance.CompareTo(y.distance));
            
            // Use the closest hit as default (for obstacle detection, etc.)
            hit = hits[0];
            bulletEndPoint = hit.point;
            
            // First, find if any hit is a headshot (prioritize headshots)
            bool isHeadshot = false;
            RaycastHit headshotHit = default(RaycastHit);
            EnemyDamage target = null;
            
            foreach (RaycastHit h in hits)
            {
                // Check for headshot first
                if (h.collider.CompareTag("Head") && !isHeadshot)
                {
                    isHeadshot = true;
                    headshotHit = h;
                    // Find the EnemyDamage component - check parent hierarchy
                    Transform enemyTransform = h.transform;
                    while (enemyTransform != null && target == null)
                    {
                        target = enemyTransform.GetComponent<EnemyDamage>();
                        if (target == null)
                            enemyTransform = enemyTransform.parent;
                        else
                            break;
                    }
                    // Update hit and bulletEndPoint for headshot
                    hit = headshotHit;
                    bulletEndPoint = hit.point;
                    break; // Found headshot, prioritize this
                }
            }
            
            // If no headshot, find the first enemy hit
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

            TrailRenderer trail = Instantiate(dartTrail, bulletSpawnPoint.position, Quaternion.identity);
            StartCoroutine(SpawnDartTrail(trail, hit));

            // Check obstacle tag
            if (hit.collider.CompareTag("Obstacle"))
            {
                CreateBulletImpactEffect(hit);
            }
            
            if (target != null)
            {
                Instantiate(impactEffect, hit.point, Quaternion.LookRotation(hit.normal));
                
                float finalDamage = damage;
                
                if (isHeadshot)
                {
                    finalDamage = damage * headshotMultiplier;
                    Debug.Log("HEADSHOT! Damage: " + finalDamage);
                }
                
                // Check if enemy is slipping and handle appropriately
                if (target.IsSlipping())
                {
                    // If enemy is slipping, just deal damage without additional effects
                    target.TakeDamage(finalDamage, isHeadshot);
                }
                else
                {
                    // Normal damage dealing
                    target.TakeDamage(finalDamage, isHeadshot);
                }
            }
        }

        // Create bullet trail effect
        StartCoroutine(SpawnBulletTrail(GetBulletStartPosition(), bulletEndPoint));
    }

    void CreateBulletImpactEffect(RaycastHit hit)
    {
        GameObject hole = Instantiate(GlobalReferences.Instance.bulletImpactEffectPrefab, hit.point, Quaternion.LookRotation(hit.normal));
        hole.transform.SetParent(hit.collider.transform);
    }

    Vector3 GetBulletStartPosition()
    {
        // Use bulletSpawnPoint if assigned, otherwise use gun transform position
        return bulletSpawnPoint != null ? bulletSpawnPoint.position : transform.position;
    }

    IEnumerator SpawnBulletTrail(Vector3 startPoint, Vector3 endPoint)
    {
        if (bulletTrailPrefab != null)
        {
            // Create the bullet trail
            LineRenderer trail = Instantiate(bulletTrailPrefab, startPoint, Quaternion.identity);
            trail.positionCount = 2;
            trail.SetPosition(0, startPoint);
            trail.SetPosition(1, startPoint);

            float distance = Vector3.Distance(startPoint, endPoint);
            float travelTime = distance / bulletSpeed;
            float elapsedTime = 0f;

            // Animate the bullet trail
            while (elapsedTime < travelTime)
            {
                elapsedTime += Time.deltaTime;
                float t = elapsedTime / travelTime;

                Vector3 currentPosition = Vector3.Lerp(startPoint, endPoint, t);
                trail.SetPosition(1, currentPosition);

                yield return null;
            }

            // Ensure trail reaches end point
            trail.SetPosition(1, endPoint);

            // Wait before destroying the trail
            yield return new WaitForSeconds(bulletTrailTime);
            Destroy(trail.gameObject);
        }
    }

    private IEnumerator SpawnDartTrail(TrailRenderer trail, RaycastHit hit)
    {
        float time = 0;
        Vector3 startPosition = trail.transform.position;

        while (time < 1)
        {
            trail.transform.position = Vector3.Lerp(startPosition, hit.point, time);
            time += Time.deltaTime / trail.time;

            yield return null;
        }


        // Animator.SetBool("isShooting", false);
        trail.transform.position = hit.point;
        // Instantiate(impactParticleSystem, hit.point, Quaternion.LookRotation(hit.normal));

        Destroy(trail.gameObject, trail.time);
    }
}