using UnityEngine;

public class BossAggroTrigger : MonoBehaviour
{
    public BossController bossController;

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") && bossController != null)
        {
            bossController.WakeUpBoss();
        }
    }
}