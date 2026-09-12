using UnityEngine;
using UnityEngine.UI;

public class BanditBossHealthBarUI : MonoBehaviour
{
    [Header("UI References")]
    public GameObject healthBarContainer; 
    public Image bossBarFill; 
    public Image bossStaggerBarFill; 
    public BanditBoss banditBoss; 

    void Start()
    {
        // Keep the health bar container active immediately on start
        if (healthBarContainer != null)
        {
            healthBarContainer.SetActive(true); 
        }
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

        // Ensure container stays active
        if (!healthBarContainer.activeSelf && !banditBoss.isDead)
        {
            healthBarContainer.SetActive(true);
        }

        // Smoothly update fill amounts based on health and stagger
        if (bossBarFill != null)
        {
            float healthPercentage = Mathf.Clamp01(banditBoss.currentHealth / banditBoss.maxHealth);
            bossBarFill.fillAmount = Mathf.Lerp(bossBarFill.fillAmount, healthPercentage, Time.deltaTime * 10f);
        }

        if (bossStaggerBarFill != null)
        {
            float staggerPercentage = Mathf.Clamp01(banditBoss.currentStagger / banditBoss.maxStagger);
            bossStaggerBarFill.fillAmount = Mathf.Lerp(bossStaggerBarFill.fillAmount, staggerPercentage, Time.deltaTime * 10f);
        }

        // Hide only when boss dies or health drops to 0
        if (banditBoss.currentHealth <= 0 || banditBoss.isDead)
        {
            healthBarContainer.SetActive(false);
        }
    }
}