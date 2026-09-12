using UnityEngine;

public class BanditWeaponHitbox : MonoBehaviour
{
    public BanditBoss bossController;
    private bool hasHitThisSwing = false;

    void OnEnable()
    {
        hasHitThisSwing = false;
    }

    void OnTriggerEnter(Collider other)
    {
        if (hasHitThisSwing) return;

        if (other.CompareTag("Player"))
        {
            hasHitThisSwing = true;
            
            // Deliver damage directly to the HUDPlayer script
            HUDPlayer playerHealth = other.GetComponent<HUDPlayer>();
            if (playerHealth != null)
            {
                playerHealth.TakeDamage(bossController.attackDamage, bossController.gameObject);
            }
            
            Debug.Log("Bandit King hit Player for " + bossController.attackDamage + " damage!");
        }
    }
}