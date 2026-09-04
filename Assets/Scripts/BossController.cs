using UnityEngine;

public class BossController : MonoBehaviour
{
    [Header("Target")]
    public Transform playerTransform; // Drag player here directly!

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

    void Start()
    {
        currentHealth = maxHealth;

        // Fallback if player reference isn't dragged in
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
        if (!isAwakened || playerTransform == null) return;

        float distanceToPlayer = Vector3.Distance(transform.position, playerTransform.position);

        // Always face the player aggressively
        Vector3 dirToPlayer = (playerTransform.position - transform.position).normalized;
        dirToPlayer.y = 0;
        if (dirToPlayer != Vector3.zero)
        {
            Quaternion lookRot = Quaternion.LookRotation(dirToPlayer);
            transform.rotation = Quaternion.Slerp(transform.rotation, lookRot, rotationSpeed * Time.deltaTime);
        }

        // Relentlessly chase or attack
        if (distanceToPlayer > attackRange)
        {
            transform.position += dirToPlayer * moveSpeed * Time.deltaTime;
        }
        else if (Time.time >= lastAttackTime + attackCooldown)
        {
            PerformBossAttack();
        }
    }

    public void WakeUpBoss()
    {
        isAwakened = true;
        Debug.Log("Boss has awakened and is locked onto you!");
    }

    void PerformBossAttack()
    {
        lastAttackTime = Time.time;
        Debug.Log("Boss swings continuously at the player!");

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
        Debug.Log("VICTORY! Boss defeated.");
        Destroy(gameObject);
    }
}