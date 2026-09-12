using UnityEngine;
using UnityEngine.UI;

public class BanditHealthBarUI : MonoBehaviour
{
    [Header("UI References")]
    public GameObject healthBarContainer; 
    public Image bossBarFill; 
    public Image bossStaggerBarFill; 
    public BanditBoss banditBossController; 
    private Camera mainCam;

    void Start()
    {
        mainCam = Camera.main;
        if (healthBarContainer != null) healthBarContainer.SetActive(false);
    }

    void LateUpdate()
    {
        // Keep the health bar constantly facing the player camera
        if (mainCam != null)
        {
            transform.LookAt(transform.position + mainCam.transform.rotation * Vector3.forward,
                             mainCam.transform.rotation * Vector3.up);
        }

        if (banditBossController == null)
        {
            if (healthBarContainer != null && healthBarContainer.activeSelf) healthBarContainer.SetActive(false);
            return;
        }

        if (healthBarContainer == null) return;

        // Reveal the health bar the moment the boss wakes up
        if (banditBossController.isAwakened && !healthBarContainer.activeSelf && !banditBossController.isDead)
        {
            healthBarContainer.SetActive(true);
        }

        if (healthBarContainer.activeSelf)
        {
            if (bossBarFill != null)
            {
                float healthPct = Mathf.Clamp01(banditBossController.currentHealth / banditBossController.maxHealth);
                bossBarFill.fillAmount = Mathf.Lerp(bossBarFill.fillAmount, healthPct, Time.deltaTime * 10f);
            }

            if (bossStaggerBarFill != null)
            {
                float staggerPct = Mathf.Clamp01(banditBossController.currentStagger / banditBossController.maxStagger);
                bossStaggerBarFill.fillAmount = Mathf.Lerp(bossStaggerBarFill.fillAmount, staggerPct, Time.deltaTime * 10f);
            }

            if (banditBossController.currentHealth <= 0 || banditBossController.isDead)
            {
                healthBarContainer.SetActive(false);
            }
        }
    }
}