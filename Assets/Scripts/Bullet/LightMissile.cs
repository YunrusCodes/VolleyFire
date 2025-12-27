using UnityEngine;

/// <summary>
/// 光導彈 - 會朝向初始記錄的玩家位置移動
/// </summary>
public class LightMissile : BulletBehavior
{
    [Header("導彈設定")]
    [SerializeField] private string targetTag = "Player";    // 目標標籤
    [SerializeField] private float damage = 1f;              // 傷害值
    [SerializeField] private float initialRotateSpeed = 1f;   // 初始旋轉速度
    [SerializeField] private float maxRotateSpeed = 5f;      // 最大旋轉速度
    [SerializeField] private float rotateAcceleration = 2f;  // 旋轉加速度
    [SerializeField] private float correctionThreshold = 0.7f; // 修正時域（0-1之間，表示射程的比例）

    private Vector3 targetPosition;                          // 目標位置
    private Vector3 startPosition;                           // 起始位置
    private bool canCorrectDirection = true;                 // 是否可以修正方向
    private float range;                                     // 射程（由初始位置到目標的距離決定）
    private float currentRotateSpeed;                        // 當前旋轉速度

    protected override void BehaviorOnStart()
    {
        // 記錄起始位置
        startPosition = transform.position;
        
        // 獲取玩家位置
        GameObject player = GameObject.FindGameObjectWithTag(targetTag);
        if (player != null)
        {
            targetPosition = player.transform.position;
            // 根據目標距離設置射程
            range = Vector3.Distance(startPosition, targetPosition);
        }
        else
        {
            // 如果找不到玩家，使用前方100單位作為目標
            targetPosition = transform.position + transform.forward * 100f;
            range = 100f;
        }

        // 初始方向設為朝向目標
        direction = (targetPosition - transform.position).normalized;
        
        // 設置初始旋轉速度
        currentRotateSpeed = initialRotateSpeed;
    }

    protected override void Move()
    {
        // 檢查是否可以修正方向
        if (canCorrectDirection)
        {
            // 計算已經飛行的距離佔射程的比例
            float distanceFromStart = Vector3.Distance(transform.position, startPosition);
            float distanceRatio = distanceFromStart / range;

            // 如果超過修正時域，停止修正
            if (distanceRatio > correctionThreshold)
            {
                canCorrectDirection = false;
            }
            else
            {
                // 計算朝向目標的方向
                Vector3 directionToTarget = (targetPosition - transform.position).normalized;
                
                // 增加旋轉速度（有上限）
                currentRotateSpeed = Mathf.Min(currentRotateSpeed + rotateAcceleration * Time.deltaTime, maxRotateSpeed);
                
                // 使用當前旋轉速度進行 Lerp 平滑旋轉
                direction = Vector3.Lerp(direction, directionToTarget, currentRotateSpeed * Time.deltaTime);
            }
        }
        
        // 更新物體的旋轉
        transform.rotation = Quaternion.LookRotation(direction);

        // 根據是否使用 Rigidbody 來移動
        if (!useRigidbody)
        {
            transform.position += direction * speed * Time.deltaTime;
        }
        else if (rb != null)
        {
            // 如果使用 Rigidbody，直接設置速度
            rb.linearVelocity = direction * speed;
        }
    }

    protected void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag(targetTag) || collision.gameObject.CompareTag("Obstacle"))
        {
            var targetHealth = collision.gameObject.GetComponent<BaseHealth>();
            if (targetHealth != null)
            {
                targetHealth.TakeDamage(damage);
            }
            DestroyBullet();
        }
    }
}
