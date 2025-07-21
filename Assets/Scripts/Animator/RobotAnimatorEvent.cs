using UnityEngine;

public class RobotAnimatorEvent : MonoBehaviour
{
    [Header("武器特效")]
    [SerializeField] private GameObject bulletPrefab;
    [SerializeField] private GameObject slashPrefab;
    [SerializeField] private Transform bulletSpawnPoint;
    [SerializeField] private Transform slashSpawnPoint;

    [Header("音效")]
    [SerializeField] private AudioClip drawSwordSound;
    [SerializeField] private AudioClip slashSound;
    private AudioSource audioSource;
    [SerializeField] private EnemyBehavior enemyBehavior;  // 新增：引用 EnemyBehavior

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();
            
    }

    // 新增：死亡動畫事件
    public void OnDeathAnimationFinished()
    {
        Debug.Log("OnDeathAnimationFinished");
        Debug.Log(enemyBehavior.isLeaving);
        if (enemyBehavior != null)
        {
            enemyBehavior.isLeaving = true;
        }
    }

    // 射擊動畫事件
    public void SpawnBullet()
    {
        if (bulletPrefab != null && bulletSpawnPoint != null)
        {
            Instantiate(bulletPrefab, bulletSpawnPoint.position, bulletSpawnPoint.rotation);
        }
        else
        {
            Debug.LogWarning("bulletPrefab 或 bulletSpawnPoint 未設定！");
        }
    }

    // 斬擊動畫事件
    public void SpawnSlash()
    {
        if (slashPrefab != null && slashSpawnPoint != null)
        {
            GameObject slash = Instantiate(slashPrefab, slashSpawnPoint.position, slashSpawnPoint.rotation);
            slash.SetActive(true);
        }
        else
        {
            Debug.LogWarning("slashPrefab 或 slashSpawnPoint 未設定！");
        }
    }

    // 拔刀音效
    public void PlayDrawSwordSound()
    {
        if (drawSwordSound != null)
        {
            audioSource.clip = drawSwordSound;
            audioSource.Play();
        }
        else
        {
            Debug.LogWarning("drawSword 音效未設定！");
        }
    }

    // 斬擊音效
    public void PlaySlashSound()
    {
        if (slashSound != null)
        {
            audioSource.clip = slashSound;
            audioSource.Play();
        }
        else
        {
            Debug.LogWarning("slash 音效未設定！");
        }
    }
} 