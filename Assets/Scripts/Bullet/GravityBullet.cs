using UnityEngine;

/// <summary>
/// 下墜子彈 - 繼承自BulletBehavior，受重力影響
/// </summary>
public class GravityBullet : BulletBehavior
{
    [Header("重力設定")]
    [SerializeField] private float gravity = 9.8f;         // 重力加速度
    [SerializeField] private Vector3 gravityDirection = Vector3.down; // 重力方向
    
    private Vector3 currentVelocity;  // 當前速度
    
    protected override void Start()
    {
        base.Start();
        
        // 初始化當前速度
        currentVelocity = direction * speed;
    }
    
    protected override void Move()
    {
        // 應用重力
        currentVelocity += gravityDirection * gravity * Time.deltaTime;
        
        // 如果不使用Rigidbody，使用Transform移動
        if (!useRigidbody)
        {
            transform.position += currentVelocity * Time.deltaTime;
        }
        else if (rb != null)
        {
            // 如果使用Rigidbody，直接設置速度
            rb.linearVelocity = currentVelocity;
        }
    }
    
    public override void Initialize(FirePoint firePoint)
    {
        base.Initialize(firePoint);
        
        // 重新設置當前速度
        currentVelocity = direction * speed;
    }
    
    /// <summary>
    /// 設置重力加速度
    /// </summary>
    /// <param name="newGravity">新的重力加速度</param>
    public void SetGravity(float newGravity)
    {
        gravity = newGravity;
    }
    
    /// <summary>
    /// 設置重力方向
    /// </summary>
    /// <param name="newGravityDirection">新的重力方向</param>
    public void SetGravityDirection(Vector3 newGravityDirection)
    {
        gravityDirection = newGravityDirection.normalized;
    }
} 