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

    void Start()
    {
        currentHealth = maxHealth;

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

        Vector3 dirToPlayer = (playerTransform.position - transform.position).normalized;
        dirToPlayer.y = 0;
        if (dirToPlayer != Vector3.zero)
        {
            Quaternion lookRot = Quaternion.LookRotation(dirToPlayer);
            transform.rotation = Quaternion.Slerp(transform.rotation, lookRot, rotationSpeed * Time.deltaTime);
        }

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
    }

    void PerformBossAttack()
    {
        lastAttackTime = Time.time;
        Debug.Log("Boss attempts an attack on player!");

        if (playerTransform != null)
        {
            HUDPlayer playerScript = playerTransform.GetComponent<HUDPlayer>();
            if (playerScript != null)
            {
                playerScript.currentHealth -= attackDamage;
                Debug.Log("SUCCESS: Boss hit player! Player HP remaining: " + playerScript.currentHealth);
            }
            else
            {
                Debug.LogError("ERROR: HUDPlayer script not found on the player transform!");
            }
        }
        else
        {
            Debug.LogError("ERROR: Boss has no playerTransform assigned!");
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