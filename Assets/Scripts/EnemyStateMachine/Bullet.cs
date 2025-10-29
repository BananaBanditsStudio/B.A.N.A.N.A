using UnityEngine;

public class Bullet : MonoBehaviour
{
    public float damage = 10f;

    private void OnCollisionEnter(Collision collision) {
        Transform hitTransform = collision.transform;
        if (hitTransform.CompareTag("Player")) {
            Debug.Log("Hit player!");
            if (hitTransform.GetComponent<PlayerHealth>() != null) {
                hitTransform.GetComponent<PlayerHealth>().TakeDamage(damage);
            }
        }

        Destroy(gameObject);
    }
}
