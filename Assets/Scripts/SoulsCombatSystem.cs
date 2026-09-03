using UnityEngine;

public class SoulsCombatSystem : MonoBehaviour
{
    [Header("Target Lock Settings")]
    public float lockRange = 15f;
    private Transform currentTarget;
    private bool isLockedOn = false;

    [Header("Combat Settings")]
    public float attackRange = 2.5f;
    public float attackCooldown = 0.8f;
    private float lastAttackTime = 0f;

    void Update()
    {
        // Toggle Target Lock with 'Q' key
        if (Input.GetKeyDown(KeyCode.Q))
        {
            if (isLockedOn)
            {
                UnlockTarget();
            }
            else
            {
                FindNearestEnemy();
            }
        }

        // Maintain Lock-on rotation if active
        if (isLockedOn && currentTarget != null)
        {
            if (Vector3.Distance(transform.position, currentTarget.position) > lockRange)
            {
                UnlockTarget();
            }
            else
            {
                Vector3 dirToTarget = (currentTarget.position - transform.position).normalized;
                dirToTarget.y = 0; 
                if (dirToTarget != Vector3.zero)
                {
                    Quaternion lookRot = Quaternion.LookRotation(dirToTarget);
                    transform.rotation = Quaternion.Slerp(transform.rotation, lookRot, Time.deltaTime * 10f);
                }
            }
        }

        // Melee Attack Input (Left Mouse Click)
        if (Input.GetMouseButtonDown(0) && Time.time >= lastAttackTime + attackCooldown)
        {
            PerformAttack();
        }
    }

    void FindNearestEnemy()
    {
        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
        float shortestDistance = Mathf.Infinity;
        Transform nearestEnemy = null;

        foreach (GameObject enemy in enemies)
        {
            float distanceToEnemy = Vector3.Distance(transform.position, enemy.transform.position);
            if (distanceToEnemy < shortestDistance && distanceToEnemy <= lockRange)
            {
                shortestDistance = distanceToEnemy;
                nearestEnemy = enemy.transform;
            }
        }

        if (nearestEnemy != null)
        {
            currentTarget = nearestEnemy;
            isLockedOn = true;
            Debug.Log("Locked onto target: " + currentTarget.name);
        }
    }

    void UnlockTarget()
    {
        isLockedOn = false;
        currentTarget = null;
        Debug.Log("Target unlocked.");
    }

    void PerformAttack()
    {
        lastAttackTime = Time.time;
        Debug.Log("Player Swings Weapon!");

        if (currentTarget != null && Vector3.Distance(transform.position, currentTarget.position) <= attackRange)
        {
            Debug.Log("Hit Enemy Dummy!");
            Renderer enemyRenderer = currentTarget.GetComponent<Renderer>();
            if (enemyRenderer != null)
            {
                StartCoroutine(FlashRed(enemyRenderer));
            }
        }
    }

    System.Collections.IEnumerator FlashRed(Renderer rend)
    {
        Color originalColor = rend.material.color;
        rend.material.color = Color.red;
        yield return new WaitForSeconds(0.2f);
        rend.material.color = originalColor;
    }
}