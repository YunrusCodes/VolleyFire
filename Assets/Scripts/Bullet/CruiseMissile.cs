using UnityEngine;

public class CruiseMissile : BulletBehavior
{
    [Header("巡弋飛彈設定")]
    [SerializeField] private float initialSpeed = 5f;       // 初始速度
    [SerializeField] private float acceleration = 1f;      // 加速度
    [SerializeField] private float maxSpeed = 30f;         // 最大速度
    [SerializeField] private float rotationSpeed = 180f;    // 轉向速度（度/秒）
    [SerializeField] private float trackingDelay = 0.5f;    // 開始追蹤前的延遲（秒）
    [SerializeField] private string targetTag = "Player";   // 目標標籤
    [SerializeField] private int damage = 2;               // 傷害值

    public float currentSpeed;                            // 當前速度
    private float elapsedTime;                            // 已經過時間
    private Quaternion targetRotation;                     // 目標旋轉

    public bool isTracking = false;                       // 是否在追蹤中
    public Transform target;

    protected override void BehaviorOnStart()
    {
        // 設置初始狀態
        currentSpeed = initialSpeed;
        elapsedTime = 0f;
        isTracking = false;
        
        // 尋找目標
        if (target == null)
        {
            target = GameObject.FindGameObjectWithTag(targetTag)?.transform;
        }
        
        // 設置初始方向和旋轉
        if (target != null)
        {
            direction = (target.position - transform.position).normalized;
        }
        else
        {
            direction = transform.forward;
        }
        
        // 關閉 Rigidbody 控制（使用 Transform 移動以便更好控制）
        useRigidbody = false;
    }

        // 更新速度（加速）
    public bool reachedMaxSpeed = false;
    protected override void Move()
    {
        elapsedTime += Time.deltaTime;

        if (currentSpeed < maxSpeed)
        {
            currentSpeed += acceleration * Time.deltaTime;
            if (currentSpeed >= maxSpeed)
            {
                currentSpeed = maxSpeed;
                reachedMaxSpeed = true;
            }
        }

        // 延遲後開始追蹤
        if (!isTracking && elapsedTime >= trackingDelay)
        {
            isTracking = true;
        }

        // 如果在追蹤狀態且有目標，且未達到最大速度
        if (isTracking && target != null && !reachedMaxSpeed)
        {
            // 計算目標方向
            Vector3 targetDirection = (target.position - transform.position).normalized;
            targetRotation = Quaternion.LookRotation(targetDirection);
            
            // 根據速度調整轉向速度（速度越快，轉向越慢）
            float speedFactor = 1f - (currentSpeed / maxSpeed);
            float currentRotationSpeed = rotationSpeed * (0.2f + (0.8f * speedFactor));

            // 平滑旋轉
            transform.rotation = Quaternion.RotateTowards(
                transform.rotation,
                targetRotation,
                currentRotationSpeed * Time.deltaTime
            );
            
            // 更新移動方向為當前朝向
            direction = transform.forward;
        }

        // 移動
        transform.position += direction * currentSpeed * Time.deltaTime;
    }

    private void OnCollisionEnter(Collision collision)
    {
        // 檢查是否擊中目標
        if (collision.gameObject.CompareTag(targetTag))
        {
            var targetHealth = collision.gameObject.GetComponent<BaseHealth>();
            if (targetHealth != null)
            {
                targetHealth.TakeDamage(damage);
            }
            DestroyBullet();
        }
    }

    private void OnValidate()
    {
        // 確保最大速度大於初始速度
        if (maxSpeed < initialSpeed)
        {
            maxSpeed = initialSpeed;
            Debug.LogWarning("最大速度不能小於初始速度，已自動調整");
        }

        // 確保加速度為正值
        if (acceleration < 0)
        {
            acceleration = 0;
            Debug.LogWarning("加速度不能為負值，已自動調整為0");
        }

        // 確保追蹤延遲不為負
        if (trackingDelay < 0)
        {
            trackingDelay = 0;
            Debug.LogWarning("追蹤延遲不能為負值，已自動調整為0");
        }
    }
} 