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
    private bool isPlayerInZone = false;

    void Start()
    {
        if (healthBarContainer != null)
        {
            healthBarContainer.SetActive(false); // Hide bar until boss awakens
        }
    }

    public void SetPlayerInZone(bool inZone)
    {
        isPlayerInZone = inZone;
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

        // Show bar only when boss is awakened, alive, and player is inside trigger zone
        if (bossController.isAwakened && isPlayerInZone && !bossController.isDead)
        {
            if (!isBarActive)
            {
                isBarActive = true;
                healthBarContainer.SetActive(true);
            }
        }
        else
        {
            // Hide bar if player leaves zone, boss dies, or health hits 0
            if (isBarActive)
            {
                isBarActive = false;
                healthBarContainer.SetActive(false);
            }
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
        }
    }
}