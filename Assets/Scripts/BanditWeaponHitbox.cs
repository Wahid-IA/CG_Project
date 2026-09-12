using UnityEngine;

[RequireComponent(typeof(Collider))]
[RequireComponent(typeof(Rigidbody))]
public class BanditWeaponHitbox : MonoBehaviour
{
    public BanditBoss bossController;
    private bool hasHitThisSwing = false;

    void Awake()
    {
        // Ensure Rigidbody is kinematic so physics don't throw errors
        Rigidbody rb = GetComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity = false;
    }

    void OnEnable()
    {
        hasHitThisSwing = false;
    }

    void OnTriggerEnter(Collider other)
    {
        if (hasHitThisSwing) return;

        // Check if weapon hit the player
        if (other.CompareTag("Player"))
        {
            hasHitThisSwing = true;
            
            HUDPlayer playerHealth = other.GetComponent<HUDPlayer>() ?? other.GetComponentInParent<HUDPlayer>();
            if (playerHealth != null && bossController != null)
            {
                playerHealth.TakeDamage(bossController.attackDamage, bossController.gameObject);
                Debug.Log("Bandit King successfully hit Player for " + bossController.attackDamage + " damage!");
            }
        }
    }
}