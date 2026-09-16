using UnityEngine;
using UnityEngine.AI;

public class Bandit : MonoBehaviour
{
    [Header("Stats")]
    public float maxHealth = 100f;
    public float currentHealth { get; private set; }
    public bool isDead { get; private set; } = false;

    [Header("References")]
    public Renderer banditRenderer;
    private Animator animator;
    private NavMeshAgent agent;
    private BanditHealthBarUI healthBarUI;

    void Start()
    {
        currentHealth = maxHealth;
        animator = GetComponentInChildren<Animator>();
        agent = GetComponent<NavMeshAgent>();

        if (banditRenderer == null)
        {
            banditRenderer = GetComponentInChildren<Renderer>();
        }

        // Automatically find the health bar UI component on children
        healthBarUI = GetComponentInChildren<BanditHealthBarUI>();
        if (healthBarUI != null)
        {
            healthBarUI.banditScript = this;
        }
    }

    public void TakeDamage(float damageAmount)
    {
        if (isDead) return;

        currentHealth -= damageAmount;
        Debug.Log(gameObject.name + " took damage! Current Health: " + currentHealth);

        if (banditRenderer != null)
        {
            StartCoroutine(FlashColor());
        }

    
        if (currentHealth <= 0)
        {
            Die();
        }
    }

    System.Collections.IEnumerator FlashColor()
    {
        if (banditRenderer != null && banditRenderer.material.HasProperty("_BaseColor"))
        {
            Color orig = banditRenderer.material.GetColor("_BaseColor");
            banditRenderer.material.SetColor("_BaseColor", Color.white);
            yield return new WaitForSeconds(0.12f);
            banditRenderer.material.SetColor("_BaseColor", orig);
        }
    }

    void Die()
    {
        isDead = true;

        if (agent != null) agent.isStopped = true;
        if (animator != null) animator.SetTrigger("Die");

        Collider[] cols = GetComponentsInChildren<Collider>();
        foreach (Collider col in cols) col.enabled = false;

        Destroy(gameObject, 4f);
    }
}