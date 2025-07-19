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
    public Transform PointedTarget;    // 指向性子彈的目標 Transform（可為 null）
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

    private float[]       nextFireTimes;   // 個別冷卻計時
    private bool          canFire = true;  // 全局開火開關

    private void Awake()
    {
        // 移除物件池初始化
        // InitializeProjectilePools();
        nextFireTimes = new float[firePoints.Length];
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
        // 直接 Instantiate 子彈
        GameObject proj = Object.Instantiate(firePoints[idx].bulletPrefab, firePoints[idx].transform.position, firePoints[idx].transform.rotation);

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

    public void SetPointerTarget(Transform target)
    {
        for (int i = 0; i < firePoints.Length; i++)
        {
            firePoints[i].PointedTarget = target;
        }
    }

    #endregion
}
