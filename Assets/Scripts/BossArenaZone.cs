using UnityEngine;

public class BossArenaZone : MonoBehaviour
{
    [Header("Reference to the Boss in the Arena")]
    public BossController bossController;

    private bool hasTriggered = false;

    void OnTriggerEnter(Collider other)
    {
        // Check if it's the player entering the arena for the first time
        if (!hasTriggered && other.CompareTag("Player"))
        {
            hasTriggered = true;
            Debug.Log("Entered Boss Arena! The battle begins.");

            // Activate the boss and let it know the fight has started
            if (bossController != null)
            {
                bossController.WakeUpBoss();
            }

            // Optional: You can spawn a visual "Fog Wall" barrier behind the player here
        }
    }
}