using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class ObjectiveUI : MonoBehaviour
{
    public Image objectiveImage;

    // White versions in order
    public Sprite[] objectiveSprites;

    // Green completed versions in the same order
    public Sprite[] completedSprites;

    public float completedDisplayDuration = 1.0f;

    int currentIndex = 0;
    bool allDone = false;
    bool isCompleting = false; // Prevent multiple coroutines from running

    void Start()
    {
        if (objectiveImage == null || objectiveSprites == null || objectiveSprites.Length == 0)
        {
            return;
        }
        ShowCurrentObjective();
    }

    void ShowCurrentObjective()
    {
        if (objectiveImage == null || objectiveSprites == null || objectiveSprites.Length == 0)
        {
            return;
        }

        if (currentIndex < objectiveSprites.Length && objectiveSprites[currentIndex] != null)
        {
            objectiveImage.sprite = objectiveSprites[currentIndex];
            objectiveImage.enabled = true;
        }
        else if (currentIndex >= objectiveSprites.Length)
        {
            objectiveImage.enabled = false;
            allDone = true;
        }
    }

    public void MarkObjectiveComplete()
    {
        if (allDone || objectiveImage == null || isCompleting)
            return;

        StartCoroutine(CompleteThenNext());
    }

    IEnumerator CompleteThenNext()
    {
        isCompleting = true;

        // swap to green version if provided
        if (completedSprites != null &&
            currentIndex < completedSprites.Length &&
            completedSprites[currentIndex] != null &&
            objectiveImage != null)
        {
            objectiveImage.sprite = completedSprites[currentIndex];
        }

        // keep green image on screen for a moment
        yield return new WaitForSeconds(completedDisplayDuration);

        // move to next objective
        currentIndex++;
        ShowCurrentObjective();
        
        isCompleting = false;
    }
}
