using UnityEngine;

[RequireComponent(typeof(CharacterController))]
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
    public float gravity = 9.81f; // Added for ground snapping
    public bool isDead { get; private set; } = false;

    [Header("Combat Settings")]
    public float attackRange = 2.5f;
    public float attackCooldown = 1.5f;   
    private float lastAttackTime = 0f;
    public float attackDamage = 15f;

    [Header("Stagger System")]
    public float maxStagger = 100f;
    public float currentStagger = 0f;
    public float defaultStaggerPerHit = 25f;
    public float staggerDecayRate = 5f; // Drains stagger slowly if player stops attacking
    public float staggerDuration = 3f;  // Stun length in seconds
    public bool isStaggered { get; private set; } = false;
    private float staggerTimer = 0f;

    [Header("Awakening State")]
    public bool isAwakened = false;     

    [Header("Animation Settings")]
    public float speedDampTime = 0.15f;
    private Animator animator;

    [Header("Visuals")]
    public Renderer bossRenderer;

    // --- Added for physics movement ---
    private CharacterController controller;
    private float verticalVelocity = 0f;

    void Start()
    {
        currentHealth = maxHealth;
        animator = GetComponentInChildren<Animator>();
        
        // Initialize Character Controller
        controller = GetComponent<CharacterController>();

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
        if (isDead) return;

        // 1. Calculate Gravity continuously
        if (controller.isGrounded && verticalVelocity < 0)
        {
            verticalVelocity = -2f; // Snaps boss to slopes
        }
        else
        {
            verticalVelocity -= gravity * Time.deltaTime;
        }

        // Handle Staggered/Stunned State (Disables movement & attacks)
        if (isStaggered)
        {
            staggerTimer -= Time.deltaTime;
            UpdateAnimationSpeed(0f); // Freeze movement during stagger

            // Apply gravity even while staggered so they don't hover
            controller.Move(new Vector3(0, verticalVelocity, 0) * Time.deltaTime);

            if (staggerTimer <= 0f)
            {
                isStaggered = false;
                currentStagger = 0f; // Reset meter after recovery

                // Stop playing looping stagger animation
                if (animator != null)
                {
                    animator.SetBool("IsStagger", false); // Matched to 'IsStagger'
                }
            }
            return; // Block AI movement and attacks while staggered
        }

        // Slowly decay stagger meter if player stops attacking
        if (currentStagger > 0f)
        {
            currentStagger = Mathf.Clamp(currentStagger - staggerDecayRate * Time.deltaTime, 0f, maxStagger);
        }

        if (playerScript == null && playerTransform != null)
        {
            playerScript = playerTransform.GetComponent<HUDPlayer>();
        }

        if (!isAwakened || playerTransform == null || (playerScript != null && playerScript.isDead)) 
        {
            UpdateAnimationSpeed(0f);
            // Apply gravity while asleep
            controller.Move(new Vector3(0, verticalVelocity, 0) * Time.deltaTime);
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
        Vector3 moveVelocity = Vector3.zero;

        if (distanceToPlayer > attackRange)
        {
            // 2. Set horizontal movement direction instead of forcing transform.position
            moveVelocity = dirToPlayer * moveSpeed;
            targetAnimSpeed = 1f;
        }
        else if (Time.time >= lastAttackTime + attackCooldown)
        {
            PerformBossAttack();
        }

        // 3. Combine horizontal movement and gravity, then execute move
        moveVelocity.y = verticalVelocity;
        controller.Move(moveVelocity * Time.deltaTime);

        UpdateAnimationSpeed(targetAnimSpeed);
    }

    public void AddStagger(float amount)
    {
        if (isDead || isStaggered) return;

        currentStagger = Mathf.Clamp(currentStagger + amount, 0f, maxStagger);

        if (currentStagger >= maxStagger)
        {
            TriggerStagger();
        }
    }

    private void TriggerStagger()
    {
        isStaggered = true;
        staggerTimer = staggerDuration;

        // Start playing looping stagger animation
        if (animator != null)
        {
            animator.SetBool("IsStagger", true); // Matched to 'IsStagger'
        }

        Debug.Log("Boss staggered!");
    }

    public void GetParried()
    {
        if (isDead) return;

        // Parrying fills 50% of the stagger meter instantly
        AddStagger(maxStagger * 0.5f);
    }

    public void TakeDamage(float damageAmount)
    {
        TakeDamage(damageAmount, defaultStaggerPerHit);
    }

    public void TakeDamage(float damageAmount, float customStaggerAmount)
    {
        if (isDead) return;

        isAwakened = true; 
        currentHealth -= damageAmount;

        AddStagger(customStaggerAmount);

        if (bossRenderer != null)
        {
            StartCoroutine(FlashColor());
        }

        if (currentHealth <= 0)
        {
            Die();
        }
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
            playerScript.TakeDamage(attackDamage, gameObject);
        }
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

        if (animator != null)
        {
            animator.SetBool("IsStagger", false); // Matched to 'IsStagger'
            animator.SetTrigger("Die");
        }

        Collider[] colliders = GetComponentsInChildren<Collider>();
        foreach (Collider col in colliders)
        {
            col.enabled = false;
        }

        Destroy(gameObject, 3f);
    }
}