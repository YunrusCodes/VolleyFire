using UnityEngine;

public class SwordSlash : MonoBehaviour
{
    [Header("斬擊Prefab")]
    public GameObject slashPrefab;
    [Header("生成位置")]
    public Transform spawnPoint;

    // 公開方法：在生成位置生成劍痕
    public void SpawnSlash()
    {
        if (slashPrefab != null && spawnPoint != null)
        {
           GameObject slash = Instantiate(slashPrefab, spawnPoint.position, spawnPoint.rotation);
           slash.SetActive(true);
        }
        else
        {
            Debug.LogWarning("slashPrefab 或 spawnPoint 未設定！");
        }
    }
}
