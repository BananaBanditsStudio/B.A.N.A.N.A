using UnityEngine;

public class ObjectiveMarker : MonoBehaviour
{
    public Transform target; // The thing to track (banana or car)
    public float hideDistance = 2f;

    private RectTransform markerRect;
    private Camera cam;

    void Start()
    {
        markerRect = GetComponent<RectTransform>();
        cam = Camera.main;
    }

    void Update()
    {
        if (target == null || cam == null) return;

        Vector3 screenPos = cam.WorldToScreenPoint(target.position);

        // Hide if behind player or too close
        bool isBehind = screenPos.z < 0;
        float distance = Vector3.Distance(cam.transform.position, target.position);
        gameObject.SetActive(!isBehind && distance > hideDistance);

        // Position marker on screen
        markerRect.position = screenPos;
    }
}
