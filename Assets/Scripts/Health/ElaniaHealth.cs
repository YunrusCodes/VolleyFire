using UnityEngine;
using System.Collections.Generic;
using VolleyFire.Bullets;  // 加入 BulletSettings 的命名空間

public class ElaniaHealth : EnemyHealth
{
    private GameObject lastHitBullet;  // 記錄最後打中的子彈
    [Header("反射子彈設定")]
    [SerializeField] public BulletSettings reflectedBulletSettings;  // 改為 public 並加入 Header
    [SerializeField] public GameObject reflectDefaultBulletExplosionPrefab;
    [SerializeField] public GameObject teleportEffectPrefab;

    private void OnCollisionEnter(Collision collision)
    {
        // 檢查是否是子彈
        BulletBehavior bullet = collision.gameObject.GetComponent<BulletBehavior>();
        if (bullet != null)
        {
            lastHitBullet = collision.gameObject;
            
            // 如果血量低於閾值且有活躍的蟲洞，進行子彈複製
            if (currentHealth <= INVINCIBLE_THRESHOLD && activeWormholes.Count > 0)
            {
                // 隨機選擇一個蟲洞
                GameObject randomWormhole = activeWormholes[Random.Range(0, activeWormholes.Count)];
                if (randomWormhole != null)
                {
                    // 複製子彈，先將旋轉歸零
                    GameObject newBullet = Instantiate(lastHitBullet, 
                        randomWormhole.transform.position, 
                        Quaternion.identity);
                    newBullet.tag = "Teleported";

                    // 移除所有原有的子彈行為組件，但先保存爆炸特效
                    GameObject explosionPrefab = null;
                    BulletBehavior[] oldBehaviors = newBullet.GetComponents<BulletBehavior>();
                    if(oldBehaviors.Length > 0){

                        foreach (var behavior in oldBehaviors)
                        {
                            // 如果還沒有取得爆炸特效，就從這個行為組件取得
                            if (explosionPrefab == null)
                            {
                                explosionPrefab = behavior.GetExplosionPrefab();
                            }
                            Destroy(behavior);
                        }
                    }
                    else{
                        explosionPrefab = reflectDefaultBulletExplosionPrefab;
                    }
                    ControllableObject[] oldControllableObjects = newBullet.GetComponents<ControllableObject>();
                    if(oldControllableObjects.Length > 0){
                        foreach (var controllableObject in oldControllableObjects)
                        {
                            Destroy(controllableObject);
                        }
                    }

                    // 添加新的 StraightBullet 組件並設置其設定
                    StraightBullet straightBullet = newBullet.AddComponent<StraightBullet>();
                    
                    // 設置子彈屬性
                    if (reflectedBulletSettings != null)
                    {
                        // 設置基本屬性
                        straightBullet.SetFromSettings(reflectedBulletSettings);
                        
                        // 設置目標標籤為 "Player"
                        reflectedBulletSettings.targetTag = "Player";
                        
                        // 如果設定檔中沒有爆炸特效，使用原始子彈的特效
                        GameObject effectToUse = reflectedBulletSettings.explosionPrefab ?? explosionPrefab;
                        if( reflectedBulletSettings.explosionPrefab == null){
                            reflectedBulletSettings.explosionPrefab = explosionPrefab;
                        }
                        // 設置基本屬性
                        straightBullet.SetFromSettings(reflectedBulletSettings);
                        
                    }
                    
                    // 找到玩家並設置朝向玩家的方向
                    GameObject player = GameObject.FindGameObjectWithTag("Player");
                    if (player != null)
                    {
                        // 計算朝向玩家的方向
                        Vector3 directionToPlayer = (player.transform.position - newBullet.transform.position).normalized;
                        
                        // 設置子彈的旋轉，使其朝向玩家
                        newBullet.transform.rotation = Quaternion.LookRotation(directionToPlayer);
                        
                        // 設置移動方向
                        straightBullet.SetDirection(directionToPlayer);
                    }
                    else
                    {
                        // 如果找不到玩家，就使用原始子彈的反方向
                        straightBullet.SetDirection(-lastHitBullet.transform.forward);
                    }
                }
                
        
                Instantiate(teleportEffectPrefab, collision.transform.position, collision.transform.rotation);
                Destroy(collision.gameObject);
                StageManager.Instance.ShowDamageText(transform.position, 0, damageTextOffset, Color.gray, "Teleported!");
            }
        }
    }

    private const float INVINCIBLE_THRESHOLD = 500f;
    private List<GameObject> activeWormholes = new List<GameObject>();
    private ElaniaBehavior elaniaBehavior;

    protected override void Awake()
    {
        base.Awake();
        elaniaBehavior = GetComponent<ElaniaBehavior>();

        // 實例化 reflectedBulletSettings
        if (reflectedBulletSettings != null)
        {
            reflectedBulletSettings = Instantiate(reflectedBulletSettings);
        }
    }

    public void UpdateWormholes(List<GameObject> wormholes)
    {
        activeWormholes = new List<GameObject>(wormholes);
    }

    public override void TakeDamage(float damage)
    {
        // 正常扣血
        base.TakeDamage(damage);
    }
}
