using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityTutorial.PlayerControl;

public class EnemyWithSM : MonoBehaviour
{

    private StateMachine stateMachine;
    public StateMachine StateMachine { get { return stateMachine; } }
    private NavMeshAgent agent;
    public NavMeshAgent Agent { get { return agent; } }
    public GameObject Player { get { return player; } }
    public Animator Animator { get { return animator; } }
    public Path2 path;

    [SerializeField]
    private string currentState;

    [Header("Player Reference")]
    [SerializeField] private GameObject playerReference;
    private GameObject player;
    private Animator animator;

    public float sightDistance = 20f;
    public float fieldOfView = 85;

    private float playerSearchCooldown = 0f;
    private const float PLAYER_SEARCH_INTERVAL = 1f;
    private bool hasLoggedPlayerNotFoundWarning = false;

    [Header("Attack Behavior")]
    public AttackBehaviorType attackBehaviorType = AttackBehaviorType.Throw;

    [Header("Ranged Attack Settings")]
    public float rangedFireRate = 2f;
    public GameObject bulletPrefab;
    public Transform gunBarrel;

    [Header("Melee Attack Settings")]
    public float meleeAttackRange = 2f;
    public float meleeAttackCooldown = 2f;
    public float meleeDamage = 10f;
    public float meleeDamageDelay = 1.3f;

    [Header("Charge Attack Settings")]
    public float chargeSpeed = 15f;
    public float explosionRange = 2f;
    public float explosionDamage = 50f;
    public GameObject explosionPrefab;

    [Header("Big Jump Attack Settings")]
    public float bigJumpRange = 8f;
    public float bigJumpCooldown = 5f;
    public float bigJumpDamage = 25f;
    public float bigJumpDamageDelay = 1.5f;
    public float bigJumpAOERadius = 4f;
    public float bigJumpShakeIntensity = 0.3f;
    public float bigJumpShakeDuration = 0.5f;
    public GameObject bigJumpEffectPrefab;

    [Header("Big Melee Attack Settings")]
    public float bigMeleeAttackRange = 3f;
    public float bigMeleeAttackCooldown = 3f;
    public float bigMeleeDamage = 20f;
    public float bigMeleeDamageDelay = 1.5f;
    public float bigMeleeKnockback = 5f;

    [Header("Movement Speed")]
    public float patrolSpeed = 2f;
    public float chaseSpeed = 6f;

    [Header("Sight Visualization")]
    public bool showSightCircle = true;
    private LineRenderer sightCircleRenderer;

    [Header("Attack Sounds")]
    public AudioClip meleeAttackSound;
    public AudioClip rangedAttackSound;
    public AudioClip chargeAttackSound;
    public AudioClip bigJumpAttackSound;
    public AudioClip bigMeleeAttackSound;
    public AudioSource AudioSource;
    public AudioClip preExplosionDialogueClip;


    void Start()
    {
        stateMachine = GetComponent<StateMachine>();
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponentInChildren<Animator>();

        FindPlayer();

        if (agent != null)
        {
            agent.speed = patrolSpeed;
        }

        if (showSightCircle)
        {
            sightCircleRenderer = gameObject.AddComponent<LineRenderer>();
            Shader shader = Shader.Find("Unlit/Color");
            if (shader == null) shader = Shader.Find("Legacy Shaders/Diffuse");
            if (shader != null)
            {
                sightCircleRenderer.material = new Material(shader);
                sightCircleRenderer.material.color = Color.yellow;
            }
            else
            {
                Material fallbackMaterial = new Material(Shader.Find("Standard"));
                fallbackMaterial.color = Color.yellow;
                fallbackMaterial.SetFloat("_Metallic", 0f);
                fallbackMaterial.SetFloat("_Glossiness", 0f);
                sightCircleRenderer.material = fallbackMaterial;
            }

            sightCircleRenderer.startWidth = 0.1f;
            sightCircleRenderer.endWidth = 0.1f;
            sightCircleRenderer.useWorldSpace = true;
            sightCircleRenderer.loop = true;
            sightCircleRenderer.enabled = true;
            sightCircleRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            sightCircleRenderer.receiveShadows = false;
        }
    }

    void Update()
    {
        if (player == null)
        {
            playerSearchCooldown -= Time.deltaTime;
            if (playerSearchCooldown <= 0f)
            {
                FindPlayer();
                playerSearchCooldown = PLAYER_SEARCH_INTERVAL;
            }
        }

        CanSeePlayer();

        if (stateMachine != null && stateMachine.activeState != null)
        {
            currentState = stateMachine.activeState.ToString();
        }
        if (animator != null && agent != null)
        {
            animator.SetFloat("speed", agent.velocity.magnitude);
        }
        DrawSightCircle();
    }

