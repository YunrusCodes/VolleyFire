using UnityEngine;

public class SatelliteObject : ControllableObject
{
    [SerializeField] private float releaseDamage = 50f;     // 釋放後的傷害值
    [SerializeField] private string targetTag = "Enemy";     // 目標標籤
    [SerializeField] private GameObject beam;                // 光束預製體
    [SerializeField] private float openBeamZ = 1f;         // 開啟光束的Z軸距離
    [SerializeField] private float closeBeamZ = 0.5f;      // 關閉光束的Z軸距離
    [SerializeField] private float destroySelfZ = 0.3f;    // 自我銷毀的Z軸距離

    private GameObject spawnedBeam;                          // 已生成的光束
    private Transform spawnPoint;                            // 生成點
    private static bool canAttack = true;                   // 是否可以攻擊（靜態變數）

    protected override void BehaviorOnStart()
    {
        base.BehaviorOnStart();
        direction = -Vector3.forward;
    }

    public void Initialize(Vector3 direction, Transform spawnPoint)
    {
        this.spawnPoint = spawnPoint;
        // 設定初始朝向
        transform.forward = direction;
    }

    protected override void MoveUncontrolled()
    {
        base.MoveUncontrolled();

        float dist = transform.position.z - spawnPoint.position.z;
        Debug.Log("dist: " + dist);

        // 檢查是否需要開啟或關閉光束
        if (canAttack)
        {
            if (dist <= openBeamZ && dist > closeBeamZ && spawnedBeam == null)
            {
                SpawnBeam();
            }
            else if (dist <= closeBeamZ && spawnedBeam != null)
            {
                DestroyBeam();
            }
        }

        // 檢查是否需要自我銷毀
        if (dist <= destroySelfZ)
        {
            Destroy(gameObject);
        }
    }

    protected override void OnControlled()
    {
        base.OnControlled();
        DestroyBeam();
    }

    protected override void OnReleased()
    {
        base.OnReleased();
        DestroyBeam();
    }

    protected override void MoveReleased()
    {
        speed = originalSpeed * releaseSpeedMultiplier;
        transform.position += direction * speed * Time.deltaTime;
    }

    private void SpawnBeam()
    {
        if (beam != null && spawnedBeam == null)
        {
            spawnedBeam = Instantiate(beam, transform.position, transform.rotation, transform);
            spawnedBeam.transform.localRotation = Quaternion.identity;

            CannonRay ray = spawnedBeam.GetComponent<CannonRay>();
            if (ray != null)
            {
                ray.SetSpawnPoint(transform);
            }
        }
    }

    private void DestroyBeam()
    {
        if (spawnedBeam != null)
        {
            Destroy(spawnedBeam);
            spawnedBeam = null;
        }
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

            // 銷毀自己
            DestroyBullet();
        }
    }

    public static void SetAttackState(bool state)
    {
        canAttack = state;
        if (!state)
        {
            // 找到所有SatelliteObject並銷毀它們的光束
            SatelliteObject[] satellites = GameObject.FindObjectsOfType<SatelliteObject>();
            foreach (SatelliteObject satellite in satellites)
            {
                satellite.DestroyBeam();
            }
        }
    }
} 