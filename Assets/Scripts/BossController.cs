using UnityEngine;

public class BossController : MonoBehaviour
{
    [Header("Target Reference")]
    public Transform playerTransform;
    private HUDPlayer playerScript;

    [Header("Boss Stats")]
    public float maxHealth = 300f;
    public float currentHealth;
    public float moveSpeed = 4f;        
    public float rotationSpeed = 10f;
    public bool isDead { get; private set; } = false;

    [Header("Combat Settings")]
    public float attackRange = 2.5f;
    public float attackCooldown = 1.5f;   
    private float lastAttackTime = 0f;
    public float attackDamage = 15f;

    [Header("Stagger State")]
    public float staggerDuration = 2.5f;
    public bool isStaggered { get; private set; } = false;
    private float staggerTimer = 0f;

    [Header("Awakening State")]
    public bool isAwakened = false;     

    [Header("Animation Settings")]
    [Tooltip("Dampening time to ensure smooth blending between Idle and Walk")]
    public float speedDampTime = 0.15f;
    private Animator animator;

    [Header("Visuals")]
    public Renderer bossRenderer;

    void Start()
    {
        currentHealth = maxHealth;
        animator = GetComponentInChildren<Animator>();

        if (playerTransform == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null) playerTransform = playerObj.transform;
        }

        if (playerTransform != null)
        {
            playerScript = playerTransform.GetComponent<HUDPlayer>();
        }

        if (bossRenderer == null)
        {
            bossRenderer = GetComponentInChildren<Renderer>();
        }
    }

    void Update()
    {
        // Stop movement and attack logic if boss is dead
        if (isDead) return;

        // Handle Stagger State from Parry
        if (isStaggered)
        {
            staggerTimer -= Time.deltaTime;
            UpdateAnimationSpeed(0f); // Freeze boss in place while staggered

            if (staggerTimer <= 0f)
            {
                isStaggered = false;
            }
            return; // Prevent movement and attacks while staggered
        }

        // Ensure player script reference is cached
        if (playerScript == null && playerTransform != null)
        {
            playerScript = playerTransform.GetComponent<HUDPlayer>();
        }

        // Stop pursuit and return to Idle if unawakened, missing target, or player is dead
        if (!isAwakened || playerTransform == null || (playerScript != null && playerScript.isDead)) 
        {
            UpdateAnimationSpeed(0f);
            return;
        }

        float distanceToPlayer = Vector3.Distance(transform.position, playerTransform.position);

        Vector3 dirToPlayer = (playerTransform.position - transform.position).normalized;
        dirToPlayer.y = 0;
        if (dirToPlayer != Vector3.zero)
        {
            Quaternion lookRot = Quaternion.LookRotation(dirToPlayer);
            transform.rotation = Quaternion.Slerp(transform.rotation, lookRot, rotationSpeed * Time.deltaTime);
        }

        float targetAnimSpeed = 0f;

        if (distanceToPlayer > attackRange)
        {
            transform.position += dirToPlayer * moveSpeed * Time.deltaTime;
            targetAnimSpeed = 1f; // Triggers Walk state (> 0.1)
        }
        else if (Time.time >= lastAttackTime + attackCooldown)
        {
            PerformBossAttack();
        }

        UpdateAnimationSpeed(targetAnimSpeed);
    }

    void UpdateAnimationSpeed(float targetSpeed)
    {
        if (animator != null && !isDead)
        {
            animator.SetFloat("Speed", targetSpeed, speedDampTime, Time.deltaTime);
        }
    }

    public void WakeUpBoss()
    {
        if (isDead) return;
        isAwakened = true;
    }

    void PerformBossAttack()
    {
        if (playerScript != null && playerScript.isDead) return;

        lastAttackTime = Time.time;

        if (animator != null)
        {
            animator.SetTrigger("Attack");
        }

        if (playerScript != null)
        {
            // Pass 'gameObject' so HUDPlayer knows who attacked
            playerScript.TakeDamage(attackDamage, gameObject);
        }
    }

    public void GetParried()
    {
        if (isDead) return;

        isStaggered = true;
        staggerTimer = staggerDuration;

        if (animator != null)
        {
            animator.SetTrigger("Stagger");
        }

        Debug.Log("Boss was parried and staggered!");
    }

    public void TakeDamage(float damageAmount)
    {
        if (isDead) return;

        isAwakened = true; 
        currentHealth -= damageAmount;

        if (bossRenderer != null)
        {
            StartCoroutine(FlashColor());
        }

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    System.Collections.IEnumerator FlashColor()
    {
        Color orig = bossRenderer.material.color;
        bossRenderer.material.color = Color.white;
        yield return new WaitForSeconds(0.15f);
        bossRenderer.material.color = orig;
    }

    void Die()
    {
        isDead = true;

        // Trigger death animation
        if (animator != null)
        {
            animator.SetTrigger("Die");
        }

        // Disable colliders so player doesn't get blocked by the falling body
        Collider[] colliders = GetComponentsInChildren<Collider>();
        foreach (Collider col in colliders)
        {
            col.enabled = false;
        }

        // Destroy boss GameObject after 3 seconds to allow death animation to finish playing
        Destroy(gameObject, 3f);
    }
}