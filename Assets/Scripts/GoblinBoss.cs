using System.Collections;
using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class GoblinBoss : MonoBehaviour
{
    [Header("Target Reference")]
    public Transform playerTransform;
    private HUDPlayer playerScript;

    [Header("Boss Stats")]
    public float maxHealth = 180f;
    public float currentHealth = 180f;
    public float moveSpeed = 6.5f;          // Goblins are faster than bandits
    public float rotationSpeed = 16f;       // Quicker turns
    public float gravity = 9.81f; 
    public bool isDead { get; private set; } = false;

    [Header("Combat Settings")]
    public float attackRange = 2.0f;        // Shorter range for claws/dagger
    public float attackCooldown = 1.2f;     // Faster attack frequency
    private float lastAttackTime = 0f;
    public float attackDamage = 14f;

    [Header("Stagger System")]
    public float maxStagger = 50f;          // Easier to stagger than a heavy bandit
    public float currentStagger = 0f;
    public float defaultStaggerPerHit = 25f;
    public float staggerDecayRate = 8f; 
    public float staggerDuration = 1.5f;   
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

        if (controller.isGrounded && verticalVelocity < 0)
        {
            verticalVelocity = -2f; 
        }
        else
        {
            verticalVelocity -= gravity * Time.deltaTime;
        }

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
            moveVelocity = dirToPlayer * moveSpeed;
            targetAnimSpeed = 1f; 
        }
        else if (Time.time >= lastAttackTime + attackCooldown)
        {
            PerformBossAttack();
        }

        moveVelocity.y = verticalVelocity;
        controller.Move(moveVelocity * Time.deltaTime);
        UpdateAnimationSpeed(targetAnimSpeed);
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
            Debug.Log("Goblin Chieftain slashed player for " + attackDamage + " damage!");
        }
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
        staggerTimer = staggerDuration;
        if (animator != null)
        {
            animator.SetBool("IsStagger", true); 
        }
        Debug.Log("Goblin Chieftain staggered!");
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
        
        Debug.Log("Goblin Chieftain took damage! Current Health: " + currentHealth);

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

        if (animator != null)
        {
            animator.SetBool("IsStagger", false); 
            animator.SetTrigger("Die");
        }

        Collider[] colliders = GetComponentsInChildren<Collider>();
        foreach (Collider col in colliders) col.enabled = false;

        Destroy(gameObject, 3f);
    }
}