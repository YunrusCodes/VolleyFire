using UnityEngine;
using UnityEngine.Events;

public class PlayerHealth : MonoBehaviour
{
    [Header("生命值設定")]
    [SerializeField] private int maxHealth = 100;
    [SerializeField] private int currentHealth;
    [SerializeField] private bool isInvulnerable = false;
    [SerializeField] private float invulnerabilityTime = 2f;
    
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
    [SerializeField] private AudioClip healSound;
    [SerializeField] private AudioSource audioSource;
    
    [Header("事件")]
    public UnityEvent<int> OnHealthChanged;
    public UnityEvent OnPlayerDeath;
    public UnityEvent OnPlayerDamaged;
    public UnityEvent OnPlayerHealed;
    
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
        OnPlayerDamaged?.Invoke();
        
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
    
    public void Heal(int healAmount)
    {
        if (isDead)
            return;
        
        currentHealth += healAmount;
        currentHealth = Mathf.Min(currentHealth, maxHealth);
        
        OnHealthChanged?.Invoke(currentHealth);
        OnPlayerHealed?.Invoke();
        
        PlayHealSound();
    }
    
    public void RestoreFullHealth()
    {
        if (isDead)
            return;
        
        currentHealth = maxHealth;
        OnHealthChanged?.Invoke(currentHealth);
        OnPlayerHealed?.Invoke();
        
        PlayHealSound();
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
        OnPlayerDeath?.Invoke();
        
        // 停用玩家控制
        var playerController = GetComponent<PlayerController>();
        if (playerController != null)
        {
            playerController.enabled = false;
        }
        
        var weaponSystem = GetComponent<WeaponSystem>();
        if (weaponSystem != null)
        {
            weaponSystem.SetCanFire(false);
        }
        
        // 可以選擇立即銷毀或延遲銷毀
        // Destroy(gameObject, 2f);
        
        // 或者只是停用物件
        gameObject.SetActive(false);
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
    
    private void PlayHealSound()
    {
        if (audioSource != null && healSound != null)
        {
            audioSource.PlayOneShot(healSound);
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
                // 閃爍效果
                objectRenderer.enabled = false;
                yield return new WaitForSeconds(0.1f);
                objectRenderer.enabled = true;
                yield return new WaitForSeconds(0.1f);
            }
            else
            {
                yield return new WaitForSeconds(0.2f);
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
    
    public void SetMaxHealth(int newMaxHealth)
    {
        maxHealth = newMaxHealth;
        if (currentHealth > maxHealth)
        {
            currentHealth = maxHealth;
        }
        OnHealthChanged?.Invoke(currentHealth);
    }
    
    public void SetInvulnerabilityTime(float time)
    {
        invulnerabilityTime = time;
    }
    
    // 復活方法
    public void Resurrect()
    {
        if (!isDead)
            return;
        
        isDead = false;
        currentHealth = maxHealth;
        
        // 重新啟用組件
        var playerController = GetComponent<PlayerController>();
        if (playerController != null)
        {
            playerController.enabled = true;
        }
        
        var weaponSystem = GetComponent<WeaponSystem>();
        if (weaponSystem != null)
        {
            weaponSystem.SetCanFire(true);
        }
        
        // 確保渲染器是啟用的
        if (objectRenderer != null)
        {
            objectRenderer.enabled = true;
            objectRenderer.material = originalMaterial;
        }
        
        OnHealthChanged?.Invoke(currentHealth);
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