using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

public class PlayerHealth : BaseHealth
{
    [Header("血條設置")]
    public Slider healthSlider;        // 血條滑桿
    public Image fillImage;            // 血條填充圖像
    public Color fullHealthColor = Color.green;     // 滿血顏色
    public Color lowHealthColor = Color.red;        // 低血顏色
    public float lowHealthPercent = 0.3f;          // 低血量百分比閾值

    // 死亡事件
    public UnityEvent onPlayerDeath = new UnityEvent();

    private void Start()
    {
        // 初始化血條
        if (healthSlider != null)
        {
            healthSlider.maxValue = maxHealth;
            healthSlider.value = currentHealth;
            UpdateHealthBar();
        }
    }

    public override void TakeDamage(float damage)
    {
        // 不直接呼叫 base.TakeDamage，而是複製其邏輯並修改顏色
        Debug.Log("TakeDamage: " + damage);
        if (isDead) return;
        currentHealth -= damage;
        currentHealth = Mathf.Max(0, currentHealth);
        
        // 顯示紫色傷害文字
        if (StageManager.Instance != null)
        {
            StageManager.Instance.ShowDamageText(transform.position, damage, damageTextOffset, new Color(0.8f, 0f, 1f, 1f));
        }

        if (currentHealth <= 0)
        {
            Die();
        }

        UpdateHealthBar();
    }

    private void UpdateHealthBar()
    {
        if (healthSlider == null || fillImage == null) return;

        healthSlider.value = currentHealth;
        
        // 計算血量百分比
        float healthPercent = currentHealth / maxHealth;
        
        // 根據血量百分比更新顏色
        fillImage.color = Color.Lerp(lowHealthColor, fullHealthColor, healthPercent / lowHealthPercent);
    }

    protected override void Die()
    {
        if (isDead) return;
        isDead = true;
        PlayerController.EnableFire(false);
        onPlayerDeath.Invoke();
    }
} 