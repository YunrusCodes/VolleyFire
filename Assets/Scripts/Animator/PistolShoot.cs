using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class PistolShoot : MonoBehaviour
{
    [Header("子彈Prefab")]
    public GameObject bulletPrefab;
    [Header("生成位置")]
    public Transform spawnPoint;

    // 公開方法：在生成位置生成子彈
    public void SpawnBullet()
    {
        if (bulletPrefab != null && spawnPoint != null)
        {
            Instantiate(bulletPrefab, spawnPoint.position, spawnPoint.rotation);
        }
        else
        {
            Debug.LogWarning("bulletPrefab 或 spawnPoint 未設定！");
        }
    }
}
