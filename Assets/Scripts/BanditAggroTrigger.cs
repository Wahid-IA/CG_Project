using UnityEngine;

public class BanditAggroTrigger : MonoBehaviour
{
    public BanditBoss banditBossController;

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") && banditBossController != null)
        {
            banditBossController.WakeUpBoss();
        }
    }
}