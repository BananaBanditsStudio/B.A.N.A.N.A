using UnityEngine;

public class RaycastWeapon : MonoBehaviour
{
    [Header("Animation Recording")]
    [SerializeField] private AnimationClip animationClip;
    
    public Transform raycastDestination;
    public bool isFiring = false;
    
    public AnimationClip AnimationClip
    {
        get => animationClip;
        set => animationClip = value;
    }

    public void StartFiring()
    {
        isFiring = true;
    }

    public void UpdateFiring(float deltaTime)
    {
        // Implement weapon firing logic here
    }

    public void UpdateBullets(float deltaTime)
    {
        // Implement bullet update logic here
    }

    public void StopFiring()
    {
        isFiring = false;
    }
}
