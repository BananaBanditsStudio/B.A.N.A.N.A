using UnityEngine;
using UnityEngine.SceneManagement;

public class LevelTrigger : MonoBehaviour
{
    public string nextSceneName; //  type the name of the next scene in Inspector
    private bool canEnter = false;
    private GameStateManager gameStateManager;

    void Start()
    {
        // Get or create GameStateManager
        gameStateManager = GameStateManager.Instance;
        if (gameStateManager == null)
        {
            GameObject gameStateGO = new GameObject("GameStateManager");
            gameStateManager = gameStateGO.AddComponent<GameStateManager>();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            if (PlayerInventory.hasBanana) //  only if banana collected
            {
                Debug.Log("🚗 Player reached car! Loading next level...");
                
                // Reset game state before scene change (especially important for TitleScreen)
                if (gameStateManager != null)
                {
                    gameStateManager.ResetState();
                }
                
                // Resume time scale before scene change
                Time.timeScale = 1f;
                
                // If loading TitleScreen, ensure cursor is unlocked for menu interaction
                if (nextSceneName.ToLower().Contains("title"))
                {
                    Cursor.lockState = CursorLockMode.None;
                    Cursor.visible = true;
                    Debug.Log("LevelTrigger: Preparing for TitleScreen - cursor unlocked");
                }
                
                SceneManager.LoadScene(nextSceneName);
            }
            else
            {
                Debug.Log("❌ Player needs the banana first!");
            }
        }
    }
}

