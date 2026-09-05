using UnityEngine;

[RequireComponent(typeof(HUDPlayer))]
public class SoulsCombatSystem : MonoBehaviour
{
    private HUDPlayer hudPlayer;
    private Animator animator;
    private SoulsPlayerController movementController;

    [Header("Target Lock")]
    public float lockRange = 15f;
    public Transform currentTarget { get; private set; }
    public bool isLockedOn { get; private set; } = false;

    [Header("Melee Combat")]
    public float attackCooldown = 0.8f;
    private float lastAttackTime = 0f;
    public float attackRange = 2.2f;
    public float attackRadius = 1.2f;
    public float attackDamage = 25f;
    public float attackStaminaCost = 20f;

    [Header("Parry Settings")]
    public float parryStaminaCost = 15f;
    public float parryStartup = 0.533f/3f;   // Delay before active parry frames start
    public float parryWindow = 0.5f/2f;    // Duration where parry is active
    public float parryRecovery = 0.433f/1.25f;  // Cooldown after parry window ends
    private float parryTimer = 0f;
    public bool isParrying { get; private set; } = false;

    public bool IsParryActive => isParrying && (parryTimer >= parryStartup) && (parryTimer <= (parryStartup + parryWindow));


    void Start()
    {
        hudPlayer = GetComponent<HUDPlayer>();
        animator = GetComponentInChildren<Animator>();
        movementController = GetComponent<SoulsPlayerController>();
    }

    void Update()
    {
        if (hudPlayer.isDead) return;

        HandleTargetLock();

        if (movementController != null && movementController.isRolling) return;

        // Handle Parry Input (Right Click)
        if (Input.GetMouseButtonDown(1) && !isParrying)
        {
            PerformParry();
        }

        if (isParrying)
        {
            parryTimer += Time.deltaTime;
            if (parryTimer >= (parryStartup + parryWindow + parryRecovery))
            {
                isParrying = false;
            }
        }

        HandleCombat();
    }

    void PerformParry()
    {
        if (!hudPlayer.ConsumeStamina(parryStaminaCost)) return;

        isParrying = true;
        parryTimer = 0f;

        if (animator != null)
        {
            animator.SetTrigger("Parry");
        }
    }

    void HandleTargetLock()
    {
        if (Input.GetKeyDown(KeyCode.Q))
        {
            if (isLockedOn) UnlockTarget();
            else FindNearestEnemy();
        }

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
                    transform.rotation = Quaternion.Slerp(transform.rotation, lookRot, Time.deltaTime * 15f);
                }
            }
        }
    }

    void HandleCombat()
    {
        if (Input.GetMouseButtonDown(0) && Time.time >= lastAttackTime + attackCooldown)
        {
            if (hudPlayer.HasStamina(attackStaminaCost))
            {
                PerformMeleeAttack();
            }
        }
    }

    void PerformMeleeAttack()
    {
        if (!hudPlayer.ConsumeStamina(attackStaminaCost)) return;

        lastAttackTime = Time.time;

        if (animator != null)
        {
            animator.SetTrigger("Attack");
        }

        Vector3 hitBoxCenter = transform.position + transform.forward * attackRange + Vector3.up * 1f;
        Collider[] hitEnemies = Physics.OverlapSphere(hitBoxCenter, attackRadius);

        foreach (Collider col in hitEnemies)
        {
            if (col.CompareTag("Enemy"))
            {
                BossController boss = col.GetComponentInParent<BossController>();
                if (boss != null)
                {
                    boss.TakeDamage(attackDamage);
                }
            }
        }
    }

    void FindNearestEnemy()
    {
        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
        float shortestDistance = Mathf.Infinity;
        Transform nearestEnemy = null;

        foreach (GameObject enemy in enemies)
        {
            float dist = Vector3.Distance(transform.position, enemy.transform.position);
            if (dist < shortestDistance && dist <= lockRange)
            {
                shortestDistance = dist;
                nearestEnemy = enemy.transform;
            }
        }

        if (nearestEnemy != null)
        {
            currentTarget = nearestEnemy;
            isLockedOn = true;
        }
    }

    void UnlockTarget()
    {
        isLockedOn = false;
        currentTarget = null;
    }
}