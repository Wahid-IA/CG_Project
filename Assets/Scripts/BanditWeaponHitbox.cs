using UnityEngine;

public class BanditWeaponHitbox : MonoBehaviour
{
    public BanditBoss bossController;
    private bool hasHitThisSwing = false;

    void OnEnable()
    {
        hasHitThisSwing = false; // Reset for each new swing
    }

    void OnTriggerEnter(Collider other)
    {
        if (hasHitThisSwing) return;

        if (other.CompareTag("Player"))
        {
            hasHitThisSwing = true;

            // Sends a message to any health script on the player
            other.SendMessage("TakeDamage", bossController.attackDamage, SendMessageOptions.DontRequireReceiver);
            Debug.Log("Bandit King hit Player for " + bossController.attackDamage + " damage!");
        }
    }
}