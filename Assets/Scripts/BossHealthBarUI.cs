using UnityEngine;
using UnityEngine.UI;

public class BossHealthBarUI : MonoBehaviour
{
    [Header("UI References")]
    public GameObject healthBarContainer; 
    public Image bossBarFill; 
    public Image bossStaggerBarFill; 
    public BossController bossController; 

    private bool isBarActive = false;

    void Start()
    {
        if (healthBarContainer != null)
        {
            healthBarContainer.SetActive(false); // Hide bar until boss awakens
        }
    }

    void Update()
    {
        // Hide bar if boss is destroyed
        if (bossController == null)
        {
            if (healthBarContainer != null && healthBarContainer.activeSelf)
            {
                healthBarContainer.SetActive(false);
            }
            return;
        }

        if (healthBarContainer == null) return;

        // Show bar when boss wakes up
        if (bossController.isAwakened && !isBarActive && !bossController.isDead)
        {
            isBarActive = true;
            healthBarContainer.SetActive(true);
        }

        // Smoothly update fill amounts based on boss health and stagger
        if (isBarActive)
        {
            // Health Fill
            if (bossBarFill != null)
            {
                float healthPercentage = Mathf.Clamp01(bossController.currentHealth / bossController.maxHealth);
                bossBarFill.fillAmount = Mathf.Lerp(bossBarFill.fillAmount, healthPercentage, Time.deltaTime * 10f);
            }

            // Stagger Fill
            if (bossStaggerBarFill != null)
            {
                float staggerPercentage = Mathf.Clamp01(bossController.currentStagger / bossController.maxStagger);
                bossStaggerBarFill.fillAmount = Mathf.Lerp(bossStaggerBarFill.fillAmount, staggerPercentage, Time.deltaTime * 10f);
            }

            // Hide when boss dies or health drops to 0
            if (bossController.currentHealth <= 0 || bossController.isDead)
            {
                healthBarContainer.SetActive(false);
            }
        }
    }
}