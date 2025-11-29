using UnityEngine;

public class Bullet : MonoBehaviour
{
    public float damage = 10f;
    public float speed = 40f;
    public float spinSpeed = 1000f; // Degrees per second
    
    [HideInInspector] public Vector3 targetPosition;
    [HideInInspector] public bool hasTarget = false;
    
    private float spawnTime;
    private const float COLLISION_DELAY = 0.1f;
    private Rigidbody rb;
    private bool velocitySet = false;
    private int frameCount = 0;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        spawnTime = Time.time;
    }

    private void FixedUpdate()
    {
        if (!velocitySet && hasTarget && rb != null)
        {
            rb.useGravity = false;
            rb.constraints = RigidbodyConstraints.None;
            rb.isKinematic = false;
            Vector3 direction = (targetPosition - transform.position).normalized;
            rb.linearVelocity = direction * speed;
            velocitySet = true;
        }
        
        // Keep forcing velocity for first 10 frames to overcome any constraints
        if (velocitySet && hasTarget && rb != null && frameCount < 10)
        {
            frameCount++;
            Vector3 direction = (targetPosition - transform.position).normalized;
            rb.linearVelocity = direction * speed;
        }
    }

    private void Update()
    {
        // Spin while travelling
        if (velocitySet)
        {
            transform.Rotate(Vector3.forward, spinSpeed * Time.deltaTime);
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (Time.time - spawnTime < COLLISION_DELAY)
        {
            return;
        }
        
        Transform hitTransform = collision.transform;
        if (hitTransform.CompareTag("Player"))
        {
            if (hitTransform.GetComponent<PlayerHealth>() != null)
            {
                hitTransform.GetComponent<PlayerHealth>().TakeDamage(damage);
            }
        }

        Destroy(gameObject);
    }
}
