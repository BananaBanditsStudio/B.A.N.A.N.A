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
    private bool isActivated = false;

    void Start()
    {
        // Subscribe to vault secrets objective completion
        if (GameObjectiveManager.Instance != null)
        {
            GameObjectiveManager.Instance.OnVaultSecretsObjectiveComplete += ActivateMarker;
            
            // Check if already complete
            if (GameObjectiveManager.Instance.IsVaultSecretsObjectiveComplete())
            {
                ActivateMarker();
            }
        }
        else
        {
            // No manager, just activate immediately
            ActivateMarker();
        }
    }
    
    void ActivateMarker()
    {
        if (isActivated) return;
        isActivated = true;
        
        CreateGlowingCylinder();
    }
    
    void OnDestroy()
    {
        // Unsubscribe to prevent memory leaks
        if (GameObjectiveManager.Instance != null)
        {
            GameObjectiveManager.Instance.OnVaultSecretsObjectiveComplete -= ActivateMarker;
        }
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

        // Create glowing material - use Particles/Additive for best halo effect
        Renderer rend = cylinderVisual.GetComponent<Renderer>();
        
        // Particles/Additive gives the best transparent glow/halo effect
        Shader glowShader = Shader.Find("Particles/Standard Unlit");
        if (glowShader == null)
            glowShader = Shader.Find("Legacy Shaders/Particles/Additive");
        if (glowShader == null)
            glowShader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        if (glowShader == null)
            glowShader = Shader.Find("Sprites/Default");
        
        cylinderMaterial = new Material(glowShader);
        
        // Set color
        cylinderMaterial.SetColor("_Color", glowColor);
        if (cylinderMaterial.HasProperty("_TintColor"))
            cylinderMaterial.SetColor("_TintColor", glowColor);
        if (cylinderMaterial.HasProperty("_BaseColor"))
            cylinderMaterial.SetColor("_BaseColor", glowColor);
        
        // Set to Additive blending for glow effect
        cylinderMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        cylinderMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.One);
        cylinderMaterial.SetInt("_ZWrite", 0);
        cylinderMaterial.DisableKeyword("_ALPHATEST_ON");
        cylinderMaterial.EnableKeyword("_ALPHABLEND_ON");
        cylinderMaterial.renderQueue = 3000;
        
        // Disable culling so it's visible from inside too
        cylinderMaterial.SetInt("_Cull", (int)UnityEngine.Rendering.CullMode.Off);
        
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
        
        // Update color on material
        cylinderMaterial.SetColor("_Color", c);
        if (cylinderMaterial.HasProperty("_TintColor"))
            cylinderMaterial.SetColor("_TintColor", c);
        if (cylinderMaterial.HasProperty("_BaseColor"))
            cylinderMaterial.SetColor("_BaseColor", c);

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
        Color startColor = glowColor;

        while (elapsed < duration && cylinderMaterial != null)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            Color c = startColor;
            c.a = Mathf.Lerp(startColor.a, 0f, t);
            
            cylinderMaterial.SetColor("_Color", c);
            if (cylinderMaterial.HasProperty("_TintColor"))
                cylinderMaterial.SetColor("_TintColor", c);
            if (cylinderMaterial.HasProperty("_BaseColor"))
                cylinderMaterial.SetColor("_BaseColor", c);
            
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

