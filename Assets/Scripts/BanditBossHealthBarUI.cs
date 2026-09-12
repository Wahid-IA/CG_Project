using UnityEngine;
using UnityEngine.UI;

public class BanditBossHealthBarUI : MonoBehaviour
{
    [Header("UI References")]
    public GameObject healthBarContainer; 
    public Image bossBarFill; 
    public Image bossStaggerBarFill; 
    public BanditBoss banditBoss; 

    private bool isBarActive = false;

    void Start()
    {
        // Hide health bar initially until boss is triggered/awakened
        if (healthBarContainer != null)
        {
            healthBarContainer.SetActive(false); 
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

        // Show the health bar the moment the boss awakens and isn't dead
        if (banditBoss.isAwakened && !isBarActive && !banditBoss.isDead)
        {
            isBarActive = true;
            healthBarContainer.SetActive(true);
        }

        // Only update fill amounts and handle shrinking if the bar is currently active
        if (isBarActive)
        {
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

            // Hide when boss dies or health drops to 0
            if (banditBoss.currentHealth <= 0 || banditBoss.isDead)
            {
                healthBarContainer.SetActive(false);
            }
        }
    }
}