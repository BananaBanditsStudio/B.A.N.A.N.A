using UnityEngine;
using UnityEngine.UI; // only needed if you display count on screen

public class PlayerInventory : MonoBehaviour
{
    public int bananaCount = 0;
    public Text bananaText; // Optional UI element

    public void AddBananas(int amount)
    {
        bananaCount += amount;
        Debug.Log("Bananas stolen: " + bananaCount);

        if (bananaText != null)
        {
            bananaText.text = "🍌 " + bananaCount;
        }
    }
}
