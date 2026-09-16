using UnityEngine;

public class BossAggroTrigger : MonoBehaviour
{
    public BossController bossController;
    public BossHealthBarUI bossHealthBarUI;

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            if (bossController != null)
            {
                bossController.WakeUpBoss();
            }

            if (bossHealthBarUI != null)
            {
                bossHealthBarUI.SetPlayerInZone(true);
            }
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            if (bossHealthBarUI != null)
            {
                bossHealthBarUI.SetPlayerInZone(false);
            }
        }
    }
}