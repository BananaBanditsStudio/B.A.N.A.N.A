using UnityEngine;


public class gun : MonoBehaviour
{
    public float damage = 10f;
    public float range = 100f;
    public Camera fpsCam;

    public GameObject grenadePrefab;
    public Transform throwPoint;
    // Update is called once per frame
    void Update()
    {
        if (Input.GetButtonDown("Fire1"))
        {
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
        if (Physics.Raycast(fpsCam.transform.position, fpsCam.transform.forward, out hit, range))
        {
            Debug.Log("Hit: " + hit.collider.name);

            EnemyDamage target = hit.transform.GetComponent<EnemyDamage>();
            if (target != null)
            {
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
}