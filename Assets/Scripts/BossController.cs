using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class BossController : MonoBehaviour
{
    [Header("Target Reference")]
    public Transform playerTransform;
    private HUDPlayer playerScript;

    [Header("Boss Stats")]
    public float maxHealth = 300f;
    public float currentHealth;
    public float moveSpeed = 4f;        
    public float rotationSpeed = 10f;
    public float gravity = 9.81f; 
    public bool isDead { get; private set; } = false;

    [Header("Combat Settings")]
    public float attackRange = 2.5f;
    public float attackCooldown = 1.5f;   
    private float lastAttackTime = 0f;
    public float attackDamage = 15f;

    [Header("Stagger System")]
    public float maxStagger = 100f;
    public float currentStagger = 0f;
    public float defaultStaggerPerHit = 25f;
    public float staggerDecayRate = 5f; 
    public float staggerDuration = 3f;  
    public bool isStaggered { get; private set; } = false;
    private float staggerTimer = 0f;

    [Header("Awakening State")]
    public bool isAwakened = false;     

    [Header("Animation Settings")]
    public float speedDampTime = 0.15f;
    private Animator animator;

    [Header("Visuals")]
    public Renderer bossRenderer;

    private CharacterController controller;
    private float verticalVelocity = 0f;

    // --- Non-Repeating Deck Tracking System ---
    private int lastAttackIndex = -1;
    private bool hasTriggeredPhase2Jump = false;
    private List<int> phase1Bag = new List<int>();
    private List<int> phase2Bag = new List<int>();

    void Start()
    {
        currentHealth = maxHealth;
        animator = GetComponentInChildren<Animator>();
        controller = GetComponent<CharacterController>();

        FindPlayerReference();

        if (bossRenderer == null)
        {
            bossRenderer = GetComponentInChildren<Renderer>();
        }
    }

    void Update()
    {
        if (isDead) return;

        // Keep player reference updated
        if (playerScript == null)
        {
            FindPlayerReference();
        }

        // --- IMMEDIATELY HALT EVERYTHING IF PLAYER IS DEAD OR MISSING ---
        if (playerScript == null || playerScript.isDead)
        {
            StopAttackingAndMovement();
            ApplyGravityOnly();
            return;
        }

        // Continuous Gravity
        if (controller.isGrounded && verticalVelocity < 0)
        {
            verticalVelocity = -2f; 
        }
        else
        {
            verticalVelocity -= gravity * Time.deltaTime;
        }

        // Handle Stagger State
        if (isStaggered)
        {
            staggerTimer -= Time.deltaTime;
            StopAttackingAndMovement();
            ApplyGravityOnly();

            if (staggerTimer <= 0f)
            {
                isStaggered = false;
                currentStagger = 0f; 

                if (animator != null)
                {
                    animator.SetBool("IsStagger", false); 
                }
            }
            return; 
        }

        // Stagger Decay
        if (currentStagger > 0f)
        {
            currentStagger = Mathf.Clamp(currentStagger - staggerDecayRate * Time.deltaTime, 0f, maxStagger);
        }

        if (!isAwakened || playerTransform == null) 
        {
            StopAttackingAndMovement();
            ApplyGravityOnly();
            return;
        }

        // --- MID-ATTACK LOCK ---
        // If boss is currently performing an attack animation, freeze movement and clear triggers
        if (IsAttacking())
        {
            if (animator != null)
            {
                animator.ResetTrigger("Attack");
            }
            UpdateAnimationSpeed(0f);
            ApplyGravityOnly();
            return;
        }

        float distanceToPlayer = Vector3.Distance(transform.position, playerTransform.position);

        // Face towards Player
        Vector3 dirToPlayer = (playerTransform.position - transform.position).normalized;
        dirToPlayer.y = 0;
        if (dirToPlayer != Vector3.zero)
        {
            Quaternion lookRot = Quaternion.LookRotation(dirToPlayer);
            transform.rotation = Quaternion.Slerp(transform.rotation, lookRot, rotationSpeed * Time.deltaTime);
        }

        Vector3 moveVelocity = Vector3.zero;
        float targetAnimSpeed = 0f;

        // --- DISTANCE CHECK & COMBAT LOGIC ---
        if (distanceToPlayer > attackRange)
        {
            // Player is out of attack range: Move closer, ensure attack triggers & indices are cleared
            if (animator != null)
            {
                animator.ResetTrigger("Attack");
                animator.SetInteger("AttackIndex", 0);
            }

            moveVelocity = dirToPlayer * moveSpeed;
            targetAnimSpeed = 1f;
        }
        else
        {
            // Player is within attack range: Stop moving and attempt attack if cooldown is ready
            targetAnimSpeed = 0f;

            if (Time.time >= lastAttackTime + attackCooldown)
            {
                PerformBossAttack();
                return;
            }
        }

        moveVelocity.y = verticalVelocity;
        controller.Move(moveVelocity * Time.deltaTime);

        UpdateAnimationSpeed(targetAnimSpeed);
    }

    void FindPlayerReference()
    {
        if (playerTransform == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null) playerTransform = playerObj.transform;
        }

        if (playerTransform != null)
        {
            playerScript = playerTransform.GetComponent<HUDPlayer>() ??
                           playerTransform.GetComponentInParent<HUDPlayer>() ?? 
                           playerTransform.GetComponentInChildren<HUDPlayer>();
        }

        if (playerScript == null)
        {
            playerScript = FindFirstObjectByType<HUDPlayer>();
        }
    }

    void ApplyGravityOnly()
    {
        if (controller != null && controller.enabled)
        {
            controller.Move(new Vector3(0, verticalVelocity, 0) * Time.deltaTime);
        }
    }

    void StopAttackingAndMovement()
    {
        UpdateAnimationSpeed(0f);
        if (animator != null)
        {
            animator.ResetTrigger("Attack");
            animator.SetInteger("AttackIndex", 0);
        }
    }

    void PerformBossAttack()
    {
        if (playerScript == null || playerScript.isDead) return;
        if (IsAttacking()) return;

        lastAttackTime = Time.time;
        int attackIndex = ChooseNextAttack();

        if (animator != null)
        {
            animator.ResetTrigger("Attack");
            animator.SetInteger("AttackIndex", attackIndex);
            animator.SetTrigger("Attack");
        }

        if (playerScript != null && !playerScript.isDead)
        {
            playerScript.TakeDamage(attackDamage, gameObject);
        }
    }

    private bool IsAttacking()
    {
        if (animator == null) return false;

        AnimatorStateInfo currentState = animator.GetCurrentAnimatorStateInfo(0);

        if (animator.IsInTransition(0))
        {
            AnimatorStateInfo nextState = animator.GetNextAnimatorStateInfo(0);
            if (IsAttackState(currentState) || IsAttackState(nextState))
            {
                return true;
            }
        }
        else if (IsAttackState(currentState))
        {
            if (currentState.normalizedTime < 0.95f)
            {
                return true;
            }
        }

        return false;
    }

    private bool IsAttackState(AnimatorStateInfo stateInfo)
    {
        return stateInfo.IsTag("Attack") ||
               stateInfo.IsName("One Hand Club Combo") || 
               stateInfo.IsName("Standing Melee Combat") || 
               stateInfo.IsName("Dual Weapon Combo") || 
               stateInfo.IsName("Jump Attack");
    }

    private int ChooseNextAttack()
    {
        bool isPhase2 = (currentHealth / maxHealth) <= 0.5f;

        // Guaranteed Jump Attack (4) ONCE when dropping <= 50% HP
        if (isPhase2 && !hasTriggeredPhase2Jump)
        {
            hasTriggeredPhase2Jump = true;
            lastAttackIndex = 4;
            phase2Bag.Clear();
            return 4;
        }

        // --- PHASE 1 LOGIC (> 50% HP) ---
        if (!isPhase2)
        {
            if (phase1Bag.Count == 0)
            {
                phase1Bag.Add(1);
                phase1Bag.Add(2);
            }

            int pickIndex = Random.Range(0, phase1Bag.Count);
            if (phase1Bag.Count > 1 && phase1Bag[pickIndex] == lastAttackIndex)
            {
                pickIndex = (pickIndex + 1) % phase1Bag.Count;
            }

            int chosen = phase1Bag[pickIndex];
            phase1Bag.RemoveAt(pickIndex);
            lastAttackIndex = chosen;
            return chosen;
        }

        // --- PHASE 2 LOGIC (<= 50% HP) ---
        if (phase2Bag.Count == 0)
        {
            if (lastAttackIndex != 4)
            {
                lastAttackIndex = 4;
                phase2Bag.Add(1);
                phase2Bag.Add(2);
                phase2Bag.Add(3);
                return 4;
            }
            else
            {
                phase2Bag.Add(1);
                phase2Bag.Add(2);
                phase2Bag.Add(3);
            }
        }

        int pIndex = Random.Range(0, phase2Bag.Count);
        if (phase2Bag.Count > 1 && phase2Bag[pIndex] == lastAttackIndex)
        {
            pIndex = (pIndex + 1) % phase2Bag.Count;
        }

        int chosenPhase2 = phase2Bag[pIndex];
        phase2Bag.RemoveAt(pIndex);
        lastAttackIndex = chosenPhase2;
        return chosenPhase2;
    }

    public void AddStagger(float amount)
    {
        if (isDead || isStaggered) return;

        currentStagger = Mathf.Clamp(currentStagger + amount, 0f, maxStagger);

        if (currentStagger >= maxStagger)
        {
            TriggerStagger();
        }
    }

    private void TriggerStagger()
    {
        isStaggered = true;
        staggerTimer = staggerDuration;

        if (animator != null)
        {
            animator.SetBool("IsStagger", true); 
        }

        Debug.Log("Boss staggered!");
    }

    public void GetParried()
    {
        if (isDead) return;
        AddStagger(maxStagger * 0.5f);
    }

    public void TakeDamage(float damageAmount)
    {
        TakeDamage(damageAmount, defaultStaggerPerHit);
    }

    public void TakeDamage(float damageAmount, float customStaggerAmount)
    {
        if (isDead) return;

        isAwakened = true; 
        currentHealth -= damageAmount;

        AddStagger(customStaggerAmount);

        if (bossRenderer != null)
        {
            StartCoroutine(FlashColor());
        }

        if (currentHealth <= 0)
        {
            Die();
        }
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
            animator.ResetTrigger("Attack");
            animator.SetInteger("AttackIndex", 0);
            animator.SetBool("IsStagger", false); 
            animator.SetTrigger("Die");
        }

        Collider[] colliders = GetComponentsInChildren<Collider>();
        foreach (Collider col in colliders)
        {
            col.enabled = false;
        }

        Destroy(gameObject, 3f);
    }
}