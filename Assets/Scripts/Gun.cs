using UnityEngine;


public class gun : MonoBehaviour
{
    public float damage = 10f;
    public float range = 100f;
    public Camera fpsCam;

    public GameObject grenadePrefab;
    public Transform throwPoint;
    public GameObject gunEffect;
    public GameObject impactEffect;
    public AudioSource m_shootingSound;

    // Bullet trail settings
    public LineRenderer bulletTrailPrefab;
    public Transform bulletSpawnPoint; // Point where bullet originates (gun barrel)
    public float bulletSpeed = 100f;
    public float bulletTrailTime = 0.05f; // How long the trail lasts

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

        // G button for throwable testing
        if (Input.GetKeyDown(KeyCode.G))
        {
            ThrowLethal();
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

    void ThrowLethal()
    {
        GameObject lethalPrefab = grenadePrefab;
        GameObject throwable = Instantiate(lethalPrefab, throwPoint.position, Camera.main.transform.rotation);
        Rigidbody rb = throwable.GetComponent<Rigidbody>();
        rb.AddForce(Camera.main.transform.forward * 40f, ForceMode.Impulse);
        throwable.GetComponent<Throwable>().hasBeenThrown = true;

        if (Physics.Raycast(fpsCam.transform.position, fpsCam.transform.forward, out RaycastHit hit, range))
        {
            Debug.Log("Throwing towards: " + hit.collider.name);
        }
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

    System.Collections.IEnumerator SpawnBulletTrail(Vector3 startPoint, Vector3 endPoint)
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
}