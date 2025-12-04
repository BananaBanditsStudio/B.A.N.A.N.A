using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
public class MainMenu : MonoBehaviour
{
    [Header("UI References")]
    public GameObject skipTutorialPanel; // The SkipTutorial empty GameObject
    public GameObject mainMenuUI; // The main menu UI to hide (assign in inspector)

    void Start()
    {
        // Ensure SkipTutorial panel is hidden initially
        if (skipTutorialPanel != null)
        {
            skipTutorialPanel.SetActive(false);
        }
    }

    public void PlayGame()
    {
        // Show SkipTutorial panel and hide main menu
        if (skipTutorialPanel != null)
        {
            skipTutorialPanel.SetActive(true);
        }

        if (mainMenuUI != null)
        {
            mainMenuUI.SetActive(false);
        }
    }

    public void PlayTutorial()
    {
        // No button - Load the tutorial scene
        SceneManager.LoadScene("Tutorial");
    }

    public void SkipTutorial()
    {
        // Yes button - Skip tutorial and go directly to Mission #1
        SceneManager.LoadScene("Mission #1");
    }

    public void QuitGame()
    {
        Application.Quit(); // Quit the game
    }
}