    private void FindPlayer()
    {
        if (playerReference != null)
        {
            player = playerReference;
            hasLoggedPlayerNotFoundWarning = false;
            return;
        }

        GameObject foundPlayer = GameObject.FindGameObjectWithTag("Player");
        if (foundPlayer != null)
        {
            player = foundPlayer;
            hasLoggedPlayerNotFoundWarning = false;
            return;
        }

        PlayerController playerController = FindObjectOfType<PlayerController>();
        if (playerController != null)
        {
            player = playerController.gameObject;
            Debug.Log($"EnemyWithSM on {gameObject.name}: Found player by PlayerController component");
            hasLoggedPlayerNotFoundWarning = false;
            return;
        }

        if (player == null && !hasLoggedPlayerNotFoundWarning)
        {
            Debug.LogWarning($"EnemyWithSM on {gameObject.name}: Could not find player!");
            hasLoggedPlayerNotFoundWarning = true;
        }
    }

    void DrawSightCircle()
    {
        if (!showSightCircle || sightCircleRenderer == null) return;

        RaycastHit hit;
        float groundY = transform.position.y - 1f;
        if (Physics.Raycast(transform.position, Vector3.down, out hit, 10f))
        {
            groundY = hit.point.y;
        }

        Vector3 center = new Vector3(transform.position.x, groundY + 0.05f, transform.position.z);
        int segments = 64;
        float angleStep = 360f / segments;

        sightCircleRenderer.positionCount = segments + 1;

        for (int i = 0; i <= segments; i++)
        {
            float angle = i * angleStep * Mathf.Deg2Rad;
            Vector3 point = center + new Vector3(
                Mathf.Cos(angle) * sightDistance,
                0f,
                Mathf.Sin(angle) * sightDistance
            );
            sightCircleRenderer.SetPosition(i, point);
        }
    }

    public bool CanSeePlayer()
    {
        if (player != null)
        {
            if (Vector3.Distance(transform.position, player.transform.position) <= sightDistance)
            {
                Vector3 direction = (player.transform.position - transform.position);
                float angleToPlayer = Vector3.Angle(direction, transform.forward);
                if (angleToPlayer >= -fieldOfView && angleToPlayer <= fieldOfView)
                {
                    Ray ray = new Ray(transform.position, direction);
                    RaycastHit hit = new RaycastHit();
                    if (Physics.Raycast(ray, out hit, sightDistance))
                    {
                        if (hit.transform.gameObject == player)
                        {
                            return true;
                        }
                    }
                }
            }
        }
        return false;
    }

    // ===============================================================
    // 🔊 UNIVERSAL SOUND SYSTEM FOR ALL ATTACKS
    // ===============================================================

    public void PlaySound(AudioClip clip)
    {
        if (clip == null) return;

        GameObject temp = new GameObject("EnemyTempSound");
        AudioSource a = temp.AddComponent<AudioSource>();

        a.clip = clip;
        a.spatialBlend = 1f;
        a.volume = 1f;
        a.Play();

        temp.transform.position = transform.position;

        Destroy(temp, clip.length);
    }

    public void PlayMeleeAttackSound()
    {
        PlaySound(meleeAttackSound);
    }

    public void PlayRangedAttackSound()
    {
        PlaySound(rangedAttackSound);
    }

    public void PlayChargeAttackSound()
    {
        PlaySound(chargeAttackSound);
    }

    public void PlayBigJumpAttackSound()
    {
        PlaySound(bigJumpAttackSound);
    }

    public void PlayBigMeleeAttackSound()
    {
        PlaySound(bigMeleeAttackSound);
    }
    public void PlayThrowAttackSound()
    {
        PlaySound(rangedAttackSound);
    }


    // ===============================================================
    // DISTRACTION + STUN (unchanged)
    // ===============================================================

    public void TriggerDistraction(Vector3 targetPosition)
    {
        if (stateMachine != null)
        {
            stateMachine.ChangeState(new DistractionState(targetPosition));
        }
    }

    public void TriggerDistraction(GameObject targetObject)
    {
        if (targetObject != null)
        {
            TriggerDistraction(targetObject.transform.position);
        }
    }

    public void ApplyStun(float duration)
    {
        if (stateMachine != null)
        {
            stateMachine.ChangeState(new StunState(duration));
        }
    }
}
