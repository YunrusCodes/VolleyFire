using UnityEngine;

public class ControllableMissile : ControllableObject
{
    [Header("巡弋飛彈設定")]
    [SerializeField] private float initialSpeed = 5f;       // 初始速度
    [SerializeField] private float acceleration = 1f;      // 加速度
    [SerializeField] private float maxSpeed = 30f;         // 最大速度
    [SerializeField] private float rotationSpeed = 180f;    // 轉向速度（度/秒）
    [SerializeField] private float trackingDelay = 0.5f;    // 開始追蹤前的延遲（秒）
    [SerializeField] private float initialUpSpeed = 20f;    // 初始上升速度
    [SerializeField] private string targetTag = "Player";   // 目標標籤
    [SerializeField] private int damage = 2;               // 傷害值
    [SerializeField] private float releaseDamage = 50f;    // 釋放後的傷害值

    private float currentSpeed;                            // 當前速度
    private float currentUpSpeed;                          // 當前上升速度
    private float elapsedTime;                            // 已經過時間
    private Quaternion targetRotation;                     // 目標旋轉
    private bool isTracking = false;                       // 是否在追蹤中
    private Transform target;
    private bool reachedMaxSpeed = false;
    private bool isAscending = true;                       // 是否在上升階段

    protected override void BehaviorOnStart()
    {
        base.BehaviorOnStart();
        // 設置初始狀態
        currentSpeed = initialSpeed;
        currentUpSpeed = initialUpSpeed;
        elapsedTime = 0f;
        isTracking = false;
        isAscending = true;
        
        // 尋找目標
        if (target == null)
        {
            target = GameObject.FindGameObjectWithTag(targetTag)?.transform;
        }
        
        // 初始設定向上方向
        direction = Vector3.up;
        transform.rotation = Quaternion.LookRotation(direction);
        
        // 關閉 Rigidbody 控制（使用 Transform 移動以便更好控制）
        useRigidbody = false;
    }

    protected override void MoveUncontrolled()
    {
        elapsedTime += Time.deltaTime;

        if (isAscending)
        {
            // 上升階段：減速上升
            currentUpSpeed -= acceleration * Time.deltaTime;
            
            // 當上升速度降至初始速度時，結束上升階段
            if (currentUpSpeed <= initialSpeed)
            {
                isAscending = false;
                currentSpeed = initialSpeed;
                // 開始追蹤倒數
                elapsedTime = 0f;
            }
            else
            {
                // 繼續上升
                transform.position += Vector3.up * currentUpSpeed * Time.deltaTime;
                return;
            }
        }

        // 加速階段
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

    protected override void OnControlled()
    {
        base.OnControlled();
        transform.rotation = Quaternion.LookRotation(Vector3.forward);
        if (useRigidbody && rb != null)
        {
            rb.linearVelocity = Vector3.zero;
        }
    }

    protected override void OnReleased()
    { 
        // 如果有目標，將+z軸朝向目標
        if (releaseTarget != null)
        {
            Vector3 targetDirection = (releaseTarget.position - transform.position).normalized;
            transform.rotation = Quaternion.LookRotation(targetDirection);
            direction = transform.forward;
        }
        else
        {
            direction = Vector3.forward;
        }
        
        speed = originalSpeed * releaseSpeedMultiplier;      

        // 如果沒有目標，尋找最近的目標（敵人或蟲洞）
        bool isValidTarget = false;
        string[] validTags = new string[] { "Enemy", "Wormhole" };
        foreach (string tag in validTags)
        {
            if (releaseTarget != null && releaseTarget.gameObject.CompareTag(tag))
            {
                isValidTarget = true;
                break;
            }
        }

        if (!isValidTarget)
        {
            float nearestDistance = float.MaxValue;
            foreach (string tag in validTags)
            {
                GameObject[] targets = GameObject.FindGameObjectsWithTag(tag);
                foreach (GameObject target in targets)
                {
                    // 檢查碰撞體是否啟用
                    Collider targetCollider = target.GetComponent<Collider>();
                    if (targetCollider == null || !targetCollider.enabled) continue;

                    float distance = Vector3.Distance(transform.position, target.transform.position);
                    if (distance < nearestDistance)
                    {
                        nearestDistance = distance;
                        releaseTarget = target.transform;
                    }
                }
            }
        }
        SetLifetime(3);

        
        if (useRigidbody && rb != null)
        {
            rb.linearVelocity = direction * speed;
        }
    }
    protected override void MoveReleased()
    {
        // 如果有目標，將+z軸朝向目標
        if (releaseTarget != null)
        {
            Vector3 targetDirection = (releaseTarget.position - transform.position).normalized;
            transform.rotation = Quaternion.Lerp(
                transform.rotation,
                Quaternion.LookRotation(targetDirection),
                Time.deltaTime * 5
            );
            direction = transform.forward;
        }
        else
        {
            direction = Vector3.forward;
        }
        base.MoveReleased();
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (currentState == ControlState.Uncontrolled)
        {
            // 未控制狀態的碰撞邏輯
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
        else if (currentState == ControlState.Released)
        {
            // 釋放狀態的碰撞邏輯
            if (collision.gameObject.CompareTag("Enemy") || collision.gameObject.CompareTag("Obstacle"))
            {
                var targetHealth = collision.gameObject.GetComponent<BaseHealth>();
                if (targetHealth != null)
                {
                    targetHealth.TakeDamage(releaseDamage);
                }
                DestroyBullet();
            }
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
