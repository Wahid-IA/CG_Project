using UnityEngine;

public class GoblinBossAggroTrigger : MonoBehaviour
{
    public GoblinBoss goblinBoss;
    public GoblinBossHealthBarUI bossHealthBarUI; // Type matches your Goblin UI script

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            if (goblinBoss != null)
            {
                goblinBoss.WakeUpBoss(); // This awakens the boss, making its health bar appear automatically
            }
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            // Optional: Handle exit logic here if needed later
        }
    }
}