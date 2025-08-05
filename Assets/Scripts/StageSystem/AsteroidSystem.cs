using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class AsteroidSystem : MonoBehaviour
{
    public Vector2 xRange;
    public Vector2 yRange;
    public float zOffset;
    public GameObject asteroidPrefab;

    void Start()
    {
        StartCoroutine(SpawnRoutine());
    }

    IEnumerator SpawnRoutine()
    {
        while (true)
        {
            SpawnAsteroids();
            yield return new WaitForSeconds(5f);
        }
    }

    public void SpawnAsteroids()
    {
        int count = Random.Range(1, 5); // 1 ~ 4
        float z = zOffset;
        // 計算中心點
        float centerX = (xRange.x + xRange.y) * 0.5f;
        float centerY = (yRange.x + yRange.y) * 0.5f;
        Vector3 centerPoint = new Vector3(centerX, centerY, z);

        // 選出要用哪些邊
        List<int> edges = new List<int> { 0, 1, 2, 3 }; // 0: left, 1: right, 2: bottom, 3: top
        // Fisher-Yates 洗牌
        for (int i = edges.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            int temp = edges[i];
            edges[i] = edges[j];
            edges[j] = temp;
        }
        edges = edges.GetRange(0, count);

        for (int i = 0; i < count; i++)
        {
            int edge = edges[i];
            float x = 0, y = 0;
            Vector3 spawnPos = Vector3.zero;
            Vector3 lookTarget = centerPoint;

            switch (edge)
            {
                case 0: // left
                    x = xRange.x;
                    y = Random.Range(yRange.x, yRange.y);
                    spawnPos = new Vector3(x, y, z);
                    break;
                case 1: // right
                    x = xRange.y;
                    y = Random.Range(yRange.x, yRange.y);
                    spawnPos = new Vector3(x, y, z);
                    break;
                case 2: // bottom
                    x = Random.Range(xRange.x, xRange.y);
                    y = yRange.x;
                    spawnPos = new Vector3(x, y, z);
                    break;
                case 3: // top
                    x = Random.Range(xRange.x, xRange.y);
                    y = yRange.y;
                    spawnPos = new Vector3(x, y, z);
                    break;
            }

            Vector3 dir = lookTarget - spawnPos;
            if (dir.sqrMagnitude < 1e-4f) dir = Vector3.forward;
            Quaternion rotation = Quaternion.LookRotation(dir);

            // 加入隨機旋轉
            rotation *= Quaternion.Euler(Random.Range(0f, 360f), Random.Range(0f, 360f), Random.Range(0f, 360f));

            // 生成隕石
            GameObject asteroid = Instantiate(asteroidPrefab, spawnPos, rotation);
            AsteroidObject asteroidObj = asteroid.GetComponent<AsteroidObject>();
            if (asteroidObj != null)
            {
                asteroidObj.Initialize(transform);
            }
        }
    }

    void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        float z = zOffset;
        Vector3 p1 = new Vector3(xRange.x, yRange.x, z);
        Vector3 p2 = new Vector3(xRange.x, yRange.y, z);
        Vector3 p3 = new Vector3(xRange.y, yRange.y, z);
        Vector3 p4 = new Vector3(xRange.y, yRange.x, z);
        Gizmos.DrawLine(p1, p2);
        Gizmos.DrawLine(p2, p3);
        Gizmos.DrawLine(p3, p4);
        Gizmos.DrawLine(p4, p1);
    }
} 