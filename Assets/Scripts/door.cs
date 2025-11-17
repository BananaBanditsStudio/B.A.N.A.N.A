using UnityEngine;

public class Door : MonoBehaviour
{
    public GameObject lockpickPrefab;
    public Transform lockPoint;

    private GameObject lockInstance;
    private bool isUnlocked = false;

    void Update()
    {
        // Only check input if door is not unlocked
        if (Input.GetKeyDown(KeyCode.F) && !isUnlocked)
        {
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
                lockInstance = Instantiate(lockpickPrefab, lockPoint.position, lockPoint.rotation);
                lockInstance.transform.SetParent(lockPoint); // Optional: keeps it attached!
            }
            
            lockInstance.SetActive(true);
        }
    }

    // Call this from your LockPick script when unlocked
    public void UnlockDoor()
    {
        isUnlocked = true;
        // Play unlock animation, enable door opening, etc.
    }
}
