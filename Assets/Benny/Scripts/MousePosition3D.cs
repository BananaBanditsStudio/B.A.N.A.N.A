using UnityEngine;

public class MousePosition3D : MonoBehaviour
{
    [Header("Camera Settings")]
    [Tooltip("The camera to use for mouse position tracking. If null, will use Camera.main")]
    public Camera targetCamera;
    
    [Header("Tracking Settings")]
    [Tooltip("Distance from camera where the object will follow the mouse")]
    public float distanceFromCamera = 10f;
    
    [Tooltip("Enable smooth following movement")]
    public bool smoothFollow = true;
    
    [Tooltip("Speed of smooth following (higher = faster)")]
    public float smoothSpeed = 10f;
    
    [Header("Alternative: Plane Tracking")]
    [Tooltip("Use a specific Y position instead of distance from camera")]
    public bool useFixedYPosition = false;
    
    [Tooltip("Fixed Y position to track on (only if useFixedYPosition is true)")]
    public float fixedYPosition = 0f;

    private void Start()
    {
        // If no camera is assigned, use the main camera
        if (targetCamera == null)
        {
            targetCamera = Camera.main;
            
            if (targetCamera == null)
            {
                Debug.LogError("MousePosition3D: No camera found! Please assign a camera or tag one as MainCamera.");
            }
        }
    }

    private void Update()
    {
        if (targetCamera == null) return;
        
        Vector3 targetPosition = GetMouseWorldPosition();
        
        // Move the object to follow the mouse
        if (smoothFollow)
        {
            // Smooth interpolation
            transform.position = Vector3.Lerp(transform.position, targetPosition, smoothSpeed * Time.deltaTime);
        }
        else
        {
            // Instant following
            transform.position = targetPosition;
        }
    }
    
    private Vector3 GetMouseWorldPosition()
    {
        // Get mouse position in screen space
        Vector3 mouseScreenPosition = Input.mousePosition;
        
        if (useFixedYPosition)
        {
            // Calculate position on a fixed Y plane
            return GetMousePositionOnPlane(fixedYPosition);
        }
        else
        {
            // Set the Z distance from camera
            mouseScreenPosition.z = distanceFromCamera;
            
            // Convert screen position to world position
            return targetCamera.ScreenToWorldPoint(mouseScreenPosition);
        }
    }
    
    private Vector3 GetMousePositionOnPlane(float yPosition)
    {
        // Create a ray from camera through mouse position
        Ray ray = targetCamera.ScreenPointToRay(Input.mousePosition);
        
        // Create a plane at the specified Y position
        Plane plane = new Plane(Vector3.up, new Vector3(0, yPosition, 0));
        
        // Raycast to the plane
        if (plane.Raycast(ray, out float distance))
        {
            return ray.GetPoint(distance);
        }
        
        // Fallback to current position if raycast fails
        return transform.position;
    }
}
