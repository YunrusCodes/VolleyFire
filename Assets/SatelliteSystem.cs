using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class SatelliteSystem : MonoBehaviour
{
    public Vector2 xRange; // x.x = min, x.y = max
    public Vector2 yRange; // y.x = min, y.y = max
    public float zOffset;  // 固定 z 值
    public GameObject satellitePrefab; // 指定 Satellite 預置物
    public float moveSpeed = 5f; // 衛星移動速度

    public float OpenBeamZ; // 觸發 beam 的 z 距離
    public GameObject beam; // beam 預置物

    private List<GameObject> satellites = new List<GameObject>();
    private HashSet<GameObject> beamsSpawned = new HashSet<GameObject>();

    void Start()
    {
        StartCoroutine(SpawnRoutine());
    }

    IEnumerator SpawnRoutine()
    {
        while (true)
        {
            SpawnSatellites();
            yield return new WaitForSeconds(5f);
        }
    }

    public void SpawnSatellites()
    {
        int count = Random.Range(1, 5); // 1~4
        List<int> edges = new List<int> { 0, 1, 2, 3 };
        // 洗牌
        for (int i = 0; i < edges.Count; i++)
        {
            int j = Random.Range(i, edges.Count);
            int temp = edges[i];
            edges[i] = edges[j];
            edges[j] = temp;
        }

        for (int n = 0; n < count; n++)
        {
            int edge = edges[n];
            float x, y, z = zOffset;
            Vector3 spawnPos;
            Vector3 lookTarget;

            switch (edge)
            {
                case 0: // 左（-x）
                    x = xRange.x;
                    y = Random.Range(yRange.x, yRange.y);
                    spawnPos = new Vector3(x, y, z);
                    lookTarget = new Vector3(xRange.y, Random.Range(yRange.x, yRange.y), z);
                    break;
                case 1: // 右（+x）
                    x = xRange.y;
                    y = Random.Range(yRange.x, yRange.y);
                    spawnPos = new Vector3(x, y, z);
                    lookTarget = new Vector3(xRange.x, Random.Range(yRange.x, yRange.y), z);
                    break;
                case 2: // 下（-y）
                    x = Random.Range(xRange.x, xRange.y);
                    y = yRange.x;
                    spawnPos = new Vector3(x, y, z);
                    lookTarget = new Vector3(Random.Range(xRange.x, xRange.y), yRange.y, z);
                    break;
                case 3: // 上（+y）
                    x = Random.Range(xRange.x, xRange.y);
                    y = yRange.y;
                    spawnPos = new Vector3(x, y, z);
                    lookTarget = new Vector3(Random.Range(xRange.x, xRange.y), yRange.x, z);
                    break;
                default:
                    x = xRange.x;
                    y = yRange.x;
                    spawnPos = new Vector3(x, y, z);
                    lookTarget = new Vector3(xRange.y, yRange.y, z);
                    break;
            }

            Quaternion rotation = Quaternion.LookRotation(lookTarget - spawnPos);
            GameObject sat = Instantiate(satellitePrefab, spawnPos, rotation);
            satellites.Add(sat);
        }
    }

    void Update()
    {
        // 統一控制所有 Satellite 沿本體 -z 軸移動
        Vector3 moveDir = -this.transform.forward;
        for (int i = satellites.Count - 1; i >= 0; i--)
        {
            GameObject sat = satellites[i];
            if (sat == null) {
                satellites.RemoveAt(i);
                continue;
            }
            sat.transform.position += moveDir * moveSpeed * Time.deltaTime;

            // 判斷是否需要生成 beam
            if (!beamsSpawned.Contains(sat))
            {
                float dist = Mathf.Abs(sat.transform.position.z - this.transform.position.z);
                if (dist <= OpenBeamZ)
                {
                    GameObject b = Instantiate(beam, sat.transform.position, sat.transform.rotation, sat.transform);
                    b.transform.localRotation = Quaternion.identity;
                    // 例如設置 spawnPoint
                    CannonRay ray = b.GetComponent<CannonRay>();
                    if (ray != null)
                    {
                        ray.SetSpawnPoint(sat.transform); // 或其他你想指定的 Transform
                    }
                    beamsSpawned.Add(sat);
                }
            }
        }
    }

    void OnDrawGizmos()
    {
        // 設定顏色
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
