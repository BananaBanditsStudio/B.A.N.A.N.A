using UnityEngine;
using UnityEngine.SceneManagement;

public class GameStateManager : MonoBehaviour
{
    public static GameStateManager Instance { get; private set; }
    
    [Header("Game State")]
    public bool isPaused = false;
    public bool isGameOver = false;
    public bool inputBlocked = false;
    
    [Header("Input Blocking")]
    public bool blockMovement = true;
    public bool blockShooting = true;
    public bool blockInteraction = true;
    public bool blockWeaponSwitching = true;
    
    void Awake()
    {
        // Singleton pattern
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    void Start()
    {
        // Initialize state
        ResetState();
    }
    
    void OnEnable()
    {
        // Reset state when the scene loads
        ResetState();
        
        // Subscribe to scene loading events
        SceneManager.sceneLoaded += OnSceneLoaded;
    }
    
    void OnDisable()
    {
        // Unsubscribe from scene loading events
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }
    
    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Reset state when a new scene is loaded
        ResetState();
    }
    
    public void SetPaused(bool paused)
    {
        isPaused = paused;
        inputBlocked = paused;
        
        if (paused)
        {
            Time.timeScale = 0f;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        else
        {
            Time.timeScale = 1f;
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }
    
    public void SetGameOver(bool gameOver)
    {
        isGameOver = gameOver;
        inputBlocked = gameOver;
        
        if (gameOver)
        {
            Time.timeScale = 0f;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }
    
    // Block input without freezing time (for death animation)
    public void BlockInput(bool block)
    {
        inputBlocked = block;
    }
    
    // Input checking methods
    public bool CanMove()
    {
        return !inputBlocked && !isPaused && !isGameOver && blockMovement;
    }
    
    public bool CanShoot()
    {
        return !inputBlocked && !isPaused && !isGameOver && blockShooting;
    }
    
    public bool CanInteract()
    {
        return !inputBlocked && !isPaused && !isGameOver && blockInteraction;
    }
    
    public bool CanSwitchWeapons()
    {
        return !inputBlocked && !isPaused && !isGameOver && blockWeaponSwitching;
    }
    
    // General input check
    public bool IsInputAllowed()
    {
        return !inputBlocked && !isPaused && !isGameOver;
    }
    
    // Static helper methods for input scripts
    public static bool CanMoveStatic()
    {
        if (Instance == null) return true; // Allow input if no GameStateManager
        return Instance.CanMove();
    }
    
    public static bool CanShootStatic()
    {
        if (Instance == null) return true; // Allow input if no GameStateManager
        return Instance.CanShoot();
    }
    
    public static bool CanInteractStatic()
    {
        if (Instance == null) return true; // Allow input if no GameStateManager
        return Instance.CanInteract();
    }
    
    public static bool IsInputAllowedStatic()
    {
        if (Instance == null) return true; // Allow input if no GameStateManager
        return Instance.IsInputAllowed();
    }
    
    // Reset state (useful for scene transitions)
    public void ResetState()
    {
        isPaused = false;
        isGameOver = false;
        inputBlocked = false;
        Time.timeScale = 1f;
        
        // Reset input flags to allow input (these flags are confusingly named - they should be true to allow input)
        blockMovement = true;
        blockShooting = true;
        blockInteraction = true;
        blockWeaponSwitching = true;
        
        // Don't force cursor state here - let individual scenes handle their own cursor state
        // This was causing issues with gameplay scenes
        
        // Debug log to confirm reset
        Debug.Log("GameStateManager: State reset - All input should be enabled");
    }
    
    // Debug method to check current state
    public void LogCurrentState()
    {
        Debug.Log($"GameStateManager State - Paused: {isPaused}, GameOver: {isGameOver}, InputBlocked: {inputBlocked}");
    }
}
