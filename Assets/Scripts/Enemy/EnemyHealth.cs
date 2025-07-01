using UnityEngine;
using UnityEngine.Events;

public class EnemyHealth : MonoBehaviour
{
    [Header("生命值設定")]
    [SerializeField] private int maxHealth = 50;
    [SerializeField] private int currentHealth;
    [SerializeField] private bool isInvulnerable = false;
    [SerializeField] private float invulnerabilityTime = 0.5f;
    
    [Header("視覺效果")]
    [SerializeField] private GameObject damageEffect;
    [SerializeField] private GameObject deathEffect;
    [SerializeField] private Renderer objectRenderer;
    [SerializeField] private Color damageColor = Color.red;
    [SerializeField] private float flashDuration = 0.1f;
    [SerializeField] private Material damageMaterial;
    
    [Header("音效")]
    [SerializeField] private AudioClip damageSound;
    [SerializeField] private AudioClip deathSound;
    [SerializeField] private AudioSource audioSource;
    
    [Header("分數")]
    [SerializeField] private int scoreValue = 100;
    
    [Header("事件")]
    public UnityEvent<int> OnHealthChanged;
    public UnityEvent OnEnemyDeath;
    public UnityEvent OnEnemyDamaged;
    
    private float invulnerabilityTimer;
    private Material originalMaterial;
    private bool isDead = false;
    
    private void Awake()
    {
        currentHealth = maxHealth;
        
        if (objectRenderer == null)
            objectRenderer = GetComponent<Renderer>();
        
        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();
        
        if (objectRenderer != null)
            originalMaterial = objectRenderer.material;
    }
    
    private void Start()
    {
        OnHealthChanged?.Invoke(currentHealth);
    }
    
    private void Update()
    {
        if (isInvulnerable)
        {
            invulnerabilityTimer -= Time.deltaTime;
            if (invulnerabilityTimer <= 0f)
            {
                SetInvulnerable(false);
            }
        }
    }
    
    public void TakeDamage(int damage)
    {
        if (isDead || isInvulnerable)
            return;
        
        currentHealth -= damage;
        currentHealth = Mathf.Max(0, currentHealth);
        
        OnHealthChanged?.Invoke(currentHealth);
        OnEnemyDamaged?.Invoke();
        
        // 播放受傷效果
        PlayDamageEffect();
        PlayDamageSound();
        
        // 設置無敵時間
        if (isInvulnerable)
        {
            SetInvulnerable(true);
        }
        
        // 檢查是否死亡
        if (currentHealth <= 0)
        {
            Die();
        }
    }
    
    private void Die()
    {
        if (isDead)
            return;
        
        isDead = true;
        
        // 播放死亡效果
        PlayDeathEffect();
        PlayDeathSound();
        
        // 觸發死亡事件
        OnEnemyDeath?.Invoke();
        
        // 增加分數（如果有分數系統）
        var scoreManager = FindFirstObjectByType<ScoreManager>();
        if (scoreManager != null)
        {
            scoreManager.AddScore(scoreValue);
        }
        
        // 停用所有組件
        var enemyController = GetComponent<EnemyController>();
        if (enemyController != null)
        {
            enemyController.enabled = false;
        }
        
        var weaponSystem = GetComponent<WeaponSystem>();
        if (weaponSystem != null)
        {
            weaponSystem.SetCanFire(false);
        }
        
        // 延遲銷毀物件
        Destroy(gameObject, 1f);
    }
    
    private void PlayDamageEffect()
    {
        if (damageEffect != null)
        {
            GameObject effect = Instantiate(damageEffect, transform.position, transform.rotation);
            Destroy(effect, 3f);
        }
        
        if (objectRenderer != null)
        {
            StartCoroutine(FlashRenderer());
        }
    }
    
    private void PlayDeathEffect()
    {
        if (deathEffect != null)
        {
            GameObject effect = Instantiate(deathEffect, transform.position, transform.rotation);
            Destroy(effect, 5f);
        }
    }
    
    private void PlayDamageSound()
    {
        if (audioSource != null && damageSound != null)
        {
            audioSource.PlayOneShot(damageSound);
        }
    }
    
    private void PlayDeathSound()
    {
        if (audioSource != null && deathSound != null)
        {
            audioSource.PlayOneShot(deathSound);
        }
    }
    
    private System.Collections.IEnumerator FlashRenderer()
    {
        if (objectRenderer == null) yield break;
        
        // 使用傷害材質或顏色
        if (damageMaterial != null)
        {
            objectRenderer.material = damageMaterial;
        }
        else
        {
            objectRenderer.material.color = damageColor;
        }
        
        yield return new WaitForSeconds(flashDuration);
        
        // 恢復原始材質
        objectRenderer.material = originalMaterial;
    }
    
    private void SetInvulnerable(bool invulnerable)
    {
        isInvulnerable = invulnerable;
        
        if (invulnerable)
        {
            invulnerabilityTimer = invulnerabilityTime;
            StartCoroutine(InvulnerabilityFlash());
        }
        else
        {
            if (objectRenderer != null)
                objectRenderer.material = originalMaterial;
        }
    }
    
    private System.Collections.IEnumerator InvulnerabilityFlash()
    {
        while (isInvulnerable)
        {
            if (objectRenderer != null)
            {
                // 快速閃爍效果
                objectRenderer.enabled = false;
                yield return new WaitForSeconds(0.05f);
                objectRenderer.enabled = true;
                yield return new WaitForSeconds(0.05f);
            }
            else
            {
                yield return new WaitForSeconds(0.1f);
            }
        }
        
        // 確保最後是啟用狀態
        if (objectRenderer != null)
            objectRenderer.enabled = true;
    }
    
    // 公開方法供外部調用
    public int GetCurrentHealth()
    {
        return currentHealth;
    }
    
    public int GetMaxHealth()
    {
        return maxHealth;
    }
    
    public float GetHealthPercentage()
    {
        return (float)currentHealth / maxHealth;
    }
    
    public bool IsDead()
    {
        return isDead;
    }
    
    public bool IsInvulnerable()
    {
        return isInvulnerable;
    }
    
    public int GetScoreValue()
    {
        return scoreValue;
    }
    
    public void SetMaxHealth(int newMaxHealth)
    {
        maxHealth = newMaxHealth;
        if (currentHealth > maxHealth)
        {
            currentHealth = maxHealth;
        }
        OnHealthChanged?.Invoke(currentHealth);
    }
    
    public void SetScoreValue(int newScoreValue)
    {
        scoreValue = newScoreValue;
    }
    
    public void SetInvulnerabilityTime(float time)
    {
        invulnerabilityTime = time;
    }
    
    // 設置傷害材質
    public void SetDamageMaterial(Material material)
    {
        damageMaterial = material;
    }
    
    // 設置傷害顏色
    public void SetDamageColor(Color color)
    {
        damageColor = color;
    }
} 