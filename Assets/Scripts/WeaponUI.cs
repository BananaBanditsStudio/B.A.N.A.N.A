using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class WeaponUI : MonoBehaviour
{
    [Header("UI Elements")]
    public TextMeshProUGUI weaponNameText;
    public TextMeshProUGUI ammoText;
    public Image weaponIcon;
    
    [Header("No Weapon State")]
    public GameObject weaponPanel;
    public TextMeshProUGUI noWeaponText;
    public string noWeaponMessage = "Press 'E' to interact";

    private RaycastWeapon currentWeapon;
    private int lastAmmo = -1;
    private int lastMagazineSize = -1;

    void Start()
    {
        // Show no weapon state initially
        ShowNoWeaponState();
    }

    void Update()
    {
        if (currentWeapon == null) return;
        
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
        lastAmmo = -1;
        lastMagazineSize = -1;
        
        if (currentWeapon != null)
        {
            ShowWeaponState();
            UpdateWeaponInfo();
        }
        else
        {
            ShowNoWeaponState();
        }
    }

    public void ClearWeapon()
    {
        currentWeapon = null;
        ShowNoWeaponState();
    }

    void ShowWeaponState()
    {
        if (weaponPanel != null) weaponPanel.SetActive(true);
        if (weaponNameText != null) weaponNameText.gameObject.SetActive(true);
        if (ammoText != null) ammoText.gameObject.SetActive(true);
        if (weaponIcon != null) weaponIcon.gameObject.SetActive(true);
        if (noWeaponText != null) noWeaponText.gameObject.SetActive(false);
    }

    void ShowNoWeaponState()
    {
        if (weaponPanel != null) weaponPanel.SetActive(false);
        if (weaponNameText != null) weaponNameText.gameObject.SetActive(false);
        if (ammoText != null) ammoText.gameObject.SetActive(false);
        if (weaponIcon != null) weaponIcon.gameObject.SetActive(false);
        
        if (noWeaponText != null)
        {
            noWeaponText.gameObject.SetActive(true);
            noWeaponText.text = noWeaponMessage;
        }
    }

    void UpdateWeaponInfo()
    {
        if (currentWeapon == null) return;

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
