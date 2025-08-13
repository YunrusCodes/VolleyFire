using UnityEngine;
using System.Collections.Generic;

public class CloudSpawner : MonoBehaviour
{
    [Header("基本設定")]
    [SerializeField] private GameObject cloudPrefab;
    [SerializeField] private Transform centerPoint;

    [Header("生成區域設定")]
    [SerializeField] private float spawnAreaWidth = 100f;
    [SerializeField] private float spawnAreaLength = 100f;
    
    [Header("生成數量設定")]
    [SerializeField] private int maxClouds = 20;        // 最多允許存在幾個
    [SerializeField] private int spawnCountPerTime = 5; // 每次生成幾個
    [SerializeField] private bool autoSpawnAtStart = true; // 啟動時是否自動生成
    
    [Header("生成週期設定")]
    [SerializeField] private bool enableAutoSpawn = true;     // 是否啟用自動生成
    [SerializeField] private float spawnInterval = 3f;        // 生成間隔（秒）
    private float nextSpawnTime;
    
    [Header("移動設定")]
    [SerializeField] private float maxHeight = 50f;
    [SerializeField] private float floatSpeed = 1f;

    private Vector3 CenterPosition => centerPoint != null ? centerPoint.position : transform.position;

    private List<Transform> clouds = new List<Transform>();

    private void Start()
    {
        if (autoSpawnAtStart)
        {
            SpawnClouds();
        }
        nextSpawnTime = Time.time + spawnInterval;
    }

    private void Update()
    {
        // 檢查是否需要自動生成
        if (enableAutoSpawn && Time.time >= nextSpawnTime)
        {
            SpawnClouds();
            nextSpawnTime = Time.time + spawnInterval;
        }

        for (int i = clouds.Count - 1; i >= 0; i--)
        {
            if (clouds[i] != null)
            {
                // 沿著自身Z軸移動
                clouds[i].Translate(Vector3.forward * floatSpeed * Time.deltaTime, Space.Self);

                // 檢查是否超出高度限制
                if (IsAboveMaxHeight(clouds[i].position))
                {
                    ResetCloudPosition(clouds[i]);
                }
            }
            else
            {
                // 移除已銷毀的雲朵引用
                clouds.RemoveAt(i);
            }
        }
    }

    /// <summary>
    /// 生成雲朵的公開方法，可以從其他腳本調用
    /// </summary>
    public void SpawnClouds()
    {
        int remainingSlots = maxClouds - clouds.Count;
        int spawnCount = Mathf.Min(spawnCountPerTime, remainingSlots);

        for (int i = 0; i < spawnCount; i++)
        {
            Transform newCloud = CreateCloud();
            if (newCloud != null)
            {
                clouds.Add(newCloud);
            }
        }
    }

    /// <summary>
    /// 清除所有雲朵
    /// </summary>
    public void ClearAllClouds()
    {
        foreach (Transform cloud in clouds)
        {
            if (cloud != null)
            {
                Destroy(cloud.gameObject);
            }
        }
        clouds.Clear();
    }

    private Transform CreateCloud()
    {
        Vector3 spawnPosition = GetRandomSpawnPosition();
        // 讓Z軸朝上
        Quaternion rotation = Quaternion.Euler(-90f, Random.Range(0f, 360f), 0f);
        GameObject cloud = Instantiate(cloudPrefab, spawnPosition, rotation, transform);
        return cloud.transform;
    }

    private Vector3 GetRandomSpawnPosition()
    {
        Vector3 centerPos = CenterPosition;
        float randomX = Random.Range(-spawnAreaWidth / 2f, spawnAreaWidth / 2f);
        float randomZ = Random.Range(-spawnAreaLength / 2f, spawnAreaLength / 2f);
        return new Vector3(centerPos.x + randomX, centerPos.y, centerPos.z + randomZ);
    }

    private bool IsAboveMaxHeight(Vector3 position)
    {
        return position.y > (CenterPosition.y + maxHeight);
    }

    private void ResetCloudPosition(Transform cloud)
    {
        Vector3 newPosition = GetRandomSpawnPosition();
        cloud.position = newPosition;
    }

    private void OnDrawGizmos()
    {
        Vector3 center = CenterPosition;

        // 繪製中心點
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(center, 2f);

        // 設定 Gizmos 顏色
        Gizmos.color = new Color(0.2f, 0.8f, 1f, 0.3f);
        Vector3 size = new Vector3(spawnAreaWidth, 0.1f, spawnAreaLength);
        Gizmos.DrawCube(center, size);

        // 繪製高度限制
        Gizmos.color = new Color(1f, 0.5f, 0.5f, 0.3f);
        Vector3 heightCenter = center + Vector3.up * maxHeight;
        Vector3 heightSize = new Vector3(spawnAreaWidth, 0.1f, spawnAreaLength);
        Gizmos.DrawCube(heightCenter, heightSize);

        // 繪製連接線框
        Gizmos.color = new Color(0.5f, 0.5f, 0.5f, 0.2f);
        Vector3 halfWidth = Vector3.right * (spawnAreaWidth / 2f);
        Vector3 halfLength = Vector3.forward * (spawnAreaLength / 2f);

        // 繪製四個垂直線
        Vector3[] corners = new Vector3[4];
        corners[0] = center + halfWidth + halfLength;
        corners[1] = center + halfWidth - halfLength;
        corners[2] = center - halfWidth - halfLength;
        corners[3] = center - halfWidth + halfLength;

        for (int i = 0; i < 4; i++)
        {
            Gizmos.DrawLine(corners[i], corners[i] + Vector3.up * maxHeight);
        }
    }
}
