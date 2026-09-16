using System.Collections;
using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class Bandit : MonoBehaviour
{
    [Header("Target Reference")]
    public Transform playerTransform;
    private HUDPlayer playerScript;

    [Header("Bandit Stats")]
    public float maxHealth = 80f;
    public float currentHealth;
    public float moveSpeed = 3.5f;        
    public float rotationSpeed = 10f;    
    public float gravity = 9.81f; 
    public bool isDead { get; private set; } = false;

    [Header("Detection / Aggro")]
    public float aggroRange = 8f;        // Distance at which the bandit notices the player
    public bool isAggroed = false;

    [Header("Combat Settings")]
    public float attackRange = 2.0f;      
    public float attackCooldown = 2.0f;    
    private float lastAttackTime = 0f;
    public float attackDamage = 10f;

    [Header("Visuals & Animation")]
    public float speedDampTime = 0.1f;
    public Renderer banditRenderer;
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

        if (banditRenderer == null)
        {
            banditRenderer = GetComponentInChildren<Renderer>();
        }
    }

    void Update()
    {
        if (isDead) return;

        // Gravity check
        if (controller.isGrounded && verticalVelocity < 0)
        {
            verticalVelocity = -2f; 
        }
        else
        {
            verticalVelocity -= gravity * Time.deltaTime;
        }

        if (playerScript == null && playerTransform != null)
        {
            playerScript = playerTransform.GetComponent<HUDPlayer>();
        }

        // Check if player is dead
        if (playerTransform == null || (playerScript != null && playerScript.isDead))
        {
            UpdateAnimationSpeed(0f);
            controller.Move(new Vector3(0, verticalVelocity, 0) * Time.deltaTime);
            return;
        }

        // Distance check for auto-aggro
        float distanceToPlayer = Vector3.Distance(transform.position, playerTransform.position);
        if (!isAggroed && distanceToPlayer <= aggroRange)
        {
            isAggroed = true;
        }

        // If not aggroed yet, stay idle
        if (!isAggroed)
        {
            UpdateAnimationSpeed(0f);
            controller.Move(new Vector3(0, verticalVelocity, 0) * Time.deltaTime);
            return;
        }

        // Movement & Chasing Player
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
            targetAnimSpeed = 1f; // Run / Walk animation
        }
        else if (Time.time >= lastAttackTime + attackCooldown)
        {
            PerformBanditAttack();
        }

        moveVelocity.y = verticalVelocity;
        controller.Move(moveVelocity * Time.deltaTime);
        UpdateAnimationSpeed(targetAnimSpeed);
    }

    void PerformBanditAttack()
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
            Debug.Log("Bandit attacked player for " + attackDamage + " damage!");
        }
    }

    public void TakeDamage(float damageAmount)
    {
        if (isDead) return;
        isAggroed = true; // Getting hit instantly aggros the bandit
        currentHealth -= damageAmount;
        
        Debug.Log("Bandit took damage! Current Health: " + currentHealth);

        if (banditRenderer != null) StartCoroutine(FlashColor());
        if (currentHealth <= 0) Die();
    }

    void UpdateAnimationSpeed(float targetSpeed)
    {
        if (animator != null && !isDead)
        {
            animator.SetFloat("Speed", targetSpeed, speedDampTime, Time.deltaTime);
        }
    }

    IEnumerator FlashColor()
    {
        Color orig = banditRenderer.material.color;
        banditRenderer.material.color = Color.white;
        yield return new WaitForSeconds(0.15f);
        banditRenderer.material.color = orig;
    }

    void Die()
    {
        isDead = true;

        if (animator != null)
        {
            animator.SetTrigger("Die");
        }

        Collider[] colliders = GetComponentsInChildren<Collider>();
        foreach (Collider col in colliders) col.enabled = false;

        Destroy(gameObject, 3f);
    }
}