using UnityEngine;
using UnityEngine.SceneManagement;

public class LevelTrigger : MonoBehaviour
{
    public string nextSceneName; //  type the name of the next scene in Inspector
    private bool canEnter = false;
    private GameStateManager gameStateManager;
    [SerializeField] private FadeTransition fadeTransition;


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
            if (PlayerInventory.hasBanana) // only if banana collected
            {
                Debug.Log("Player reached car! Starting fade transition...");

                // Reset game state before scene change (important if returning to TitleScreen)
                if (gameStateManager != null)
                    gameStateManager.ResetState();

                // Resume time scale before scene change
                Time.timeScale = 1f;

                // Unlock cursor for TitleScreen
                if (nextSceneName.ToLower().Contains("title"))
                {
                    Cursor.lockState = CursorLockMode.None;
                    Cursor.visible = true;
                    Debug.Log("Preparing for TitleScreen - cursor unlocked");
                }

                // 🚗 Trigger fade transition instead of instant load
                if (fadeTransition != null)
                {
                    fadeTransition.nextSceneName = nextSceneName; // just to be sure
                    fadeTransition.StartFadeOut();
                }
                else
                {
                    Debug.LogWarning("FadeTransition not assigned! Loading instantly.");
                    SceneManager.LoadScene(nextSceneName);
                }
            }
            else
            {
                Debug.Log("Player needs the banana first!");
            }
        }
    }

}

