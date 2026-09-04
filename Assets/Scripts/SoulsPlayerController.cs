using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class SoulsPlayerController : MonoBehaviour
{
    private CharacterController controller;
    private Transform camTransform;
    private Animator animator; // <--- ADDED: Animator Reference

    [Header("Movement Settings")]
    public float walkSpeed = 1.75f;
    public float runSpeed = 5f;
    public float rotationSpeed = 15f;
    private float currentSpeed;

    [Header("Stamina System")]
    public float maxStamina = 100f;
    public float currentStamina;
    public float staminaRegenRate = 30f;
    public float rollStaminaCost = 25f;
    public float sprintStaminaCost = 20f;

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
        
        // <--- ADDED: Finds Animator on child FBX model
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

        HandleMovement();
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

        // <--- ADDED: Calculate & Send Speed parameter to Animator
        float animSpeed = 0f;
        if (direction.magnitude >= 0.1f)
        {
            animSpeed = isSprinting ? 1.0f : 0.5f; // 1.0 triggers Run (>0.7), 0.5 triggers Walk (>0.1)
        }

        if (animator != null)
        {
            animator.SetFloat("Speed", animSpeed);
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

        // <--- ADDED: Trigger Roll Animation in Animator
        if (animator != null)
        {
            animator.SetTrigger("Roll");
        }

        float targetAngle = Mathf.Atan2(inputDir.x, inputDir.z) * Mathf.Rad2Deg + camTransform.eulerAngles.y;
        
        // Instantly rotate player toward roll direction
        transform.rotation = Quaternion.Euler(0f, targetAngle, 0f);
        rollDirection = Quaternion.Euler(0f, targetAngle, 0f) * Vector3.forward;
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