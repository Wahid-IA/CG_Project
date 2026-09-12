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
            other.SendMessage("TakeDamage", bossController.attackDamage, SendMessageOptions.DontRequireReceiver);
            Debug.Log("Bandit King hit Player for " + bossController.attackDamage + " damage!");
        }
    }
}