using UnityEngine;
using UnityEngine.EventSystems;

[RequireComponent(typeof(HUDPlayer))]
public class SoulsCombatSystem : MonoBehaviour
{
    private HUDPlayer hudPlayer;
    private Animator animator;
    private SoulsPlayerController movementController;

    [Header("Stance & Sheath Settings")]
    public bool isArmed { get; private set; } = false;
    [Tooltip("Seconds of inactivity out of combat before auto-sheathing")]
    public float autoSheathDelay = 5.0f; 
    private float lastCombatInputTime;

    [Header("Target Lock")]
    public float lockRange = 15f;
    public Transform currentTarget { get; private set; }
    public bool isLockedOn { get; private set; } = false;

    [Header("Melee Combat & Combo")]
    public float attackCooldown = 0.5f;
    private float lastAttackTime = 0f;
    public float comboResetWindow = 1.2f;
    private int currentCombo = 0;

    public float attackRange = 2.2f;
    public float attackRadius = 1.2f;
    public float attackDamage = 25f;
    public float attackStaminaCost = 20f;

    [Header("Shield Block / Parry Settings")]
    public float parryStaminaCost = 15f;
    public float parryStartup = 0.15f;   
    public float parryWindow = 0.35f;    
    public float parryRecovery = 0.3f;  
    private float parryTimer = 0f;
    public bool isParrying { get; private set; } = false;

    public bool IsParryActive => isParrying && (parryTimer >= parryStartup) && (parryTimer <= (parryStartup + parryWindow));

    [Header("Combat State")]
    public float combatDetectionRadius = 15f;

    public bool isInCombat
    {
        get
        {
            if (isLockedOn && currentTarget != null) return true;

            Collider[] hitColliders = Physics.OverlapSphere(transform.position, combatDetectionRadius);
            foreach (Collider col in hitColliders)
            {
                if (col.CompareTag("Enemy")) return true;
            }

            return false;
        }
    }

    void Start()
    {
        hudPlayer = GetComponent<HUDPlayer>();
        animator = GetComponentInChildren<Animator>();
        movementController = GetComponent<SoulsPlayerController>();
        
        // Ensure starting state is unarmed
        if (animator != null)
        {
            animator.SetBool("IsArmed", false);
        }
    }

    void Update()
    {
        if (hudPlayer != null && hudPlayer.isDead) return;
        if (InGameMainMenu.isMainMenuActive || PauseMenu.isPaused) return;
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return;

        HandleTargetLock();
        HandleAutoSheathTimer();

        // Prevent attacking/blocking while rolling
        if (movementController != null && movementController.isRolling) return;

        // RIGHT CLICK: Perform Shield Block / Parry (Requires weapon drawn)
        if (Input.GetMouseButtonDown(1) && !isParrying && isArmed)
        {
            PerformShieldBlock();
        }

        if (isParrying)
        {
            parryTimer += Time.deltaTime;
            if (parryTimer >= (parryStartup + parryWindow + parryRecovery))
            {
                isParrying = false;
            }
        }

        HandleCombatInput();
    }

    void HandleCombatInput()
    {
        // Reset combo count if too much time passes between clicks
        if (Time.time - lastAttackTime > comboResetWindow && currentCombo > 0)
        {
            currentCombo = 0;
            if (animator != null) animator.SetInteger("Combo", 0);
        }

        // LEFT CLICK: Draw weapon on 1st press, Attack combo on subsequent presses
        if (Input.GetMouseButtonDown(0))
        {
            lastCombatInputTime = Time.time;

            if (!isArmed)
            {
                DrawWeapon();
                return;
            }

            if (Time.time >= lastAttackTime + attackCooldown && hudPlayer.HasStamina(attackStaminaCost))
            {
                PerformMeleeAttack();
            }
        }
    }

    public void DrawWeapon()
    {
        isArmed = true;
        lastCombatInputTime = Time.time;

        if (animator != null)
        {
            animator.SetBool("IsArmed", true);
            animator.SetTrigger("Draw");
        }
    }

    public void SheathWeapon()
    {
        isArmed = false;
        currentCombo = 0;

        if (animator != null)
        {
            animator.SetBool("IsArmed", false);
            animator.SetInteger("Combo", 0);
            animator.SetTrigger("Sheath");
        }
    }

    void HandleAutoSheathTimer()
    {
        // Auto-sheath when idle and out of combat
        if (isArmed && !isInCombat && (Time.time - lastCombatInputTime > autoSheathDelay))
        {
            SheathWeapon();
        }
    }

    void PerformMeleeAttack()
    {
        if (!hudPlayer.ConsumeStamina(attackStaminaCost)) return;

        lastAttackTime = Time.time;
        currentCombo = (currentCombo % 3) + 1; // Cycles 1 -> 2 -> 3

        if (animator != null)
        {
            animator.SetInteger("Combo", currentCombo);
            animator.SetTrigger("Attack");
        }

        Vector3 hitBoxCenter = transform.position + transform.forward * attackRange + Vector3.up * 1f;
        Collider[] hitEnemies = Physics.OverlapSphere(hitBoxCenter, attackRadius);

        foreach (Collider col in hitEnemies)
        {
            if (col.CompareTag("Enemy"))
            {
                BanditBoss banditBoss = col.GetComponentInParent<BanditBoss>();
                if (banditBoss != null) banditBoss.TakeDamage(attackDamage);

                Bandit regularBandit = col.GetComponentInParent<Bandit>();
                if (regularBandit != null) regularBandit.TakeDamage(attackDamage);

                BossController boss = col.GetComponentInParent<BossController>();
                if (boss != null) boss.TakeDamage(attackDamage);
            }
        }
    }

    void PerformShieldBlock()
    {
        if (!hudPlayer.ConsumeStamina(parryStaminaCost)) return;

        isParrying = true;
        parryTimer = 0f;
        lastCombatInputTime = Time.time;

        if (animator != null)
        {
            animator.SetTrigger("Parry");
        }
    }

    public void TriggerDeathAnimation()
    {
        if (animator != null)
        {
            animator.SetBool("IsArmed", isArmed);
            animator.SetTrigger("Die");
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