using UnityEngine;
using UnityEngine.UI;

public class BossHealthBarUI : MonoBehaviour
{
    [Header("References")]
    public GameObject healthBarContainer; 
    public Image bossBarFill;             
    public BossController bossController; 

    private bool isBarActive = false;

    void Start()
    {
        if (healthBarContainer != null)
        {
            healthBarContainer.SetActive(false); // Hide on start
        }
        else
        {
            Debug.LogError("BossHealthBarUI: Health Bar Container is NOT assigned!");
        }

        if (bossController == null)
        {
            Debug.LogError("BossHealthBarUI: Boss Controller reference is missing on BossHUDManager!");
        }
    }

    void Update()
    {
        if (bossController == null || bossBarFill == null || healthBarContainer == null) return;

        // Check if boss has awakened
        if (bossController.isAwakened && !isBarActive)
        {
            isBarActive = true;
            healthBarContainer.SetActive(true);
            Debug.Log("Boss health bar activated on screen!");
        }

        // Smoothly update fill amount
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