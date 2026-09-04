using UnityEngine;
using UnityEngine.UI;

public class BossHealthBarUI : MonoBehaviour
{
    [Header("UI References")]
    public GameObject healthBarContainer; 
    public Image bossBarFill;             
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
        if (bossController == null || bossBarFill == null || healthBarContainer == null) return;

        // Show bar when boss wakes up
        if (bossController.isAwakened && !isBarActive)
        {
            isBarActive = true;
            healthBarContainer.SetActive(true);
        }

        // Smoothly update fill amount based on boss health
        if (isBarActive)
        {
            float healthPercentage = bossController.currentHealth / bossController.maxHealth;
            bossBarFill.fillAmount = Mathf.Lerp(bossBarFill.fillAmount, healthPercentage, Time.deltaTime * 10f);

            // Hide when boss dies
            if (bossController.currentHealth <= 0)
            {
                healthBarContainer.SetActive(false);
            }
        }
    }
}