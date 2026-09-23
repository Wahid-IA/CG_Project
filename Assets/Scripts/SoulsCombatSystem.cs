using UnityEngine;
using UnityEngine.EventSystems;

[RequireComponent(typeof(HUDPlayer))]
public class SoulsCombatSystem : MonoBehaviour
{
    private HUDPlayer hudPlayer;
    private Animator animator;
    private SoulsPlayerController movementController;

    [Header("Stance Settings")]
    public bool isArmed { get; private set; } = true;

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

    public bool IsPerformingAction
    {
        get
        {
            if (isParrying) return true;
            if (animator == null) return false;

            AnimatorStateInfo currentState = animator.GetCurrentAnimatorStateInfo(0);

            if (animator.IsInTransition(0))
            {
                AnimatorStateInfo nextState = animator.GetNextAnimatorStateInfo(0);
                return IsActionState(currentState) || IsActionState(nextState);
            }

            return IsActionState(currentState) && currentState.normalizedTime < 0.95f;
        }
    }

    private bool IsActionState(AnimatorStateInfo state)
    {
        return state.IsTag("Attack") || state.IsTag("Block") ||
               state.IsName("sword and shield slash") ||
               state.IsName("sword and shield slash 3") ||
               state.IsName("sword and shield slash 4") ||
               state.IsName("Attack State") ||
               state.IsName("Shield_Block_Enter") ||
               state.IsName("Sword_Block_Exit");
    }

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
        
        isArmed = true;
    }

    void Update()
    {
        if (hudPlayer != null && hudPlayer.isDead) return;
        if (InGameMainMenu.isMainMenuActive || PauseMenu.isPaused) return;
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return;

        HandleTargetLock();

        if (movementController != null && movementController.isRolling) return;

        if (Input.GetMouseButtonDown(1) && !IsPerformingAction)
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
        if (Time.time - lastAttackTime > comboResetWindow && currentCombo > 0)
        {
            currentCombo = 0;
            if (animator != null) animator.SetInteger("Combo", 0);
        }

        if (Input.GetMouseButtonDown(0))
        {
            if (Time.time >= lastAttackTime + attackCooldown && hudPlayer.HasStamina(attackStaminaCost))
            {
                PerformMeleeAttack();
            }
        }
    }

    public void CancelActions()
    {
        isParrying = false;
        parryTimer = 0f;

        if (animator != null)
        {
            animator.ResetTrigger("Attack");
            animator.ResetTrigger("Parry");
        }
    }

    void PerformMeleeAttack()
    {
        if (!hudPlayer.ConsumeStamina(attackStaminaCost)) return;

        lastAttackTime = Time.time;
        currentCombo = (currentCombo % 3) + 1;

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

                GoblinBoss goblinBoss = col.GetComponentInParent<GoblinBoss>();
                if (goblinBoss != null) goblinBoss.TakeDamage(attackDamage);

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

        if (animator != null)
        {
            animator.SetTrigger("Parry");
        }
    }

    public void TriggerDeathAnimation()
    {
        if (animator != null)
        {
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