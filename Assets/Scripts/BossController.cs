using UnityEngine;

public class BossController : MonoBehaviour
{
    private Transform playerTransform;

    [Header("Boss Stats")]
    public float maxHealth = 300f;
    public float currentHealth;
    public float moveSpeed = 3.5f;
    public float rotationSpeed = 8f;

    [Header("Combat Ranges")]
    public float aggroRange = 60f;
    public float attackRange = 4f;
    public float attackCooldown = 2f;
    private float lastAttackTime = 0f;

    [Header("Visuals")]
    public Renderer bossRenderer;

    void Start()
    {
        currentHealth = maxHealth;

        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            playerTransform = playerObj.transform;
        }

        if (bossRenderer == null)
        {
            bossRenderer = GetComponentInChildren<Renderer>();
        }
    }

    void Update()
    {
        if (playerTransform == null) return;

        float distanceToPlayer = Vector3.Distance(transform.position, playerTransform.position);

        if (distanceToPlayer <= aggroRange)
        {
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
    }

    void PerformBossAttack()
    {
        lastAttackTime = Time.time;

        HUDPlayer playerScript = playerTransform.GetComponent<HUDPlayer>();
        if (playerScript != null)
        {
            playerScript.currentHealth -= 20f;
        }
    }

    public void TakeDamage(float damageAmount)
    {
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