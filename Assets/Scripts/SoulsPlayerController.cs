using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class SoulsPlayerController : MonoBehaviour
{
    private CharacterController controller;
    private Transform camTransform;
    private Animator animator;

    [Header("Movement Settings")]
    public float walkSpeed = 5f;
    public float runSpeed = 9f;
    public float rotationSpeed = 15f;
    private float currentSpeed;

    [Header("Animation Settings")]
    [Tooltip("Time in seconds to smoothly damp between animation states (Idle, Walk, Sprint)")]
    public float speedDampTime = 0.15f;

    [Header("Stamina System")]
    public float maxStamina = 100f;
    public float currentStamina;
    public float staminaRegenRate = 30f;
    public float rollStaminaCost = 25f;
    public float sprintStaminaCost = 20f;
    public float attackStaminaCost = 15f;

    [Header("Combat Settings")]
    public float attackCooldown = 0.8f;
    private float lastAttackTime = 0f;

    [Header("Dodge Roll Settings")]
    public float rollSpeed = 14f;
    public float rollDuration = 0.35f;
    private bool isRolling = false;
    private float rollTimer = 0f;
    private Vector3 rollDirection;

    [Header("Physics")]
    public float gravity = -9.81f;
    private Vector3 velocity;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        
        // Searches root and child GameObjects for the Animator component
        animator = GetComponentInChildren<Animator>();

        if (Camera.main != null)
        {
            camTransform = Camera.main.transform;
        }
        else
        {
            camTransform = transform;
        }

        currentStamina = maxStamina;
    }

    void Update()
    {
        HandleStamina();

        if (isRolling)
        {
            PerformRoll();
            return;
        }

        HandleCombat();
        HandleMovement();
    }

    void HandleCombat()
    {
        // Trigger attack on Left Mouse Click if off cooldown and stamina is available
        if (Input.GetMouseButtonDown(0) && Time.time >= lastAttackTime + attackCooldown && currentStamina >= attackStaminaCost)
        {
            lastAttackTime = Time.time;
            currentStamina -= attackStaminaCost;

            if (animator != null)
            {
                animator.SetTrigger("Attack");
            }
        }
    }

    void HandleMovement()
    {
        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");
        Vector3 direction = new Vector3(horizontal, 0f, vertical).normalized;

        // Check Sprint Input (Left Shift)
        bool isSprinting = Input.GetKey(KeyCode.LeftShift) && direction.magnitude > 0 && currentStamina > 5f;

        if (isSprinting)
        {
            currentSpeed = runSpeed;
            currentStamina -= sprintStaminaCost * Time.deltaTime;
        }
        else
        {
            currentSpeed = walkSpeed;
        }

        // Calculate Target Animation Speed (Idle = 0.0, Walk = 0.3, Sprint = 1.0)
        float animSpeedTarget = 0f;
        if (direction.magnitude >= 0.1f)
        {
            animSpeedTarget = isSprinting ? 1.0f : 0.3f;
        }

        // Smoothly update the Animator Speed parameter
        if (animator != null)
        {
            animator.SetFloat("Speed", animSpeedTarget, speedDampTime, Time.deltaTime);
        }

        // Dodge roll input (Spacebar)
        if (Input.GetKeyDown(KeyCode.Space) && currentStamina >= rollStaminaCost)
        {
            Vector3 rollInput = direction.magnitude > 0 ? direction : transform.forward;
            StartRoll(rollInput);
            return;
        }

        if (direction.magnitude >= 0.1f)
        {
            float targetAngle = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg + camTransform.eulerAngles.y;
            float angle = Mathf.LerpAngle(transform.eulerAngles.y, targetAngle, rotationSpeed * Time.deltaTime);
            transform.rotation = Quaternion.Euler(0f, angle, 0f);

            Vector3 moveDir = Quaternion.Euler(0f, targetAngle, 0f) * Vector3.forward;
            controller.Move(moveDir.normalized * currentSpeed * Time.deltaTime);
        }

        // Apply Gravity
        if (controller.isGrounded && velocity.y < 0)
        {
            velocity.y = -2f;
        }
        velocity.y += gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);
    }

    void StartRoll(Vector3 inputDir)
    {
        isRolling = true;
        rollTimer = rollDuration;
        currentStamina -= rollStaminaCost;
        Debug.Log("Dodge Roll Executed! Remaining Stamina: " + currentStamina);

        float targetAngle = Mathf.Atan2(inputDir.x, inputDir.z) * Mathf.Rad2Deg + camTransform.eulerAngles.y;
        rollDirection = Quaternion.Euler(0f, targetAngle, 0f) * Vector3.forward;

        // Reset movement speed in animator during roll
        if (animator != null)
        {
            animator.SetFloat("Speed", 0f, 0.05f, Time.deltaTime);
        }
    }

    void PerformRoll()
    {
        rollTimer -= Time.deltaTime;
        controller.Move(rollDirection * rollSpeed * Time.deltaTime);

        if (rollTimer <= 0f)
        {
            isRolling = false;
        }
    }

    void HandleStamina()
    {
        if (!Input.GetKey(KeyCode.LeftShift) && !isRolling)
        {
            currentStamina = Mathf.Clamp(currentStamina + staminaRegenRate * Time.deltaTime, 0f, maxStamina);
        }
    }
}