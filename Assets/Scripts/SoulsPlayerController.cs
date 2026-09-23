using UnityEngine;

[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(HUDPlayer))]
public class SoulsPlayerController : MonoBehaviour
{
    private CharacterController controller;
    private Transform camTransform;
    private Animator animator;
    private HUDPlayer hudPlayer;
    private SoulsCombatSystem combatSystem;

    [Header("Movement Stats")]
    public float walkSpeed = 4f;
    public float runSpeed = 7.5f;
    public float rotationSpeed = 12f;
    public float speedDampTime = 0.15f;
    private float currentSpeed;

    [Header("Dodge Roll Settings")]
    public float rollSpeed = 8.25f;
    public float rollDuration = 0.55f;
    [Tooltip("Immunity duration from roll start")]
    public float iFrameDuration = 0.35f; 
    public float rollStaminaCost = 25f;
    public float sprintStaminaCost = 15f;

    public bool isRolling { get; private set; } = false;
    private float rollTimer = 0f;
    private Vector3 rollDirection;

    public bool IsInvincible => isRolling && (rollDuration - rollTimer) <= iFrameDuration;

    [Header("Physics")]
    public float gravity = -9.81f;
    private Vector3 velocity;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        animator = GetComponentInChildren<Animator>();
        hudPlayer = GetComponent<HUDPlayer>();
        combatSystem = GetComponent<SoulsCombatSystem>();
        camTransform = Camera.main != null ? Camera.main.transform : transform;
    }

    void Update()
    {
        if (hudPlayer != null && hudPlayer.isDead) return;

        bool inCombat = combatSystem != null && combatSystem.isInCombat;
        bool isSprintingInput = Input.GetKey(KeyCode.LeftShift);

        hudPlayer.RegenStamina((isSprintingInput && inCombat) || isRolling);

        if (animator != null && combatSystem != null)
        {
            animator.SetBool("IsArmed", combatSystem.isArmed);
        }

        if (isRolling)
        {
            PerformRoll();
            return;
        }

        HandleMovement();
    }

    void HandleMovement()
    {
        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");
        Vector3 direction = new Vector3(horizontal, 0f, vertical).normalized;

        // --- 1. ROLL INPUT (Evaluated FIRST to enable roll-canceling mid-attack) ---
        if (Input.GetKeyDown(KeyCode.Space) && hudPlayer.HasStamina(rollStaminaCost))
        {
            Vector3 rollInput = direction.magnitude > 0 ? direction : transform.forward;
            StartRoll(rollInput);
            return;
        }

        // --- 2. ACTION LOCK (Prevents regular movement while attacking/blocking) ---
        if (combatSystem != null && combatSystem.IsPerformingAction)
        {
            if (animator != null)
            {
                animator.SetFloat("Speed", 0f, speedDampTime, Time.deltaTime);
            }

            ApplyGravity();
            return;
        }

        bool inCombat = combatSystem != null && combatSystem.isInCombat;
        bool hasStaminaToSprint = !inCombat || hudPlayer.HasStamina(2f);
        bool isSprinting = Input.GetKey(KeyCode.LeftShift) && direction.magnitude > 0 && hasStaminaToSprint;

        if (isSprinting)
        {
            currentSpeed = runSpeed;
            if (inCombat)
            {
                hudPlayer.ConsumeStamina(sprintStaminaCost * Time.deltaTime);
            }
        }
        else
        {
            currentSpeed = walkSpeed;
        }

        float animSpeedTarget = direction.magnitude >= 0.1f ? (isSprinting ? 1.0f : 0.3f) : 0f;
        if (animator != null)
        {
            animator.SetFloat("Speed", animSpeedTarget, speedDampTime, Time.deltaTime);
        }

        if (direction.magnitude >= 0.1f)
        {
            Vector3 moveDir = Quaternion.Euler(0f, camTransform.eulerAngles.y, 0f) * direction;

            if (combatSystem == null || !combatSystem.isLockedOn)
            {
                float targetAngle = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg + camTransform.eulerAngles.y;
                float angle = Mathf.LerpAngle(transform.eulerAngles.y, targetAngle, rotationSpeed * Time.deltaTime);
                transform.rotation = Quaternion.Euler(0f, angle, 0f);
            }

            controller.Move(moveDir.normalized * currentSpeed * Time.deltaTime);
        }

        ApplyGravity();
    }

    void ApplyGravity()
    {
        if (controller.isGrounded && velocity.y < 0) velocity.y = -2f;
        velocity.y += gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);
    }

    void StartRoll(Vector3 inputDir)
    {
        if (!hudPlayer.ConsumeStamina(rollStaminaCost)) return;

        // Cancel any pending/active attack or parry
        if (combatSystem != null)
        {
            combatSystem.CancelActions();
        }

        isRolling = true;
        rollTimer = rollDuration;

        float targetAngle = Mathf.Atan2(inputDir.x, inputDir.z) * Mathf.Rad2Deg + camTransform.eulerAngles.y;
        rollDirection = Quaternion.Euler(0f, targetAngle, 0f) * Vector3.forward;

        if (animator != null)
        {
            animator.SetFloat("Speed", 0f, 0.05f, Time.deltaTime);
            animator.SetTrigger("Roll");
        }
    }

    void PerformRoll()
    {
        rollTimer -= Time.deltaTime;
        float progress = 1f - Mathf.Clamp01(rollTimer / rollDuration);
        float currentRollSpeed = rollSpeed * Mathf.Sin(progress * Mathf.PI);

        controller.Move(rollDirection * currentRollSpeed * Time.deltaTime);

        if (rollTimer <= 0f) isRolling = false;
    }
}