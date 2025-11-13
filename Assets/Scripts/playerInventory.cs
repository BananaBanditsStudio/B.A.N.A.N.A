using UnityEngine;
using UnityEngine.UI; // only needed if you display count on screen

public class PlayerInventory : MonoBehaviour
{
    public static bool hasBanana = false;
    public int bananaCount = 0;
    public static int keyCount = 0;

    public void AddBananas(int amount)
    {
        bananaCount += amount;
        hasBanana = true;
        Debug.Log("🍌 Banana added to inventory!");
    }
    public static bool hasKey = false;
}
