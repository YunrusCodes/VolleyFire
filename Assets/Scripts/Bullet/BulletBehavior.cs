using UnityEngine;

/// <summary>
/// 子彈行為基礎類別 - 所有子彈行為都應該繼承此類別
/// </summary>
public abstract class BulletBehavior : MonoBehaviour
{
    [Header("子彈基礎設定")]
    [SerializeField] protected float speed = 30f;           // 子彈速度
    [SerializeField] protected float lifetime = 5f;         // 子彈存活時間
    [SerializeField] protected bool useRigidbody = true;    // 是否使用Rigidbody移動
    protected FirePoint firePoint;

    // 保護變數，子類別可以訪問
    protected Rigidbody rb;           // Rigidbody組件
    protected Vector3 direction;      // 移動方向
    protected float spawnTime;        // 生成時間
    
    [SerializeField] protected GameObject explosionPrefab;
    private GameObject myExplosionEffect;
    
    protected virtual void Awake()
    {
        // 獲取Rigidbody組件
        rb = GetComponent<Rigidbody>();
        
        // 記錄生成時間
        spawnTime = Time.time;

        // 呼叫子類別的初始化行為
        BehaviorOnStart();
    }
    
    protected virtual void Start()
    {
        // 不在這裡設定 direction
        if (useRigidbody && rb != null)
        {
            rb.linearVelocity = direction * speed;
        }
    }
    
    protected virtual void Update()
    {
        // 檢查存活時間，-1 視為無限壽命
        if (lifetime >= 0f && Time.time - spawnTime > lifetime)
        {
            DestroyBullet();
            return;
        }
        
        // 調用子類別的移動邏輯
        Move();
    }
    
    /// <summary>
    /// 移動邏輯 - 子類別必須實作此方法
    /// </summary>
    protected abstract void Move();

    /// <summary>
    /// 初始化行為 - 子類別必須實作此方法
    /// </summary>
    protected abstract void BehaviorOnStart();
    
    /// <summary>
    /// 初始化子彈
    /// </summary>
    public virtual void Initialize(FirePoint firePoint)
    {
        // 重置生成時間
        spawnTime = Time.time;
        // 不在這裡設定 direction
        this.firePoint = firePoint;
        speed = firePoint.projectileSpeed;
        // 如果使用Rigidbody，設置初始速度
        if (useRigidbody && rb != null)
        {
            rb.linearVelocity = direction * speed;
        }
    }
    
    /// <summary>
    /// 設置子彈速度
    /// </summary>
    /// <param name="newSpeed">新的速度</param>
    public virtual void SetSpeed(float newSpeed)
    {
        speed = newSpeed;
        
        // 更新Rigidbody速度
        if (useRigidbody && rb != null)
        {
            rb.linearVelocity = direction * speed;
        }
    }
    
    /// <summary>
    /// 設置移動方向
    /// </summary>
    /// <param name="newDirection">新的方向</param>
    public virtual void SetDirection(Vector3 newDirection)
    {
        direction = newDirection.normalized;
        
        // 更新Rigidbody速度
        if (useRigidbody && rb != null)
        {
            rb.linearVelocity = direction * speed;
        }
    }
    
    /// <summary>
    /// 設置存活時間
    /// </summary>
    /// <param name="newLifetime">新的存活時間</param>
    public virtual void SetLifetime(float newLifetime)
    {
        lifetime = newLifetime;
    }
    
    /// <summary>
    /// 銷毀子彈
    /// </summary>
    public virtual void DestroyBullet()
    {
        // 只在第一次產生特效
        if (explosionPrefab != null)
        {
            if (myExplosionEffect == null)
            {
                myExplosionEffect = Instantiate(explosionPrefab, transform.position, Quaternion.identity);
            }
            else
            {
                myExplosionEffect.transform.position = transform.position;
                myExplosionEffect.transform.rotation = Quaternion.identity;
                myExplosionEffect.SetActive(true);
            }
        }
        // 直接銷毀自己
        Destroy(gameObject);
    }
} 