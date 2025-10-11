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


        Instantiate(gunEffect, transform.position, transform.rotation);
        if (Physics.Raycast(fpsCam.transform.position, fpsCam.transform.forward, out hit, range))
        {
            Debug.Log("Hit: " + hit.collider.name);

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
}