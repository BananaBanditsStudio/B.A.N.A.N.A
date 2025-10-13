using UnityEngine;
using UnityEngine.UI;

public class WeaponSwitcher : MonoBehaviour
{
    [Header("Weapons")]
    public GameObject[] weapons; // Array of weapon GameObjects (Gun, BananaPeelThrower, etc.)

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

        // Deactivate all weapons
        for (int i = 0; i < weapons.Length; i++)
        {
            if (weapons[i] != null)
            {
                weapons[i].SetActive(false);
            }
        }

        // Activate selected weapon
        if (weapons[index] != null)
        {
            weapons[index].SetActive(true);
            currentWeaponIndex = index;
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
