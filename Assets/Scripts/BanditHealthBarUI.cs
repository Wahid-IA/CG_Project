using UnityEngine;
using UnityEngine.UI;

public class BanditHealthBarUI : MonoBehaviour
{
    [Header("UI References")]
    public GameObject healthBarContainer; 
    public Image banditBarFill; 
    public Bandit banditScript; 

    void Start()
    {
        if (healthBarContainer != null) healthBarContainer.SetActive(false);
    }

    void Update()
    {
        if (banditScript == null || healthBarContainer == null) return;

        // Show bar only when aggroed and alive
        if (banditScript.isAggroed && !banditScript.isDead && banditScript.currentHealth > 0)
        {
            if (!healthBarContainer.activeSelf) healthBarContainer.SetActive(true);

            if (banditBarFill != null)
            {
                float healthPercentage = Mathf.Clamp01(banditScript.currentHealth / banditScript.maxHealth);
                banditBarFill.fillAmount = Mathf.Lerp(banditBarFill.fillAmount, healthPercentage, Time.deltaTime * 10f);
            }
        }
        else
        {
            if (healthBarContainer.activeSelf) healthBarContainer.SetActive(false);
        }
    }
}