using UnityEngine;

#region FirePoint

[System.Serializable]
public struct FirePoint
{
    public Transform   transform;      // 發射點位置
    public GameObject  bulletPrefab;   // 子彈預製體 (需掛 BulletBehavior)
    public AudioClip   fireSound;      // 射擊音效
    public AudioSource audioSource;    // 音效來源
    [Min(0.01f)] public float firePeriod;        // **各自射速 (秒/發)**
    public float projectileSpeed;      // 若 BulletBehavior 需要，可傳此值
    public Vector3? PointedTarget;     // 指向性子彈的目標位置（可為 null）
}

#endregion

/// <summary>
/// WeaponSystem — per‑firepoint cooldown 版
/// · FirePoint 決定自身射速，不使用 globalFireRate。
/// </summary>
public class WeaponSystem : MonoBehaviour
{
    #region Inspector

    [Header("武器設定")]
    [SerializeField] private FirePoint[] firePoints;   // 各槍口設定 (必填)
    [SerializeField] private int         maxProjectiles = 20; // 每槍口物件池大小

    #endregion

    private ObjectPool[] projectilePools; // 對應各 FirePoint 的物件池
    private float[]       nextFireTimes;   // 個別冷卻計時
    private bool          canFire = true;  // 全局開火開關

    private void Awake()
    {
        InitializeProjectilePools();
        nextFireTimes = new float[firePoints.Length];
    }

    /// <summary>
    /// 建立物件池 (每 FirePoint 一池)。
    /// </summary>
    private void InitializeProjectilePools()
    {
        projectilePools = new ObjectPool[firePoints.Length];
        for (int i = 0; i < firePoints.Length; i++)
        {
            projectilePools[i] = new ObjectPool(firePoints[i].bulletPrefab,
                                                maxProjectiles, transform);
        }
    }

    /// <summary>
    /// 嘗試射擊：逐槍口判斷。
    /// </summary>
    public void Fire()
    {
        if (!canFire) return;

        for (int i = 0; i < firePoints.Length; i++)
        {
            if (Time.time >= nextFireTimes[i])
            {
                SpawnProjectile(i);
                float rate = Mathf.Max(0.01f, firePoints[i].firePeriod);
                nextFireTimes[i] = Time.time + rate;
            }
        }
    }

    /// <summary>
    /// 生成並初始化子彈。
    /// </summary>
    private void SpawnProjectile(int idx)
    {
        GameObject proj = projectilePools[idx].GetObject();

        // 位置與旋轉
        proj.transform.SetPositionAndRotation(firePoints[idx].transform.position,
                                              firePoints[idx].transform.rotation);

        // 初始化子彈行為
        var bullet = proj.GetComponent<BulletBehavior>();
        bullet?.Initialize(firePoints[idx]);

        // 音效
        PlayFireSound(firePoints[idx]);
    }

    private static void PlayFireSound(in FirePoint fp)
    {
        if (fp.audioSource && fp.fireSound)
            fp.audioSource.PlayOneShot(fp.fireSound);
    }

    #region 公開 API

    public void SetCanFire(bool enabled) => canFire = enabled;

    /// <summary>動態調整單一 FirePoint 射速。</summary>
    public void SetFireRate(int index, float newRate)
    {
        if (index < 0 || index >= firePoints.Length) return;
        firePoints[index].firePeriod = Mathf.Max(0.01f, newRate);
    }

    public void SetPointerTarget(Vector3 target)
    {
        for (int i = 0; i < firePoints.Length; i++)
        {
            firePoints[i].PointedTarget = target;
        }
    }

    #endregion
}

#region ObjectPool

public class ObjectPool
{
    private readonly GameObject[] pool;
    private int current;

    public ObjectPool(GameObject prefab, int size, Transform parent)
    {
        pool = new GameObject[size];
        for (int i = 0; i < size; i++)
        {
            pool[i] = Object.Instantiate(prefab, parent);
            pool[i].SetActive(false);
        }
    }

    public GameObject GetObject()
    {
        // 找未啟用物件
        for (int i = 0; i < pool.Length; i++)
        {
            int idx = (current + i) % pool.Length;
            if (!pool[idx].activeInHierarchy)
            {
                current = (idx + 1) % pool.Length;
                pool[idx].SetActive(true);
                return pool[idx];
            }
        }

        // 全滿 → 覆蓋 current
        pool[current].SetActive(true);
        current = (current + 1) % pool.Length;
        return pool[(current + pool.Length - 1) % pool.Length];
    }
}

#endregion
