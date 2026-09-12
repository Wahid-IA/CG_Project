using UnityEngine;

public class BanditAggroTrigger : MonoBehaviour
{
    [Header("Leave empty to auto-find")]
    public BanditBoss banditBossController;

    void Start()
    {
        // Automatically finds the BanditBoss script in your scene
        if (banditBossController == null)
        {
            banditBossController = FindFirstObjectByType<BanditBoss>();
            
            if (banditBossController == null)
            {
                Debug.LogError("BanditAggroTrigger could not find a BanditBoss in the scene!");
            }
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