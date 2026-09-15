using UnityEngine;

public class BanditAggroTrigger : MonoBehaviour
{
    [Header("Leave empty to auto-find")]
    public BanditBoss banditBossController;

    void Start()
    {
        if (banditBossController == null)
        {
            banditBossController = FindFirstObjectByType<BanditBoss>();
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") && banditBossController != null)
        {
            banditBossController.WakeUpBoss();
        }
    }
}