using UnityEngine;
using UnityEngine.UI;

public class BanditHealthBarUI : MonoBehaviour
{
    [Header("UI References")]
    public GameObject healthBarContainer; 
    public Image bossBarFill; 
    public Image bossStaggerBarFill; 
    public BanditBoss banditBossController; 

    private bool isBarActive = false;

    void Start()
    {
        if (healthBarContainer != null) healthBarContainer.SetActive(false);
    }

    void Update()
    {
        if (banditBossController == null)
        {
            if (healthBarContainer != null && healthBarContainer.activeSelf) healthBarContainer.SetActive(false);
            return;
        }

        if (healthBarContainer == null) return;

        if (banditBossController.isAwakened && !isBarActive && !banditBossController.isDead)
        {
            isBarActive = true;
            healthBarContainer.SetActive(true);
        }

        if (isBarActive)
        {
            if (bossBarFill != null)
            {
                float healthPercentage = Mathf.Clamp01(banditBossController.currentHealth / banditBossController.maxHealth);
                bossBarFill.fillAmount = Mathf.Lerp(bossBarFill.fillAmount, healthPercentage, Time.deltaTime * 10f);
            }

            if (bossStaggerBarFill != null)
            {
                float staggerPercentage = Mathf.Clamp01(banditBossController.currentStagger / banditBossController.maxStagger);
                bossStaggerBarFill.fillAmount = Mathf.Lerp(bossStaggerBarFill.fillAmount, staggerPercentage, Time.deltaTime * 10f);
            }

            if (banditBossController.currentHealth <= 0 || banditBossController.isDead)
            {
                healthBarContainer.SetActive(false);
            }
        }
    }
}