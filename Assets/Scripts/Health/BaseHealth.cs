using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public abstract class BaseHealth : MonoBehaviour, IHealth
{
    [SerializeField] protected float maxHealth = 100;
    [SerializeField] protected float currentHealth;
    protected bool isDead = false;
    
    [Header("傷害文字設定")]
    [SerializeField] protected Vector3 damageTextOffset = new Vector3(0, 1, 0);  // 傷害文字位置偏移

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
        
        // 顯示傷害文字
        if (StageManager.Instance != null)
        {
            StageManager.Instance.ShowDamageText(transform.position, damage, damageTextOffset, null);
        }

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