using UnityEngine;
using TMPro;

public class DamagePopup : MonoBehaviour
{
    private TextMeshPro damageText;
    private float disappearTimer;
    private Color textColor;
    private Color outlineColor;
    private Camera cameraMain;

    public float moveSpeed = 2f;

    void Awake()
    {
        damageText = GetComponent<TextMeshPro>();
        if (damageText == null)
        {
            Debug.LogError("DamagePopup: TextMeshPro component not found!");
        }
    }

    void Start()
    {
        cameraMain = Camera.main;
        if (cameraMain == null)
        {
            cameraMain = FindObjectOfType<Camera>();
        }
        disappearTimer = 0.2f;
        if (damageText != null)
        {
            textColor = damageText.color;
            outlineColor = damageText.outlineColor;
        }
    }


    public void Setup(float damage, bool isCriticalHit)
    {
        if (damageText == null)
        {
            damageText = GetComponent<TextMeshPro>();
        }
        
        if (damageText != null)
        {
            damageText.text = damage.ToString("0");
            
            // Set color based on critical hit
            if (isCriticalHit)
            {
                textColor = Color.red;
                outlineColor = Color.red; // Make outline red as well for critical hits
            }
            else
            {
                textColor = damageText.color; // Use default/prefab color for normal hits
                outlineColor = damageText.outlineColor; // Keep default outline color
            }
            
            textColor.a = 1f;
            outlineColor.a = 1f;
            damageText.color = textColor;
            damageText.outlineColor = outlineColor;
        }
        else
        {
            Debug.LogError("DamagePopup: Cannot setup - TextMeshPro component is null!");
        }
    }

    void Update()
    {
        if (damageText == null) return;
        
        // Move upward (slower movement)
        transform.position += new Vector3(0, moveSpeed, 0) * Time.deltaTime;
        
        // Face the camera (billboard effect)
        if (cameraMain != null)
        {
            transform.rotation = Quaternion.LookRotation(transform.position - cameraMain.transform.position);
        }
        
        disappearTimer -= Time.deltaTime;
        if (disappearTimer < 0)
        {
            float disappearSpeed = 3f;
            textColor.a -= disappearSpeed * Time.deltaTime;
            outlineColor.a -= disappearSpeed * Time.deltaTime;
            if (textColor.a < 0)
            {
                textColor.a = 0;
            }
            if (outlineColor.a < 0)
            {
                outlineColor.a = 0;
            }
            damageText.color = textColor;
            damageText.outlineColor = outlineColor;
            if (textColor.a <= 0)
            {
                Destroy(gameObject);
            }
        }
    }
}
