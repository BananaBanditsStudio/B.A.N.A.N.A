using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class WeaponUI : MonoBehaviour
{
    [Header("UI Elements")]
    public TextMeshProUGUI weaponNameText;
    public TextMeshProUGUI ammoText;
    public Image weaponIcon;

    private RaycastWeapon currentWeapon;
    private int lastAmmo = -1; // Track ammo to only update UI when it changes
    private int lastMagazineSize = -1;

    void Update()
    {
        if (currentWeapon == null) return;
        
        // Only update text when ammo or magazine size actually changes (performance optimization)
        if (currentWeapon.currentAmmo != lastAmmo || currentWeapon.magazineSize != lastMagazineSize)
        {
            lastAmmo = currentWeapon.currentAmmo;
            lastMagazineSize = currentWeapon.magazineSize;
            ammoText.text = lastAmmo + "/" + lastMagazineSize;
        }
    }

    public void SetWeapon(RaycastWeapon newWeapon)
    {
        currentWeapon = newWeapon;
        lastAmmo = -1; // Reset to force update
        lastMagazineSize = -1;
        UpdateWeaponInfo();
    }

    void UpdateWeaponInfo()
    {
        if (currentWeapon == null)
            return;

        weaponNameText.text = currentWeapon.gameObject.name.ToUpper();

        if (currentWeapon.weaponIcon != null)
        {
            weaponIcon.sprite = currentWeapon.weaponIcon;
            weaponIcon.enabled = true;
        }
        else
        {
            weaponIcon.enabled = false;
        }
    }
}
