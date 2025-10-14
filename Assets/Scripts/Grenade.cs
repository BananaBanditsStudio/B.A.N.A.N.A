using UnityEngine;
using System.Collections;


public class Grenade : MonoBehaviour
{
    public float range = 100f;
    public GameObject grenadePrefab;
    public Transform throwPoint;
    public Camera fpsCam;

    void Start()
    {
        fpsCam = Camera.main;
    }
    void Update()
    {

        // G button for throwable testing
        if (Input.GetKeyDown(KeyCode.G))
        {
            ThrowLethal();
        }
    }

    void ThrowLethal()
    {
        GameObject lethalPrefab = grenadePrefab;
        GameObject throwable = Instantiate(lethalPrefab, throwPoint.position, fpsCam.transform.rotation);
        Rigidbody rb = throwable.GetComponent<Rigidbody>();
        rb.AddForce(fpsCam.transform.forward * 40f, ForceMode.Impulse);
        throwable.GetComponent<Throwable>().hasBeenThrown = true;

        if (Physics.Raycast(fpsCam.transform.position, fpsCam.transform.forward, out RaycastHit hit, range))
        {
            Debug.Log("Throwing towards: " + hit.collider.name);
        }
    }
}