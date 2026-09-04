using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(CharacterController))]
public class EldenRingHUDPlayer : MonoBehaviour
{
    private CharacterController controller;
    private Transform camTransform;

    [Header("Movement Stats")]
    public float walkSpeed = 4f;
    public float runSpeed = 7.5f;
    public float rotationSpeed = 12f;
    private float currentSpeed;

    [Header("Vitals (HP & Stamina)")]
    public float maxHealth = 100f;
    public float currentHealth;
    public float maxStamina = 100f;
    public float currentStamina;
    public float staminaRegenRate = 35f;
    public float rollStaminaCost = 25f;
    public float sprintStaminaCost = 15f;
    public float attackStaminaCost = 20f;

    [Header("UI Bar References (Drag & Drop)")]
    public Image healthFillImage;
    public Image staminaFillImage;

    [Header("Dodge Roll Settings")]
    public float rollSpeed = 13f;
    public float rollDuration = 0.35f;
    private bool isRolling = false;
    private float rollTimer = 0f;
    private Vector3 rollDirection;

    [Header("Melee Combat & Hitbox")]
    public float attackCooldown = 0.8f;
    private float lastAttackTime = 0f;
    public float attackRange = 2.2f;
    public float attackRadius = 1.2f;

    [Header("Target Lock Settings")]
    public float lockRange = 15f;
    private Transform currentTarget;
    private bool isLockedOn = false;

    [Header("Physics")]
    public float gravity = -9.81f;
    private Vector3 velocity;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        camTransform = Camera.main != null ? Camera.main.transform : transform;
        currentHealth = maxHealth;
        currentStamina = maxStamina;
    }

    void Update()
    {
        HandleStamina();
        UpdateUI();

        // Toggle Target Lock with 'Q' key
        if (Input.GetKeyDown(KeyCode.Q))
        {
            if (isLockedOn) UnlockTarget();
            else FindNearestEnemy();
        }

        // Maintain Target Lock Facing
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

        if (isRolling)
        {
            PerformRoll();
            return;
        }

        HandleCombat();
        HandleMovement();
    }

    void HandleMovement()
    {
        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");
        Vector3 direction = new Vector3(horizontal, 0f, vertical).normalized;

        bool isSprinting = Input.GetKey(KeyCode.LeftShift) && direction.magnitude > 0 && currentStamina > 2f;

        if (isSprinting)
        {
            currentSpeed = runSpeed;
            currentStamina -= sprintStaminaCost * Time.deltaTime;
        }
        else
        {
            currentSpeed = walkSpeed;
        }

        // Dodge Roll (Spacebar)
        if (Input.GetKeyDown(KeyCode.Space) && currentStamina >= rollStaminaCost)
        {
            Vector3 rollInput = direction.magnitude > 0 ? direction : transform.forward;
            StartRoll(rollInput);
            return;
        }

        if (direction.magnitude >= 0.1f)
        {
            Vector3 moveDir = Quaternion.Euler(0f, camTransform.eulerAngles.y, 0f) * direction;
            
            // If NOT locked on, rotate player to movement direction
            if (!isLockedOn)
            {
                float targetAngle = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg + camTransform.eulerAngles.y;
                float angle = Mathf.LerpAngle(transform.eulerAngles.y, targetAngle, rotationSpeed * Time.deltaTime);
                transform.rotation = Quaternion.Euler(0f, angle, 0f);
            }

            controller.Move(moveDir.normalized * currentSpeed * Time.deltaTime);
        }

        // Gravity
        if (controller.isGrounded && velocity.y < 0) velocity.y = -2f;
        velocity.y += gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);
    }

    void HandleCombat()
    {
        if (Input.GetMouseButtonDown(0) && Time.time >= lastAttackTime + attackCooldown)
        {
            if (currentStamina >= attackStaminaCost)
            {
                PerformMeleeAttack();
            }
            else
            {
                Debug.Log("Not enough stamina to attack!");
            }
        }
    }

    void PerformMeleeAttack()
    {
        lastAttackTime = Time.time;
        currentStamina -= attackStaminaCost;
        Debug.Log("Player swings weapon!");

        Vector3 hitBoxCenter = transform.position + transform.forward * attackRange + Vector3.up * 1f;
        Collider[] hitEnemies = Physics.OverlapSphere(hitBoxCenter, attackRadius);

        foreach (Collider col in hitEnemies)
        {
            if (col.CompareTag("Enemy"))
            {
                Debug.Log("Hit enemy: " + col.name);
                Renderer enemyRenderer = col.GetComponent<Renderer>();
                if (enemyRenderer != null)
                {
                    StartCoroutine(FlashEnemyRed(enemyRenderer));
                }
            }
        }
    }

    System.Collections.IEnumerator FlashEnemyRed(Renderer rend)
    {
        Color originalColor = rend.material.color;
        rend.material.color = Color.red;
        yield return new WaitForSeconds(0.2f);
        rend.material.color = originalColor;
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
            Debug.Log("Locked onto: " + currentTarget.name);
        }
    }

    void UnlockTarget()
    {
        isLockedOn = false;
        currentTarget = null;
        Debug.Log("Target unlocked.");
    }

    void StartRoll(Vector3 inputDir)
    {
        isRolling = true;
        rollTimer = rollDuration;
        currentStamina -= rollStaminaCost;

        float targetAngle = Mathf.Atan2(inputDir.x, inputDir.z) * Mathf.Rad2Deg + camTransform.eulerAngles.y;
        rollDirection = Quaternion.Euler(0f, targetAngle, 0f) * Vector3.forward;
    }

    void PerformRoll()
    {
        rollTimer -= Time.deltaTime;
        controller.Move(rollDirection * rollSpeed * Time.deltaTime);
        if (rollTimer <= 0f) isRolling = false;
    }

    void HandleStamina()
    {
        if (!Input.GetKey(KeyCode.LeftShift) && !isRolling)
        {
            currentStamina = Mathf.Clamp(currentStamina + staminaRegenRate * Time.deltaTime, 0f, maxStamina);
        }
    }

    void UpdateUI()
    {
        if (healthFillImage != null)
            healthFillImage.fillAmount = Mathf.Lerp(healthFillImage.fillAmount, currentHealth / maxHealth, Time.deltaTime * 10f);

        if (staminaFillImage != null)
            staminaFillImage.fillAmount = Mathf.Lerp(staminaFillImage.fillAmount, currentStamina / maxStamina, Time.deltaTime * 15f);
    }
}