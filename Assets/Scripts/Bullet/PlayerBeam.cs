using UnityEngine;

public class PlayerBeam : BulletBehavior
{
    [Header("雷射設定")]
    [SerializeField] private GameObject laserObject;       // 雷射物件
    [SerializeField] private float expandSpeed = 150f;     // 雷射擴展速度（玩家版本較快）
    [SerializeField] private float shrinkSpeed = 8f;      // 雷射收縮速度（玩家版本較快）
    [SerializeField] private float maxLength = 50f;       // 最大長度（玩家版本較長）
    [SerializeField] private string targetTag = "Enemy";   // 目標標籤（改為敵人）
    [SerializeField] private float damage = 10;            // 傷害值（玩家版本較高）

    private bool isActive = false;                        // 是否在作用中
    private bool isShrinking = false;                     // 是否在收縮中
    private Transform spawnPoint;                         // 發射點
    private float damageInterval = 0.05f; // 傷害間隔（玩家版本較短）
    private float damageTimer = 0f;      // 傷害計時器

    protected override void BehaviorOnStart()
    {
        isActive = true; // 直接進入作用階段
        isShrinking = false;

        // 設置初始狀態
        if (laserObject)
        {
            laserObject.SetActive(true);
            // 設置初始尺寸
            laserObject.transform.localScale = new Vector3(1f, 1f, 0f);
        }

        // 關閉 Rigidbody 控制
        useRigidbody = false;
    }

    public void SetSpawnPoint(Transform point)
    {
        spawnPoint = point;
    }

    protected override void Move()
    {
        // 如果有發射點，更新位置和旋轉
        if(!spawnPoint.gameObject.activeSelf) Destroy(gameObject);
        if (spawnPoint != null)
        {
            transform.position = spawnPoint.position;
            transform.rotation = spawnPoint.rotation;
        }

        // 直接進入作用階段
        if (isActive)
        {
            HandleActive();
        }
        else if (isShrinking)
        {
            HandleShrinking();
        }
    }

    private void HandleActive()
    {
        if (!isShrinking)
        {
            // 擴展雷射
            if (laserObject)
            {
                Vector3 scale = laserObject.transform.localScale;
                scale.z = Mathf.Min(scale.z + expandSpeed * Time.deltaTime, maxLength);
                laserObject.transform.localScale = scale;
            }

            // 這裡不再根據 activeTime 結束作用階段，請根據遊戲需求自行調整
            // 可考慮由外部控制何時進入收縮階段
        }
    }

    public void HandleShrinking()
    {
        if (laserObject)
        {
            // 收縮雷射
            Vector3 scale = laserObject.transform.localScale;
            scale.z = Mathf.Max(0f, scale.z - shrinkSpeed * Time.deltaTime);
            laserObject.transform.localScale = scale;

            // 檢查是否完全收縮
            if (scale.z <= 0f)
            {
                DestroyBullet();
            }
        }
    }

    private void OnCollisionStay(Collision collision)
    {
        if (!isActive) return; // 只在作用階段造成傷害
        if (!collision.collider.CompareTag(targetTag)) return;
        damageTimer += Time.deltaTime;
        if (damageTimer >= damageInterval)
        {
            damageTimer = 0f;
            var targetHealth = collision.collider.GetComponent<BaseHealth>();
            if (targetHealth != null)
            {
                // 讓 damage 代表每秒傷害
                targetHealth.TakeDamage(damage * damageInterval);

                // 產生隨機小位移
                Vector3 randomOffset = new Vector3(
                    Random.Range(-0.2f, 0.2f),
                    Random.Range(-0.2f, 0.2f),
                    Random.Range(-0.2f, 0.2f)
                );
                // 產生爆炸特效
                if (explosionPrefab != null && collision.contactCount > 0)
                {
                    Instantiate(
                        explosionPrefab,
                        collision.GetContact(0).point + randomOffset,
                        Quaternion.identity
                    );
                }
            }
        }
    }

    private void OnValidate()
    {
        // 確保速度參數為正值
        if (expandSpeed < 0f)
        {
            expandSpeed = 0f;
            Debug.LogWarning("擴展速度不能為負值，已自動調整為0");
        }
        if (shrinkSpeed < 0f)
        {
            shrinkSpeed = 0f;
            Debug.LogWarning("收縮速度不能為負值，已自動調整為0");
        }
        
        // 確保最大長度為正值
        if (maxLength <= 0f)
        {
            maxLength = 1f;
            Debug.LogWarning("最大長度必須大於0，已自動調整為1");
        }
    }
    
    public override void DestroyBullet()
    {
        // 回收自己
        gameObject.SetActive(false);
    }
} 