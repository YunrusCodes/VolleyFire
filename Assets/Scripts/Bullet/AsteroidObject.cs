using UnityEngine;

public class AsteroidObject : ControllableObject
{
    private Transform spawnPoint;                            // 生成點
    [SerializeField] private float releaseDamage = 50f;     // 釋放後的傷害值
    [SerializeField] private string targetTag = "Enemy";     // 目標標籤
    [SerializeField] private float destroySelfZ = 0.3f;    // 自我銷毀的Z軸距離

    public void Initialize(Transform spawnPoint)
    {
        this.spawnPoint = spawnPoint;
    }

    protected override void BehaviorOnStart()
    {
        originalSpeed = speed;
        direction = Vector3.back; // -z方向
        // 初始化時設置無限生命週期
        SetLifetime(-1f);
    }

    protected override void Move()
    {
        if (currentState == ControlState.Controlled)
        {
            return; // 控制中不移動
        }

        if (!useRigidbody)
        {
            transform.position += direction * speed * Time.deltaTime;
        }
    }

    protected override void OnUncontrolled()
    {
        direction = Vector3.back; // -z方向
        speed = originalSpeed;
        
        if (useRigidbody && rb != null)
        {
            rb.linearVelocity = direction * speed;
        }
        
        // 檢查是否需要自我銷毀
        float dist = transform.position.z - spawnPoint.position.z;
        if (dist <= destroySelfZ)
        {
            Destroy(gameObject);
        }
    }

    protected override void OnControlled()
    {
        if (useRigidbody && rb != null)
        {
            rb.linearVelocity = Vector3.zero;
        }

    }

    protected override void OnReleased()
    {
        direction = Vector3.forward;
        
        speed = originalSpeed * releaseSpeedMultiplier;
        
        if (useRigidbody && rb != null)
        {
            rb.linearVelocity = direction * speed;
        }
        // 釋放時重新開始計時（設置 5 秒存活時間）
        SetLifetime(5f);
        // 重置生成時間為當前時間
        spawnTime = Time.time;
    }

    protected void OnCollisionEnter(Collision collision)
    {
        // 只在釋放狀態下才檢查碰撞
        if (currentState != ControlState.Released) return;

        // 檢查是否撞到目標或障礙物
        if (collision.gameObject.CompareTag(targetTag) || collision.gameObject.CompareTag("Obstacle"))
        {
            // 嘗試對目標造成傷害
            var targetHealth = collision.gameObject.GetComponent<BaseHealth>();
            if (targetHealth != null)
            {
                targetHealth.TakeDamage(releaseDamage);
            }
            Debug.Log("DestroyBullet");
            // 銷毀自己
            DestroyBullet();
        }
    }
} 