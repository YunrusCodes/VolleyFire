using UnityEngine;

public class CannonRay : BulletBehavior
{
    [Header("雷射設定")]
    [SerializeField] private GameObject chargingEffect;    // 續能特效
    [SerializeField] private GameObject laserObject;       // 雷射物件
    [SerializeField] private float chargingTime = 1.0f;   // 續能時間
    [SerializeField] private float activeTime = 2.0f;     // 作用時間
    [SerializeField] private float expandSpeed = 100f;     // 雷射擴展速度
    [SerializeField] private float shrinkSpeed = 5f;      // 雷射收縮速度
    [SerializeField] private float maxLength = 30f;       // 最大長度
    [SerializeField] private string targetTag = "Player";   // 目標標籤
    [SerializeField] private float damage = 5;              // 傷害值

    private float elapsedTime = 0f;                       // 已經過時間
    private bool isCharging = true;                       // 是否在續能中
    private bool isActive = false;                        // 是否在作用中
    private bool isShrinking = false;                     // 是否在收縮中
    private Transform spawnPoint;                         // 發射點
    private float damageInterval = 0.1f; // 傷害間隔
    private float damageTimer = 0f;      // 傷害計時器

    protected override void BehaviorOnStart()
    {
        // 初始化狀態
        elapsedTime = 0f;
        isCharging = true;
        isActive = false;
        isShrinking = false;

        // 設置初始狀態
        if (chargingEffect) chargingEffect.SetActive(true);
        if (laserObject)
        {
            laserObject.SetActive(false);
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
        if (spawnPoint != null)
        {
            transform.position = spawnPoint.position;
            transform.rotation = spawnPoint.rotation;
        }

        // 更新計時器
        elapsedTime += Time.deltaTime;

        // 處理各階段
        if (isCharging)
        {
            HandleCharging();
        }
        else if (isActive)
        {
            HandleActive();
        }
        else if (isShrinking)
        {
            HandleShrinking();
        }
    }

    private void HandleCharging()
    {
        if (elapsedTime >= chargingTime)
        {
            // 續能完成，進入作用階段
            isCharging = false;
            isActive = true;
            elapsedTime = 0f;

            // 切換特效
            if (chargingEffect) chargingEffect.SetActive(false);
            if (laserObject) laserObject.SetActive(true);
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

            // 檢查是否結束作用階段
            if (elapsedTime >= activeTime)
            {
                isActive = false;
                isShrinking = true;
                elapsedTime = 0f;
            }
        }
    }

    private void HandleShrinking()
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
                targetHealth.TakeDamage(damage / 10f);

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
        // 確保時間參數為正值
        if (chargingTime < 0f)
        {
            chargingTime = 0f;
            Debug.LogWarning("續能時間不能為負值，已自動調整為0");
        }
        if (activeTime < 0f)
        {
            activeTime = 0f;
            Debug.LogWarning("作用時間不能為負值，已自動調整為0");
        }
        
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
} 