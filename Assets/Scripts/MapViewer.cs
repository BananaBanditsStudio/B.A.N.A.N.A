using UnityEngine;
using UnityEngine.UI;

public class MapViewer : MonoBehaviour
{
    [Header("Map UI")]
    [Tooltip("The full map UI panel (should be a child of Canvas, initially disabled)")]
    public GameObject fullMapPanel;
    
    [Tooltip("The RawImage component in the full map panel (will auto-detect if not set)")]
    public RawImage fullMapRawImage;
    
    [Header("Minimap Camera")]
    [Tooltip("The minimap camera (optional - if set, will adjust orthographic size for full map)")]
    public Camera minimapCamera;
    
    [Header("Full Map Settings")]
    [Tooltip("Orthographic size for full map view (larger = shows more area)")]
    public float fullMapOrthographicSize = 100f;
    
    [Tooltip("Original orthographic size (for minimap) - will be auto-detected if not set")]
    public float minimapOrthographicSize = 30f;
    
    [Tooltip("Render texture width for full map (higher = less blurry, but more performance cost)")]
    public int fullMapTextureWidth = 1024;
    
    [Tooltip("Render texture height for full map (higher = less blurry, but more performance cost)")]
    public int fullMapTextureHeight = 768;
    
    [Header("Input")]
    [Tooltip("Key to toggle full map")]
    public KeyCode toggleKey = KeyCode.M;
    
    [Header("Game State")]
    [Tooltip("Pause game when map is open?")]
    public bool pauseGameWhenOpen = false;
    
    private bool isMapOpen = false;
    private bool originalMinimapSizeSet = false;
    private RenderTexture minimapRenderTexture;
    private RenderTexture fullMapRenderTexture;
    private RenderTexture originalRenderTexture;
    
    private void Start()
    {
        // Auto-detect minimap camera if not assigned
        if (minimapCamera == null)
        {
            GameObject minimapCamObj = GameObject.Find("MinimapCamera");
            if (minimapCamObj != null)
            {
                minimapCamera = minimapCamObj.GetComponent<Camera>();
            }
        }
        
        // Get the render texture from minimap camera
        if (minimapCamera != null)
        {
            minimapRenderTexture = minimapCamera.targetTexture;
            originalRenderTexture = minimapRenderTexture;
            
            // Create a higher resolution render texture for full map
            if (minimapRenderTexture != null)
            {
                fullMapRenderTexture = new RenderTexture(fullMapTextureWidth, fullMapTextureHeight, minimapRenderTexture.depth, minimapRenderTexture.format);
                fullMapRenderTexture.name = "FullMapRenderTexture";
            }
            
            // Store original minimap size
            if (!originalMinimapSizeSet)
            {
                minimapOrthographicSize = minimapCamera.orthographicSize;
                originalMinimapSizeSet = true;
            }
        }
        
        // Auto-detect full map RawImage if not assigned
        if (fullMapRawImage == null && fullMapPanel != null)
        {
            fullMapRawImage = fullMapPanel.GetComponent<RawImage>();
            if (fullMapRawImage == null)
            {
                fullMapRawImage = fullMapPanel.GetComponentInChildren<RawImage>();
            }
        }
        
        // If still no RawImage found, try to find the minimap RawImage to copy its texture
        if (fullMapRawImage == null)
        {
            GameObject minimapUI = GameObject.Find("Minimap");
            if (minimapUI != null)
            {
                RawImage minimapRawImage = minimapUI.GetComponent<RawImage>();
                if (minimapRawImage != null && minimapRawImage.texture != null)
                {
                    minimapRenderTexture = minimapRawImage.texture as RenderTexture;
                }
            }
        }
        
        // Setup full map RawImage with high-res render texture
        if (fullMapRawImage != null && fullMapRenderTexture != null)
        {
            fullMapRawImage.texture = fullMapRenderTexture;
        }
        else if (fullMapRawImage == null)
        {
            Debug.LogWarning("MapViewer: Full map RawImage not found! Make sure the full map panel has a RawImage component.");
        }
        else if (fullMapRenderTexture == null)
        {
            Debug.LogWarning("MapViewer: Full map RenderTexture could not be created! Make sure the minimap camera has a target texture assigned.");
        }
        
        // Ensure full map panel starts disabled
        if (fullMapPanel != null)
        {
            fullMapPanel.SetActive(false);
        }
    }
    
    private void Update()
    {
        // Toggle map on key press
        if (Input.GetKeyDown(toggleKey))
        {
            ToggleMap();
        }
        
        // Close map on Escape (optional)
        if (isMapOpen && Input.GetKeyDown(KeyCode.Escape))
        {
            CloseMap();
        }
    }
    
    public void ToggleMap()
    {
        if (isMapOpen)
        {
            CloseMap();
        }
        else
        {
            OpenMap();
        }
    }
    
    public void OpenMap()
    {
        isMapOpen = true;
        
        // Show full map panel
        if (fullMapPanel != null)
        {
            fullMapPanel.SetActive(true);
        }
        
        // Switch camera to high-res render texture and adjust size
        if (minimapCamera != null)
        {
            // Switch to high-resolution render texture for full map
            if (fullMapRenderTexture != null)
            {
                minimapCamera.targetTexture = fullMapRenderTexture;
            }
            
            // Adjust minimap camera to show more area
            minimapCamera.orthographicSize = fullMapOrthographicSize;
        }
        
        // Pause game if enabled
        if (pauseGameWhenOpen)
        {
            Time.timeScale = 0f;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }
    
    public void CloseMap()
    {
        isMapOpen = false;
        
        // Hide full map panel
        if (fullMapPanel != null)
        {
            fullMapPanel.SetActive(false);
        }
        
        // Restore minimap camera size and render texture
        if (minimapCamera != null)
        {
            // Switch back to original low-res render texture for minimap
            if (originalRenderTexture != null)
            {
                minimapCamera.targetTexture = originalRenderTexture;
            }
            
            // Restore minimap camera size
            minimapCamera.orthographicSize = minimapOrthographicSize;
        }
        
        // Resume game if paused
        if (pauseGameWhenOpen)
        {
            Time.timeScale = 1f;
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }
    
    private void OnDestroy()
    {
        // Cleanup full map render texture
        if (fullMapRenderTexture != null)
        {
            fullMapRenderTexture.Release();
            Destroy(fullMapRenderTexture);
        }
    }
    
    public bool IsMapOpen()
    {
        return isMapOpen;
    }
}

