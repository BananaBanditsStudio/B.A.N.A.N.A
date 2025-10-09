using UnityEngine;

public class FootStepSystem : MonoBehaviour
{
    public AudioSource audioSource;

    [Header("Footstep Sounds")]
    public AudioClip concrete;
    public AudioClip grass;
    public AudioClip dirt;
    public AudioClip rock;

    [Header("Raycast Settings")]
    public Transform rayStart;
    public float range = 1f;
    public LayerMask layerMask;

    private RaycastHit hit;

    public void Footstep()
    {
        if (Physics.Raycast(rayStart.position, Vector3.down, out hit, range, layerMask))
        {
            if (hit.collider.CompareTag("concrete"))
                PlayFootstep(concrete);
            else if (hit.collider.CompareTag("grass"))
                PlayFootstep(grass);
            else if (hit.collider.CompareTag("dirt"))
                PlayFootstep(dirt);
            else if (hit.collider.CompareTag("rock"))
                PlayFootstep(rock);
        }
    }

    void PlayFootstep(AudioClip clip)
    {
        audioSource.pitch = Random.Range(0.8f, 1f);
        audioSource.volume = Random.Range(0.9f, 1f);
        audioSource.PlayOneShot(clip);
    }
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.F))
            Footstep();
    }

}
