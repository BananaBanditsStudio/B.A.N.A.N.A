using UnityEngine;
using UnityEngine.SceneManagement;

public class TitleScreenManager : MonoBehaviour
{
    void Start()
    {
        // Ensure cursor is unlocked and visible for menu interaction
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        
        // Reset game state to ensure no input blocking
        GameStateManager gameStateManager = GameStateManager.Instance;
        if (gameStateManager != null)
        {
            gameStateManager.ResetState();
        }
        
        // Ensure time scale is normal
        Time.timeScale = 1f;
        
        Debug.Log("TitleScreenManager: TitleScreen initialized - cursor unlocked and game state reset");
    }
    
    void Update()
    {
        // Keep cursor unlocked while on title screen
        if (Cursor.lockState != CursorLockMode.None)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }
}
