using UnityEngine;

public class VaultMarker : MonoBehaviour
{
    [Header("Cylinder Settings")]
    [SerializeField] private float cylinderHeight = 10f;
    [SerializeField] private float cylinderRadius = 2f;
    [SerializeField] private Color glowColor = new Color(1f, 0.8f, 0f, 0.3f);

    [Header("Animation")]
    [SerializeField] private float pulseSpeed = 2f;
    [SerializeField] private float minAlpha = 0.1f;
    [SerializeField] private float maxAlpha = 0.4f;
    [SerializeField] private float rotationSpeed = 30f;

    [Header("Detection")]
    [SerializeField] private float triggerRadius = 3f;
    [SerializeField] private string feedbackMessage = "You found the bank vault!";

    private GameObject cylinderVisual;
    private Material cylinderMaterial;
    private bool hasTriggered = false;

    void Start()
    {
        CreateGlowingCylinder();
    }

    void CreateGlowingCylinder()
    {
        cylinderVisual = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        cylinderVisual.name = "VaultGlowCylinder";
        cylinderVisual.transform.SetParent(transform);
        cylinderVisual.transform.localPosition = new Vector3(0, cylinderHeight / 2f, 0);
        cylinderVisual.transform.localScale = new Vector3(cylinderRadius * 2f, cylinderHeight / 2f, cylinderRadius * 2f);

        // Remove collider from visual
        Collider col = cylinderVisual.GetComponent<Collider>();
        if (col != null) Destroy(col);

        // Create glowing material with URP shader
        Renderer rend = cylinderVisual.GetComponent<Renderer>();
        
        // Try URP Unlit shader first (best for glowing effects)
        Shader urpShader = Shader.Find("Universal Render Pipeline/Unlit");
        if (urpShader == null)
        {
            // Fallback to URP Lit if Unlit not found
            urpShader = Shader.Find("Universal Render Pipeline/Lit");
        }
        
        if (urpShader == null)
        {
            // Last resort: try to find any URP shader
            urpShader = Shader.Find("Shader Graphs/Unlit");
        }
        
        if (urpShader == null)
        {
            Debug.LogError("VaultMarker: Could not find URP shader! Make sure URP is set up correctly.");
            urpShader = Shader.Find("Sprites/Default"); // Fallback
        }
        
        cylinderMaterial = new Material(urpShader);
        
        // Set up transparency for URP
        if (cylinderMaterial.HasProperty("_Surface"))
        {
            cylinderMaterial.SetFloat("_Surface", 1); // Transparent
            cylinderMaterial.SetFloat("_Blend", 0); // Alpha
        }
        
        if (cylinderMaterial.HasProperty("_BaseColor"))
        {
            cylinderMaterial.SetColor("_BaseColor", glowColor);
        }
        else if (cylinderMaterial.HasProperty("_Color"))
        {
            cylinderMaterial.SetColor("_Color", glowColor);
        }
        
        // Set emission for glow effect
        if (cylinderMaterial.HasProperty("_EmissionColor"))
        {
            cylinderMaterial.EnableKeyword("_EMISSION");
            cylinderMaterial.SetColor("_EmissionColor", glowColor * 2f);
        }
        
        // Enable transparency
        cylinderMaterial.SetFloat("_ZWrite", 0);
        cylinderMaterial.renderQueue = 3000; // Transparent queue
        
        rend.material = cylinderMaterial;

        // Add trigger collider to this object
        SphereCollider trigger = gameObject.AddComponent<SphereCollider>();
        trigger.isTrigger = true;
        trigger.radius = triggerRadius;
    }

    void Update()
    {
        if (cylinderVisual == null || hasTriggered || cylinderMaterial == null) return;

        // Pulse alpha
        float alpha = Mathf.Lerp(minAlpha, maxAlpha, (Mathf.Sin(Time.time * pulseSpeed) + 1f) / 2f);
        Color c = glowColor;
        c.a = alpha;
        
        // Update color using URP property names
        if (cylinderMaterial.HasProperty("_BaseColor"))
        {
            cylinderMaterial.SetColor("_BaseColor", c);
            if (cylinderMaterial.HasProperty("_EmissionColor"))
                cylinderMaterial.SetColor("_EmissionColor", c * 2f);
        }
        else if (cylinderMaterial.HasProperty("_Color"))
        {
            cylinderMaterial.SetColor("_Color", c);
        }
        else
        {
            cylinderMaterial.color = c;
        }

        // Rotate
        cylinderVisual.transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime, Space.World);
    }

    void OnTriggerEnter(Collider other)
    {
        if (hasTriggered) return;

        if (other.CompareTag("Player"))
        {
            hasTriggered = true;
            GameObjectiveManager.FoundBankVault();

            PlayerUI ui = FindObjectOfType<PlayerUI>();
            if (ui != null)
                ui.ShowFeedback(feedbackMessage);

            // Fade out and destroy
            StartCoroutine(FadeOutAndDestroy());
        }
    }

    System.Collections.IEnumerator FadeOutAndDestroy()
    {
        float duration = 1f;
        float elapsed = 0f;
        
        Color startColor;
        if (cylinderMaterial.HasProperty("_BaseColor"))
            startColor = cylinderMaterial.GetColor("_BaseColor");
        else if (cylinderMaterial.HasProperty("_Color"))
            startColor = cylinderMaterial.GetColor("_Color");
        else
            startColor = cylinderMaterial.color;

        while (elapsed < duration && cylinderMaterial != null)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            Color c = startColor;
            c.a = Mathf.Lerp(startColor.a, 0f, t);
            
            if (cylinderMaterial.HasProperty("_BaseColor"))
            {
                cylinderMaterial.SetColor("_BaseColor", c);
                if (cylinderMaterial.HasProperty("_EmissionColor"))
                    cylinderMaterial.SetColor("_EmissionColor", c * 2f);
            }
            else if (cylinderMaterial.HasProperty("_Color"))
            {
                cylinderMaterial.SetColor("_Color", c);
            }
            else
            {
                cylinderMaterial.color = c;
            }
            
            yield return null;
        }

        if (cylinderVisual != null)
            Destroy(cylinderVisual);
    }

    void OnDrawGizmosSelected()
    {
        // Draw cylinder in editor
        Gizmos.color = new Color(1f, 0.8f, 0f, 0.3f);
        Vector3 center = transform.position + Vector3.up * (cylinderHeight / 2f);
        Gizmos.DrawWireSphere(center, cylinderRadius);
        Gizmos.DrawLine(transform.position, transform.position + Vector3.up * cylinderHeight);

        // Draw trigger radius
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, triggerRadius);
    }
}

