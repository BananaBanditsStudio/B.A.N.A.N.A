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

    void Update()
    {
        if (currentWeapon == null) return;
        ammoText.text = currentWeapon.currentAmmo + "/" + currentWeapon.magazineSize;
    }

    public void SetWeapon(RaycastWeapon newWeapon)
    {
        currentWeapon = newWeapon;
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
