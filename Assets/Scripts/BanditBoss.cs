using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class BanditBoss : MonoBehaviour
{
    [Header("Target Reference")]
    public Transform playerTransform;

    [Header("Boss Stats")]
    public float maxHealth = 150f;
    public float currentHealth;
    public float moveSpeed = 6f;         
    public float rotationSpeed = 15f;    
    public float gravity = 9.81f; 
    public bool isDead { get; private set; } = false;

    [Header("Combat Settings")]
    public float attackRange = 2.5f;       
    public float attackCooldown = 1f;    
    private float lastAttackTime = 0f;
    public float attackDamage = 12f;

    [Header("Stagger System")]
    public float maxStagger = 60f;       
    public float currentStagger = 0f;
    public float defaultStaggerPerHit = 20f;
    public float staggerDecayRate = 8f; 
    public float staggerDuration = 2f;   
    public bool isStaggered { get; private set; } = false;
    private float staggerTimer = 0f;

    [Header("Awakening State")]
    public bool isAwakened = false;     

    [Header("Animation Settings")]
    public float speedDampTime = 0.1f;
    private Animator animator;

    [Header("Visuals")]
    public Renderer bossRenderer;

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

        if (bossRenderer == null)
        {
            bossRenderer = GetComponentInChildren<Renderer>();
        }
    }

    void Update()
    {
        if (isDead) return;

        // Ground check & gravity
        if (controller.isGrounded && verticalVelocity < 0)
        {
            verticalVelocity = -2f; 
        }
        else
        {
            verticalVelocity -= gravity * Time.deltaTime;
        }

        // Stagger behavior
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

        // If not awakened or no player, stay idle
        if (!isAwakened || playerTransform == null) 
        {
            UpdateAnimationSpeed(0f);
            controller.Move(new Vector3(0, verticalVelocity, 0) * Time.deltaTime);
            return;
        }

        // Movement & Chasing Player
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
            PerformBanditAttack();
        }

        moveVelocity.y = verticalVelocity;
        controller.Move(moveVelocity * Time.deltaTime);
        UpdateAnimationSpeed(targetAnimSpeed);
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
        if (animator != null) animator.SetBool("IsStagger", true); 
    }

    public void TakeDamage(float damageAmount)
    {
        if (isDead) return;
        isAwakened = true; 
        currentHealth -= damageAmount;
        AddStagger(defaultStaggerPerHit);

        if (bossRenderer != null) StartCoroutine(FlashColor());
        if (currentHealth <= 0) Die();
    }

    void PerformBanditAttack()
    {
        lastAttackTime = Time.time;
        if (animator != null) animator.SetTrigger("Attack");
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
        Debug.Log("Bandit Boss has woken up and is targeting the player!");
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
            animator.SetBool("IsStagger", false); 
            animator.SetTrigger("Die");
        }

        Collider[] colliders = GetComponentsInChildren<Collider>();
        foreach (Collider col in colliders) col.enabled = false;

        Destroy(gameObject, 3f);
    }
}