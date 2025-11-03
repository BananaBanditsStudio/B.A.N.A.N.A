using UnityEngine;

public class Door : MonoBehaviour
{
    public GameObject lockpickPrefab;
    public Transform lockPoint;

    private GameObject lockInstance;
    private bool isUnlocked = false;

    void Update()
    {
        Debug.Log("Door Update running");
        if (Input.GetKeyDown(KeyCode.F))
        {
            Debug.Log("F key is pressed");
        }

        if (Input.GetKeyDown(KeyCode.F) && !isUnlocked)
        {
            Debug.Log("F key pressed!");

            if (lockpickPrefab == null)
            {
                Debug.LogError("Lockpick Prefab NOT assigned!");
                return;
            }
            if (lockPoint == null)
            {
                Debug.LogError("Lock Point NOT assigned!");
                return;
            }

            if (lockInstance == null)
            {
                Debug.Log("Instantiating LockPick prefab");
                lockInstance = Instantiate(lockpickPrefab, lockPoint.position, lockPoint.rotation);
                lockInstance.transform.SetParent(lockPoint); // Optional: keeps it attached!
            }
            else
            {
                Debug.Log("LockPick prefab already exists, just activating");
            }
            lockInstance.SetActive(true);
        }
    }

    // Call this from your LockPick script when unlocked
    public void UnlockDoor()
    {
        Debug.Log("Door unlocked via lockpick!");
        isUnlocked = true;
        // Play unlock animation, enable door opening, etc.
    }
}
