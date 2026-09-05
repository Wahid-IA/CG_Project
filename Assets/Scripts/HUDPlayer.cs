using UnityEngine;
using UnityEngine.UI;

public class HUDPlayer : MonoBehaviour
{
    [Header("Health Settings")]
    public float maxHealth = 100f;
    public float currentHealth;
    public Image healthFillImage;

    [Header("Stamina Settings")]
    public float maxStamina = 100f;
    public float currentStamina;
    public float staminaRegenRate = 35f;
    public Image staminaFillImage;

    public bool isDead { get; private set; } = false;
    private Animator animator;
    private SoulsPlayerController movementController;

    void Start()
    {
        animator = GetComponentInChildren<Animator>();
        movementController = GetComponent<SoulsPlayerController>();
        currentHealth = maxHealth;
        currentStamina = maxStamina;
    }

    void Update()
    {
        UpdateUI();
    }

    public bool HasStamina(float amount)
    {
        return currentStamina >= amount;
    }

    public bool ConsumeStamina(float amount)
    {
        if (currentStamina >= amount)
        {
            currentStamina -= amount;
            return true;
        }
        return false;
    }

    public void RegenStamina(bool isActionConsumingStamina)
    {
        if (!isActionConsumingStamina && !isDead)
        {
            currentStamina = Mathf.Clamp(currentStamina + staminaRegenRate * Time.deltaTime, 0f, maxStamina);
        }
    }

    public void TakeDamage(float amount, GameObject attacker = null)
    {
        if (isDead) return;

        // 1. Check Dodge Roll I-Frames
        if (movementController != null && movementController.IsInvincible)
        {
            Debug.Log("Dodged attack with I-Frames!");
            return;
        }

        // 2. Check Parry Window
        SoulsCombatSystem combat = GetComponent<SoulsCombatSystem>();
        if (combat != null && combat.IsParryActive)
        {
            Debug.Log("PARRY SUCCESSFUL!");

            // Stagger the attacker if reference was provided
            if (attacker != null)
            {
                BossController boss = attacker.GetComponent<BossController>();
                if (boss != null)
                {
                    boss.GetParried();
                }
            }
            return; // Block damage completely
        }

        // 3. Normal Damage logic
        currentHealth = Mathf.Clamp(currentHealth - amount, 0f, maxHealth);

        if (currentHealth <= 0f)
        {
            Die();
        }
    }

    private void Die()
    {
        isDead = true;

        if (animator != null)
        {
            animator.SetTrigger("Die");
        }

        CharacterController controller = GetComponent<CharacterController>();
        if (controller != null) controller.enabled = false;
    }

    private void UpdateUI()
    {
        if (healthFillImage != null)
            healthFillImage.fillAmount = Mathf.Lerp(healthFillImage.fillAmount, currentHealth / maxHealth, Time.deltaTime * 10f);

        if (staminaFillImage != null)
            staminaFillImage.fillAmount = Mathf.Lerp(staminaFillImage.fillAmount, currentStamina / maxStamina, Time.deltaTime * 15f);
    }
}