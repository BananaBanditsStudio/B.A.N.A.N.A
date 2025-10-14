using UnityEngine;
using System.Collections;


public class gun : MonoBehaviour
{
    public float damage = 10f;
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
        if (Input.GetButtonDown("Fire1"))
        {
            m_shootingSound.Play();
            Shoot();
        }
    }

    void Shoot()
    {
        RaycastHit hit;

        // Determine the end point of the bullet
        Vector3 bulletEndPoint;

        Instantiate(gunEffect, transform.position, transform.rotation);
        if (Physics.Raycast(fpsCam.transform.position, fpsCam.transform.forward, out hit, range))
        {
            Debug.Log("Hit: " + hit.collider.name);
            bulletEndPoint = hit.point;

            TrailRenderer trail = Instantiate(dartTrail, bulletSpawnPoint.position, Quaternion.identity);
            StartCoroutine(SpawnDartTrail(trail, hit));

            // Check obstacle tag
            if (hit.collider.CompareTag("Obstacle"))
            {
                CreateBulletImpactEffect(hit);
            }

            EnemyDamage target = hit.transform.GetComponent<EnemyDamage>();
            if (target != null)
            {
                Instantiate(impactEffect, hit.point, Quaternion.LookRotation(hit.normal));
                target.TakeDamage(damage);
            }
        }
        else
        {
            // No hit, bullet travels max range
            bulletEndPoint = fpsCam.transform.position + fpsCam.transform.forward * range;
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