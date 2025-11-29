using UnityEngine;
using UnityEngine.UI;
public class PlayerHealth : MonoBehaviour
{
    [SerializeField] private float health;
    private float lerpTimer;
    [Header("Health")]
    [SerializeField]
    public float maxHealth = 100;
    [SerializeField]
    public float chipSpeed = 2;

    [SerializeField]
    public Image frontHealthBar;
    [SerializeField]
    public Image backHealthBar;


    [Header("Damage Overlay")]
    public Image overlay;
    public float duration;
    public float fadeSpeed;

    [Header("Death")]
    public Animator playerAnimator;
    public string deathTrigger = "Dead";
    private bool isDead = false;

    private float durationTimer;
    
    void Start()
    {
        health = maxHealth;
        overlay.color = new Color(overlay.color.r, overlay.color.g, overlay.color.b, 0f);
        
        if (playerAnimator == null)
        {
            // Get the animator component from the player
            playerAnimator = GetComponent<Animator>();
        }
    }

    // Update is called once per frame
    void Update()
    {
        health = Mathf.Clamp(health, 0, maxHealth);
        UpdateHealthUI();
        if (overlay.color.a > 0)
        {
            durationTimer += Time.deltaTime;
            if (durationTimer > duration)
            {
                float tempAlpha = overlay.color.a;
                tempAlpha -= fadeSpeed * Time.deltaTime;
                overlay.color = new Color(overlay.color.r, overlay.color.g, overlay.color.b, tempAlpha);
            }
        }
    }

    void UpdateHealthUI()
    {
        // Debug.Log("Updating Health UI: " + health);
        float fillF = frontHealthBar.fillAmount;
        float fillB = backHealthBar.fillAmount;
        float hFraction = health / maxHealth;

        if (fillB > hFraction)
        {
            // Taking Damage - front bar drops instantly, back bar lerps down
            frontHealthBar.fillAmount = hFraction;
            backHealthBar.color = Color.red;
            lerpTimer += Time.deltaTime;
            float percentComplete = lerpTimer / chipSpeed;
            backHealthBar.fillAmount = Mathf.Lerp(fillB, hFraction, percentComplete);
        }

        if (fillF < hFraction)
        {
            // Healing - back bar jumps up instantly, front bar lerps up
            backHealthBar.fillAmount = hFraction;
            backHealthBar.color = Color.green;
            lerpTimer += Time.deltaTime;
            float percentComplete = lerpTimer / chipSpeed;
            frontHealthBar.fillAmount = Mathf.Lerp(fillF, hFraction, percentComplete);
        }
    }

    public void TakeDamage(float damage)
    {
        health -= damage;
        lerpTimer = 0f;
        // Flash the image
        durationTimer = 0f;
        overlay.color = new Color(overlay.color.r, overlay.color.g, overlay.color.b, 0.4f);
    }


    public void Heal(float healAmount)
    {
        health += healAmount;
        health = Mathf.Clamp(health, 0, maxHealth);
        lerpTimer = 0f;
    }
    
    // Public getter for health value
    public float GetHealth()
    {
        return health;
    }
    
    // Public getter for health property (for GameOverManager)
    public float Health
    {
        get { return health; }
    }
    
    public bool IsDead
    {
        get { return isDead; }
    }
    
    public void TriggerDeath()
    {
        if (isDead) return;
        isDead = true;
        
        if (playerAnimator != null)
        {
            playerAnimator.SetBool(deathTrigger, true);
            playerAnimator.SetTrigger(deathTrigger);
        }
    }
}
