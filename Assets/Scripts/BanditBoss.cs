using System.Collections;
using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class BanditBoss : MonoBehaviour
{
    [Header("Target Reference")]
    public Transform playerTransform;
    private HUDPlayer playerScript;

    [Header("Boss Stats")]
    public float maxHealth = 250f;
    public float currentHealth;
    public float walkSpeed = 3.5f;          // Speed when walking close to player
    public float runSpeed = 6.0f;           // Speed when running from farther away
    public float rotationSpeed = 12f;    
    public float gravity = 9.81f; 
    public bool isDead { get; private set; } = false;

    [Header("Combat & Ranges")]
    public float attackRange = 2.8f;        // Distance to stop and trigger attack
    public float runRange = 6.0f;           // > 6m = Run, <= 6m = Walk
    public float attackCooldown = 1.8f;     // Delay between attack sequences
    public float attackDamageDelay = 0.45f; // Delay for weapon swing impact frame
    public float attackDamage = 20f;
    private float lastAttackTime = 0f;
    public bool isAttacking { get; private set; } = false;

    [Header("Stagger System")]
    public float maxStagger = 80f;      
    public float currentStagger = 0f;
    public float defaultStaggerPerHit = 20f;
    public float staggerDecayRate = 6f; 
    public float staggerDuration = 2f;   
    public bool isStaggered { get; private set; } = false;
    private float staggerTimer = 0f;

    [Header("Awakening State")]
    public bool isAwakened = false;     

    [Header("Visuals & Animation")]
    public float speedDampTime = 0.1f;
    public Renderer bossRenderer;
    private Animator animator;
    private CharacterController controller;
    private float verticalVelocity = 0f;

    void Start()
    {
        currentHealth = maxHealth;
        animator = GetComponentInChildren<Animator>();
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

        // 1. Gravity Handling
        if (controller.isGrounded && verticalVelocity < 0)
        {
            verticalVelocity = -2f; 
        }
        else
        {
            verticalVelocity -= gravity * Time.deltaTime;
        }

        // 2. Check if Animator is currently in or transitioning into an "Attacking" state
        bool isAnimatorInAttackTag = animator != null && 
            (animator.GetCurrentAnimatorStateInfo(0).IsTag("Attacking") || 
             animator.GetNextAnimatorStateInfo(0).IsTag("Attacking"));

        // 3. Stagger Handling
        if (isStaggered)
        {
            staggerTimer -= Time.deltaTime;
            UpdateAnimationSpeed(0f); 
            controller.Move(new Vector3(0, verticalVelocity, 0) * Time.deltaTime);

            if (staggerTimer <= 0f)
            {
                isStaggered = false;
                currentStagger = 0f; 
                if (animator != null) animator.SetBool("IsStagger", false); 
            }
            return; 
        }

        // 4. Freeze horizontal movement completely during Attack animations or attack routine
        if (isAnimatorInAttackTag || isAttacking)
        {
            UpdateAnimationSpeed(0f);
            controller.Move(new Vector3(0, verticalVelocity, 0) * Time.deltaTime);
            return;
        }

        // Decay stagger over time
        if (currentStagger > 0f)
        {
            currentStagger = Mathf.Clamp(currentStagger - staggerDecayRate * Time.deltaTime, 0f, maxStagger);
        }

        if (playerScript == null && playerTransform != null)
        {
            playerScript = playerTransform.GetComponent<HUDPlayer>();
        }

        // Idle state if not awakened or player is dead
        if (!isAwakened || playerTransform == null || (playerScript != null && playerScript.isDead)) 
        {
            UpdateAnimationSpeed(0f);
            controller.Move(new Vector3(0, verticalVelocity, 0) * Time.deltaTime);
            return;
        }

        float distanceToPlayer = Vector3.Distance(transform.position, playerTransform.position);
        Vector3 dirToPlayer = (playerTransform.position - transform.position).normalized;
        dirToPlayer.y = 0;

        // Turn to face player
        if (dirToPlayer != Vector3.zero)
        {
            Quaternion lookRot = Quaternion.LookRotation(dirToPlayer);
            transform.rotation = Quaternion.Slerp(transform.rotation, lookRot, rotationSpeed * Time.deltaTime);
        }

        // 5. Locomotion & Range Logic
        if (distanceToPlayer > attackRange)
        {
            // Clear lingering Attack trigger so it doesn't queue up unwanted swings
            if (animator != null)
            {
                animator.ResetTrigger("Attack");
            }

            // Move towards player with Walk or Run speed depending on distance
            float moveSpeed;
            float targetAnimSpeed;

            if (distanceToPlayer > runRange)
            {
                moveSpeed = runSpeed;
                targetAnimSpeed = 1.0f; // Drives Run animation
            }
            else
            {
                moveSpeed = walkSpeed;
                targetAnimSpeed = 0.5f; // Drives Walk animation
            }

            Vector3 moveVelocity = dirToPlayer * moveSpeed;
            moveVelocity.y = verticalVelocity;
            controller.Move(moveVelocity * Time.deltaTime);
            UpdateAnimationSpeed(targetAnimSpeed);
        }
        else
        {
            // Within attack range -> Stop moving and attack if cooldown is ready
            UpdateAnimationSpeed(0f);
            controller.Move(new Vector3(0, verticalVelocity, 0) * Time.deltaTime);

            if (Time.time >= lastAttackTime + attackCooldown)
            {
                PerformBossAttack();
            }
        }
    }

    void PerformBossAttack()
    {
        if (playerScript != null && playerScript.isDead) return;

        lastAttackTime = Time.time;

        // Pick random attack index (1, 2, or 3)
        int attackIndex = Random.Range(1, 4); 
        if (animator != null)
        {
            animator.ResetTrigger("Attack"); // Clean trigger state
            animator.SetInteger("AttackIndex", attackIndex);
            animator.SetTrigger("Attack");
        }

        StartCoroutine(ApplyAttackDamageRoutine(attackIndex));
    }

    private IEnumerator ApplyAttackDamageRoutine(int attackIndex)
    {
        isAttacking = true;

        // Wait 1 frame so Animator registers transition into the attack state
        yield return null;

        // Reset trigger immediately so it cannot re-trigger automatically
        if (animator != null)
        {
            animator.ResetTrigger("Attack");
        }

        // Delay to match weapon swing impact frame
        yield return new WaitForSeconds(attackDamageDelay);

        // Apply damage if player is still within hit range (+ tolerance)
        if (!isStaggered && !isDead && playerScript != null && !playerScript.isDead)
        {
            float currentDist = Vector3.Distance(transform.position, playerTransform.position);
            if (currentDist <= attackRange + 0.8f)
            {
                playerScript.TakeDamage(attackDamage, gameObject);
                Debug.Log($"Bandit King executed Attack #{attackIndex} hitting player for {attackDamage} damage!");
            }
        }

        // Wait until attack animation finishes playing in Animator
        while (animator != null && 
              (animator.GetCurrentAnimatorStateInfo(0).IsTag("Attacking") || 
               animator.GetNextAnimatorStateInfo(0).IsTag("Attacking")))
        {
            yield return null;
        }

        isAttacking = false;
    }

    public void AddStagger(float amount)
    {
        if (isDead || isStaggered) return;
        currentStagger = Mathf.Clamp(currentStagger + amount, 0f, maxStagger);
        if (currentStagger >= maxStagger) TriggerStagger();
    }

    private void TriggerStagger()
    {
        isStaggered = true;
        isAttacking = false;
        staggerTimer = staggerDuration;

        if (animator != null)
        {
            animator.ResetTrigger("Attack");
            animator.SetBool("IsStagger", true); 
        }
        Debug.Log("Bandit King staggered!");
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
        
        Debug.Log("Bandit King took damage! Current Health: " + currentHealth);

        AddStagger(customStaggerAmount);

        if (bossRenderer != null) StartCoroutine(FlashColor());
        if (currentHealth <= 0) Die();
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

    IEnumerator FlashColor()
    {
        Color orig = bossRenderer.material.color;
        bossRenderer.material.color = Color.white;
        yield return new WaitForSeconds(0.15f);
        bossRenderer.material.color = orig;
    }

    void Die()
    {
        isDead = true;
        isAttacking = false;

        if (animator != null)
        {
            animator.ResetTrigger("Attack");
            animator.SetBool("IsStagger", false); 
            animator.SetTrigger("Die");
        }

        Collider[] colliders = GetComponentsInChildren<Collider>();
        foreach (Collider col in colliders) col.enabled = false;

        Destroy(gameObject, 3f);
    }
}