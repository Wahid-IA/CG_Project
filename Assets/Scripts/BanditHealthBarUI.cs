using UnityEngine;
using UnityEngine.UI;

public class BanditHealthBarUI : MonoBehaviour
{
    [Header("UI References")]
    public GameObject healthBarContainer; 
    public Image bossBarFill; 
    public Bandit banditScript; // Reference to the regular bandit script

    private bool isBarActive = false;

    void Start()
    {
        if (healthBarContainer != null)
        {
            healthBarContainer.SetActive(false); // Hide bar until damaged or engaged
        }
    }

    void Update()
    {
        // Hide bar if bandit is destroyed or dead
        if (banditScript == null || banditScript.isDead)
        {
            if (healthBarContainer != null && healthBarContainer.activeSelf)
            {
                healthBarContainer.SetActive(false);
            }
            return;
        }

        if (healthBarContainer == null) return;

        // Show bar once health is reduced or combat starts
        if (banditScript.currentHealth < banditScript.maxHealth && !isBarActive)
        {
            isBarActive = true;
            healthBarContainer.SetActive(true);
        }

        // Smoothly update fill amount
        if (isBarActive && bossBarFill != null)
        {
            float healthPercentage = Mathf.Clamp01(banditScript.currentHealth / banditScript.maxHealth);
            bossBarFill.fillAmount = Mathf.Lerp(bossBarFill.fillAmount, healthPercentage, Time.deltaTime * 10f);

            // Hide when health drops to 0
            if (banditScript.currentHealth <= 0)
            {
                healthBarContainer.SetActive(false);
            }
        }
    }
}