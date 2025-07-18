using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class SatelliteSystem : MonoBehaviour
{
    public Vector2 xRange;
    public Vector2 yRange;
    public float zOffset;
    public GameObject satellitePrefab;
    public float moveSpeed = 5f;

    public float OpenBeamZ;
    public GameObject beam;

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

            GameObject sat = Instantiate(satellitePrefab, spawnPos, rotation);
            satellites.Add(sat);
        }
    }

    private void SpawnTwoOrthogonalSatellites()
    {
        float z = zOffset;

        // === 第 1 顆（在 X 邊）===
        bool leftSide = Random.value < 0.5f;
        float x1 = leftSide ? xRange.x : xRange.y;
        float y1 = Random.Range(yRange.x, yRange.y);
        Vector3 pos1 = new Vector3(x1, y1, z);

        // 朝向對邊隨機 Y 點
        float x1Target = leftSide ? xRange.y : xRange.x;
        float y1Target = Random.Range(yRange.x, yRange.y);
        Vector3 target1 = new Vector3(x1Target, y1Target, z);

        Quaternion rot1 = Quaternion.LookRotation(target1 - pos1);
        GameObject sat1 = Instantiate(satellitePrefab, pos1, rot1);
        satellites.Add(sat1);

        // === 第 2 顆（在 Y 邊）===
        bool bottomSide = Random.value < 0.5f;
        float x2 = Random.Range(xRange.x, xRange.y);
        float y2 = bottomSide ? yRange.x : yRange.y;
        Vector3 pos2 = new Vector3(x2, y2, z);

        // 朝向對邊隨機 X 點
        float y2Target = bottomSide ? yRange.y : yRange.x;
        float x2Target = Random.Range(xRange.x, xRange.y);
        Vector3 target2 = new Vector3(x2Target, y2Target, z);

        Quaternion rot2 = Quaternion.LookRotation(target2 - pos2);
        GameObject sat2 = Instantiate(satellitePrefab, pos2, rot2);
        satellites.Add(sat2);
    }


    bool LineLineIntersection(Vector3 p1, Vector3 d1, Vector3 p2, Vector3 d2, out float t1, out float t2)
    {
        t1 = t2 = 0f;
        Vector3 dp = p2 - p1;
        float v12 = Vector3.Dot(d1, d1);
        float v22 = Vector3.Dot(d2, d2);
        float v1v2 = Vector3.Dot(d1, d2);
        float denom = v12 * v22 - v1v2 * v1v2;
        if (Mathf.Abs(denom) < 1e-6f) return false;
        t1 = (Vector3.Dot(dp, d1) * v22 - Vector3.Dot(dp, d2) * v1v2) / denom;
        t2 = (Vector3.Dot(dp, d2) * v12 - Vector3.Dot(dp, d1) * v1v2) / denom;
        Vector3 cross = p1 + d1 * t1;
        if (cross.x < xRange.x || cross.x > xRange.y || cross.y < yRange.x || cross.y > yRange.y)
            return false;
        return true;
    }

    void Update()
    {
        Vector3 moveDir = -this.transform.forward;
        for (int i = satellites.Count - 1; i >= 0; i--)
        {
            GameObject sat = satellites[i];
            if (sat == null)
            {
                satellites.RemoveAt(i);
                continue;
            }

            sat.transform.position += moveDir * moveSpeed * Time.deltaTime;

            if (!beamsSpawned.Contains(sat))
            {
                float dist = Mathf.Abs(sat.transform.position.z - this.transform.position.z);
                if (dist <= OpenBeamZ)
                {
                    GameObject b = Instantiate(beam, sat.transform.position, sat.transform.rotation, sat.transform);
                    b.transform.localRotation = Quaternion.identity;

                    CannonRay ray = b.GetComponent<CannonRay>();
                    if (ray != null)
                    {
                        ray.SetSpawnPoint(sat.transform);
                    }

                    beamsSpawned.Add(sat);
                }
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
