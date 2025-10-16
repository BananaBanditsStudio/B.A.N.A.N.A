using UnityEngine;
using UnityEngine.UI;

public class WeaponSwitcher : MonoBehaviour
{
    [Header("Weapons")]
    public GameObject[] weapons; // Array of weapon GameObjects (Gun, BananaPeelThrower, etc.)

    [System.Serializable]
    public class WeaponData
    {
        public GameObject weapon;
        public GameObject[] arms;
    }

    [Header("Weapon Arms")]
    public WeaponData[] weaponData; // Array of weapon data (weapon + arms)

    [Header("UI Slots")]
    public Image[] weaponSlots; // Array of UI Image components for weapon slots

    [Header("Highlight Settings")]
    public Color highlightColor = Color.yellow;
    public Color normalColor = Color.white;

    private int currentWeaponIndex = 0;

    void Start()
    {
        // Initialize - activate first weapon and deactivate others
        SwitchToWeapon(0);
    }

    void Update()
    {
        // Number keys 1-9 for weapon switching
        if (Input.GetKeyDown(KeyCode.Alpha1)) SwitchToWeapon(0);
        if (Input.GetKeyDown(KeyCode.Alpha2)) SwitchToWeapon(1);
        if (Input.GetKeyDown(KeyCode.Alpha3)) SwitchToWeapon(2);
        if (Input.GetKeyDown(KeyCode.Alpha4)) SwitchToWeapon(3);
        if (Input.GetKeyDown(KeyCode.Alpha5)) SwitchToWeapon(4);

        // Mouse scroll wheel for cycling weapons
        if (Input.GetAxis("Mouse ScrollWheel") > 0f)
        {
            // Scroll up - next weapon
            SwitchToWeapon((currentWeaponIndex + 1) % weapons.Length);
        }
        else if (Input.GetAxis("Mouse ScrollWheel") < 0f)
        {
            // Scroll down - previous weapon
            int newIndex = currentWeaponIndex - 1;
            if (newIndex < 0) newIndex = weapons.Length - 1;
            SwitchToWeapon(newIndex);
        }
    }

    void SwitchToWeapon(int index)
    {
        // Validate index
        if (index < 0 || index >= weapons.Length) return;

        // Recover any slipping enemies when switching weapons
        if (SlippingRecoveryManager.Instance != null)
        {
            SlippingRecoveryManager.Instance.ForceRecoverAllSlippingEnemies();
        }

        // Deactivate all weapons and their corresponding arms
        for (int i = 0; i < weapons.Length; i++)
        {
            if (weapons[i] != null)
            {
                weapons[i].SetActive(false);
            }
            
            // Deactivate arms for this weapon using weaponData
            if (i < weaponData.Length && weaponData[i] != null && weaponData[i].arms != null)
            {
                for (int armIndex = 0; armIndex < weaponData[i].arms.Length; armIndex++)
                {
                    if (weaponData[i].arms[armIndex] != null)
                    {
                        weaponData[i].arms[armIndex].SetActive(false);
                    }
                }
            }
        }

        // Activate selected weapon
        if (weapons[index] != null)
        {
            weapons[index].SetActive(true);
            currentWeaponIndex = index;
        }

        // Activate arms for the selected weapon using weaponData
        if (index < weaponData.Length && weaponData[index] != null && weaponData[index].arms != null)
        {
            for (int armIndex = 0; armIndex < weaponData[index].arms.Length; armIndex++)
            {
                if (weaponData[index].arms[armIndex] != null)
                {
                    weaponData[index].arms[armIndex].SetActive(true);
                }
            }
        }

        // Update UI highlighting
        UpdateUIHighlight();
    }

    void UpdateUIHighlight()
    {
        // Reset all slots to normal color
        for (int i = 0; i < weaponSlots.Length; i++)
        {
            if (weaponSlots[i] != null)
            {
                weaponSlots[i].color = normalColor;
            }
        }

        // Highlight current weapon slot
        if (currentWeaponIndex < weaponSlots.Length && weaponSlots[currentWeaponIndex] != null)
        {
            weaponSlots[currentWeaponIndex].color = highlightColor;
        }
    }
}
