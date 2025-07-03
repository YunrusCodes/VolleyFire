using UnityEngine;

public abstract class BaseHealth : MonoBehaviour, IHealth
{
    [SerializeField] protected int maxHealth = 100;
    [SerializeField] protected int currentHealth;
    protected bool isDead = false;

    protected virtual void Awake()
    {
        currentHealth = maxHealth;
    }

    public virtual void TakeDamage(int damage)
    {
        Debug.Log("TakeDamage: " + damage);
        if (isDead) return;
        currentHealth -= damage;
        currentHealth = Mathf.Max(0, currentHealth);
        if (currentHealth <= 0)
        {
            Die();
        }
    }

    protected virtual void Die()
    {
        if (isDead) return;
        isDead = true;
    }

    public int GetCurrentHealth() => currentHealth;
    public int GetMaxHealth() => maxHealth;
    public bool IsDead() => isDead;
    public void SetMaxHealth(int newMaxHealth)
    {
        maxHealth = newMaxHealth;
        if (currentHealth > maxHealth)
            currentHealth = maxHealth;
    }
} 