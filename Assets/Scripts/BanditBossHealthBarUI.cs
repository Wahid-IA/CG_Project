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
        if (healthBarContainer != null)
        {
            healthBarContainer.SetActive(true); 
        }
    }

    void Update()
    {
        if (banditBoss == null)
        {
            if (healthBarContainer != null && healthBarContainer.activeSelf)
            {
                healthBarContainer.SetActive(false);
            }
            return;
        }

        if (healthBarContainer == null) return;

        if (!healthBarContainer.activeSelf && !banditBoss.isDead)
        {
            healthBarContainer.SetActive(true);
        }

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

        if (banditBoss.currentHealth <= 0 || banditBoss.isDead)
        {
            healthBarContainer.SetActive(false);
        }
    }
}