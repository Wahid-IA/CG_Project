using UnityEngine;

public class BossController : MonoBehaviour
{
    [Header("Target Reference")]
    public Transform playerTransform;

    [Header("Boss Stats")]
    public float maxHealth = 300f;
    public float currentHealth;
    public float moveSpeed = 4f;         
    public float rotationSpeed = 10f;

    [Header("Combat Settings")]
    public float attackRange = 2.5f;
    public float attackCooldown = 1.5f;   
    private float lastAttackTime = 0f;
    public float attackDamage = 15f;

    [Header("Awakening State")]
    public bool isAwakened = false;     

    [Header("Visuals")]
    public Renderer bossRenderer;

    // <--- ADDED: Animator Reference
    private Animator animator;

    void Start()
    {
        currentHealth = maxHealth;

        // <--- ADDED: Get Animator on child model
        animator = GetComponentInChildren<Animator>();

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
        // If unawakened or no player, stay in Idle
        if (!isAwakened || playerTransform == null) 
        {
            if (animator != null) animator.SetFloat("Speed", 0f, 0.15f, Time.deltaTime);
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

        // Movement & Attack Logic
        if (distanceToPlayer > attackRange)
        {
            transform.position += dirToPlayer * moveSpeed * Time.deltaTime;
            targetAnimSpeed = 0.5f; // <--- 0.5f = Walk (1.0f triggers Sprint)
        }
        else
        {
            targetAnimSpeed = 0f; // <--- 0f = Idle

            if (Time.time >= lastAttackTime + attackCooldown)
            {
                PerformBossAttack();
            }
        }

        // Smoothly damp animation transitions over 0.15 seconds
        if (animator != null)
        {
            animator.SetFloat("Speed", targetAnimSpeed, 0.15f, Time.deltaTime);
        }
    }

    public void WakeUpBoss()
    {
        isAwakened = true;
    }

    void PerformBossAttack()
    {
        lastAttackTime = Time.time;

        HUDPlayer playerScript = playerTransform.GetComponent<HUDPlayer>();
        if (playerScript != null)
        {
            playerScript.currentHealth -= attackDamage;
        }
    }

    public void TakeDamage(float damageAmount)
    {
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
        Destroy(gameObject);
    }
}