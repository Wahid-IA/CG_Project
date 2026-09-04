using UnityEngine;

public class BossArenaZone : MonoBehaviour
{
    [Header("Reference to the Boss in the Arena")]
    public BossController bossController;

    private bool hasTriggered = false;

    void OnTriggerEnter(Collider other)
    {
        Debug.Log("Something entered the trigger: " + other.name + " with tag: " + other.tag);

        // Check if it's the player entering the arena for the first time
        if (!hasTriggered && other.CompareTag("Player"))
        {
            hasTriggered = true;
            Debug.Log("SUCCESS: Player crossed the threshold! Waking up the boss.");

            if (bossController != null)
            {
                bossController.WakeUpBoss();
            }
            else
            {
                Debug.LogError("ERROR: Boss Controller reference is missing on the BossArenaTrigger!");
            }
        }
    }
}