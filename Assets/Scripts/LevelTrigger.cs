using UnityEngine;
using UnityEngine.SceneManagement;

public class LevelTrigger : MonoBehaviour
{
    public string nextSceneName; //  type the name of the next scene in Inspector
    private bool canEnter = false;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            if (PlayerInventory.hasBanana) //  only if banana collected
            {
                Debug.Log("🚗 Player reached car! Loading next level...");
                SceneManager.LoadScene(nextSceneName);
            }
            else
            {
                Debug.Log("❌ Player needs the banana first!");
            }
        }
    }
}
