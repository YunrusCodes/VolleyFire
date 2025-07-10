using UnityEngine;

public abstract class BaseHealth : MonoBehaviour, IHealth
{
    [SerializeField] protected float maxHealth = 100;
    [SerializeField] protected float currentHealth;
    protected bool isDead = false;

    protected virtual void Awake()
    {
        currentHealth = maxHealth;
    }

    public virtual void TakeDamage(float damage)
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
    public bool IsHurt()
    {
        return currentHealth < maxHealth;
    }
    protected virtual void Die()
    {
        if (isDead) return;
        isDead = true;
    }

    public float GetCurrentHealth() => currentHealth;
    public float GetMaxHealth() => maxHealth;
    public bool IsDead() => isDead;
    
    /// <summary>
    /// 獲取血量百分比 (0.0f - 1.0f)
    /// </summary>
    public float GetHealthPercentage()
    {
        if (maxHealth <= 0) return 0f;
        return (float)currentHealth / maxHealth;
    }
    
    public void SetMaxHealth(float newMaxHealth)
    {
        maxHealth = newMaxHealth;
        if (currentHealth > maxHealth)
            currentHealth = maxHealth;
    }
    public void ResetHealth()
    {
        currentHealth = maxHealth;
    }
} 