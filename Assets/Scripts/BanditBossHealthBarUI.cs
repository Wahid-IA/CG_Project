using UnityEngine;
using UnityEngine.UI;

public class BanditBossHealthBarUI : MonoBehaviour
{
    [Header("UI References")]
    public GameObject healthBarContainer; 
    public Image bossBarFill; 
    public Image bossStaggerBarFill; 
    public BanditBoss banditBoss; // Points specifically to the Bandit King script

    private bool isBarActive = false;

    void Start()
    {
        if (healthBarContainer != null)
        {
            healthBarContainer.SetActive(false); // Hide bar until boss awakens
        }
        
        // Safety check to ensure the canvas itself is enabled
        Canvas canvas = GetComponent<Canvas>();
        if (canvas != null) canvas.enabled = true;
    }
    void Update()
    {
        // Hide bar if boss is destroyed
        if (banditBoss == null)
        {
            if (healthBarContainer != null && healthBarContainer.activeSelf)
            {
                healthBarContainer.SetActive(false);
            }
            return;
        }

        if (healthBarContainer == null) return;

        // Show bar when boss wakes up
        if (banditBoss.isAwakened && !isBarActive && !banditBoss.isDead)
        {
            isBarActive = true;
            healthBarContainer.SetActive(true);
        }

        // Smoothly update fill amounts based on health and stagger
        if (isBarActive)
        {
            // Health Fill
            if (bossBarFill != null)
            {
                float healthPercentage = Mathf.Clamp01(banditBoss.currentHealth / banditBoss.maxHealth);
                bossBarFill.fillAmount = Mathf.Lerp(bossBarFill.fillAmount, healthPercentage, Time.deltaTime * 10f);
            }

            // Stagger Fill
            if (bossStaggerBarFill != null)
            {
                float staggerPercentage = Mathf.Clamp01(banditBoss.currentStagger / banditBoss.maxStagger);
                bossStaggerBarFill.fillAmount = Mathf.Lerp(bossStaggerBarFill.fillAmount, staggerPercentage, Time.deltaTime * 10f);
            }

            // Hide when boss dies or health drops to 0 (Only after it has been activated)
            if (isBarActive && (banditBoss.currentHealth <= 0 || banditBoss.isDead))
            {
                healthBarContainer.SetActive(false);
            }
        }
    }
}