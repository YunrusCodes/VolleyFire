using System.Collections.Generic;
using UnityEngine;
using System.Collections;

public class FunnelSystem : MonoBehaviour
{
    public bool enableAction = false;
    public void SetEnableAction(bool value)
    {
        Debug.Log("SetEnableAction: " + value);
        if(value == enableAction) return;
        enableAction = value;
        ApplyMode(FunnelMode.StandBy);
    }
    public void Attack()
    {
        Debug.Log("Attack");
        if(!enableAction || (Mode == FunnelMode.StandBy && _activeCoroutineCount > 0)) return;
        ApplyMode(FunnelMode.AttackPattern);
    }
    public void StandBy()
    {
        Debug.Log("StandBy");
        if(!enableAction && Mode == FunnelMode.AttackPattern) return;
        ApplyMode(FunnelMode.StandBy);
    }

    public enum FunnelMode
    {
        AllAtMaster,
        AttackPattern,
        StandBy
    }

    public List<Transform> Funnels = new List<Transform>();
    public FunnelMode Mode = FunnelMode.AllAtMaster;
    public float LookAtPlaneOffset = 2.0f;
    
    // 修改為世界座標的範圍設定
    [Header("世界座標範圍設定")]
    public Vector2 WorldXRange = new Vector2(-5, 5); // X軸世界座標範圍
    public Vector2 WorldYRange = new Vector2(-5, 5); // Y軸世界座標範圍
    public float WorldZOffset = 1.0f; // Z軸偏移（相對於中心點）
    public Vector3 WorldCenterPoint; // 世界座標中心點

    [Header("縮放設定")]
    [Range(0.1f, 1.0f)]
    public float MinScale = 0.2f; // 最小縮放（在-Z平面）
    [Range(0.1f, 1.0f)]
    public float MaxScale = 1.0f; // 最大縮放（在+Z平面）

    [Header("間距設定")]
    public float MinFunnelDistance = 2.0f; // Funnel 之間的最小距離

    public GameObject BulletPrefab;
    public float MovementSpeed = 5f;
    public float RotationSpeed = 180f;
    public Material RayMaterial; // 新增：射線的材質

    public float StandByRaycastDistance = 400f; // 射線檢測距離
    public float StandByShootCooldown = 1.5f; // 射擊冷卻時間

    private List<Coroutine> _attackCoroutines = new List<Coroutine>();
    private int _activeCoroutineCount;
    private Dictionary<Transform, float> _funnelLastShootTime = new Dictionary<Transform, float>();
    private Dictionary<Transform, GameObject> _funnelAimMarkers = new Dictionary<Transform, GameObject>(); // 新增：儲存每個Funnel的瞄準標記
    private Dictionary<Transform, Vector3> _funnelLastPositions = new Dictionary<Transform, Vector3>(); // 新增：記錄每個 Funnel 的上次位置

    void Start()
    {
        // 設置世界座標中心點（如果未指定）
        if (WorldCenterPoint == Vector3.zero)
        {
            WorldCenterPoint = transform.position;
        }
    }

    void ApplyMode(FunnelMode mode)
    {
        // 先停止所有現有的攻擊協程
        foreach (var coroutine in _attackCoroutines)
        {
            if (coroutine != null)
                StopCoroutine(coroutine);
        }
        _attackCoroutines.Clear();
        _activeCoroutineCount = 0;

        switch (mode)
        {
            case FunnelMode.AllAtMaster:
                SetAllAtMaster();
                break;
            case FunnelMode.AttackPattern:
                StartAttackPattern();
                break;
            case FunnelMode.StandBy:
                StartSequentialMoveToStandBy();
                break;
        }
    }

    void SetAllAtMaster()
    {
        return;
    }

    void StartAttackPattern()
    {
        _activeCoroutineCount = 0;
        for (int i = 0; i < Funnels.Count; i++)
        {
            var funnel = Funnels[i];
            if (funnel != null)
            {
                _activeCoroutineCount++;
                var coroutine = StartCoroutine(AttackPatternCoroutine(funnel, i * 0.75f));  // 添加延遲參數
                _attackCoroutines.Add(coroutine);
            }
        }
    }

    IEnumerator AttackPatternCoroutine(Transform funnel, float startDelay)
    {
        Debug.Log("AttackPatternCoroutine");
        // 等待指定的延遲時間後開始
        yield return new WaitForSeconds(startDelay);

        // 1. 從-Z平面開始（已經在StandBy狀態）移動到Master.z平面
        Vector3 masterZTarget = GetRandomPositionOnPlane(0, funnel);
        yield return StartCoroutine(MoveToPositionFacingDirection(funnel, masterZTarget));

        // 2. 移動到+Z平面，同時漸進轉向玩家
        Vector3 positiveZTarget = GetRandomPositionOnPlane(-WorldZOffset, funnel);
        yield return StartCoroutine(MoveToPositionWithRotationLerp(funnel, positiveZTarget, true));
        
        // 在+Z平面停留0.5秒
        yield return new WaitForSeconds(0.5f);
        
        // 3. 立即發射子彈
        if (BulletPrefab != null)
        {
            Instantiate(BulletPrefab, funnel.position, funnel.rotation);
        }

        // 4. 移動回Master.z平面，漸進轉向移動方向
        masterZTarget = GetRandomPositionOnPlane(0, funnel);
        yield return StartCoroutine(MoveToPositionWithRotationLerp(funnel, masterZTarget, false));

        // 減少活動協程計數
        _activeCoroutineCount--;

        // 直接開始這個 Funnel 的 StandBy 行為
        yield return StartCoroutine(MoveToStandByCoroutine(funnel, 0f));
        Mode = FunnelMode.StandBy;
    }

    IEnumerator MoveToPositionWithRotationLerp(Transform funnel, Vector3 targetPosition, bool isAimingAtPlayer)
    {
        Debug.DrawLine(funnel.position, targetPosition, Color.red);
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null && isAimingAtPlayer) yield break;

        Vector3 startPosition = funnel.position;
        Quaternion startRotation = funnel.rotation;
        float journeyLength = Vector3.Distance(startPosition, targetPosition);
        float startTime = Time.time;

        while (Vector3.Distance(funnel.position, targetPosition) > 0.1f)
        {
            // 計算移動進度（0到1之間）
            float distanceCovered = (Time.time - startTime) * MovementSpeed;
            float fractionOfJourney = distanceCovered / journeyLength;
            fractionOfJourney = Mathf.Clamp01(fractionOfJourney);

            // 移動
            funnel.position = Vector3.MoveTowards(funnel.position, targetPosition, MovementSpeed * Time.deltaTime);

            // 計算目標旋轉
            Quaternion targetRotation;
            if (isAimingAtPlayer && player != null)
            {
                // 面向玩家的旋轉
                Vector3 directionToPlayer = (player.transform.position - funnel.position).normalized;
                targetRotation = Quaternion.LookRotation(directionToPlayer);
            }
            else
            {
                // 面向移動方向的旋轉
                Vector3 moveDirection = (targetPosition - funnel.position).normalized;
                targetRotation = Quaternion.LookRotation(moveDirection);
            }

            // 根據移動進度進行插值
            Quaternion lerpedRotation = Quaternion.Lerp(startRotation, targetRotation, fractionOfJourney);
            funnel.rotation = Quaternion.RotateTowards(funnel.rotation, lerpedRotation, RotationSpeed * Time.deltaTime);

            yield return null;
        }

        // 確保最終旋轉正確
        if (isAimingAtPlayer && player != null)
        {
            Vector3 finalDirectionToPlayer = (player.transform.position - funnel.position).normalized;
            funnel.rotation = Quaternion.LookRotation(finalDirectionToPlayer);
        }
        else
        {
            Vector3 finalMoveDirection = (targetPosition - startPosition).normalized;
            funnel.rotation = Quaternion.LookRotation(finalMoveDirection);
        }
    }

    Vector3 GetRandomPositionOnPlane(float zOffset, Transform currentFunnel)
    {
        const int MAX_ATTEMPTS = 10; // 最大嘗試次數
        const float CHECK_RADIUS = 5f; // 碰撞檢查半徑

        // 計算縮放係數：從 +Z 到 -Z 逐漸縮小
        float scale = Mathf.Lerp(MinScale, MaxScale, (zOffset + WorldZOffset) / (WorldZOffset * 2));
        
        for (int attempt = 0; attempt < MAX_ATTEMPTS; attempt++)
        {
            // 根據縮放係數調整範圍
            Vector2 scaledX = new Vector2(WorldXRange.x * scale, WorldXRange.y * scale);
            Vector2 scaledY = new Vector2(WorldYRange.x * scale, WorldYRange.y * scale);
            
            float x = Random.Range(scaledX.x, scaledX.y) + WorldCenterPoint.x;
            float y = Random.Range(scaledY.x, scaledY.y) + WorldCenterPoint.y;
            float z = WorldCenterPoint.z + zOffset;
            Vector3 position = new Vector3(x, y, z);

            // 檢查該位置是否有碰撞體
            Collider[] colliders = Physics.OverlapSphere(position, CHECK_RADIUS);
            if (colliders.Length > 0)
            {
                Debug.Log($"Position {position} has collision, trying again. Attempt: {attempt + 1}");
                continue;
            }

            // 檢查與其他 Funnel 上次位置的距離
            bool tooClose = false;
            foreach (var pair in _funnelLastPositions)
            {
                Transform otherFunnel = pair.Key;
                Vector3 otherPosition = pair.Value;

                // 跳過自己
                if (otherFunnel == currentFunnel)
                    continue;

                if (Vector3.Distance(position, otherPosition) < MinFunnelDistance)
                {
                    tooClose = true;
                    Debug.Log($"Position {position} is too close to funnel {otherFunnel.name}'s last position, trying again. Attempt: {attempt + 1}");
                    break;
                }
            }

            if (!tooClose)
            {
                // 更新這個 Funnel 的位置記錄
                _funnelLastPositions[currentFunnel] = position;
                return position;
            }
        }

        // 如果所有嘗試都失敗，回傳最後一次計算的位置
        Debug.LogWarning("Could not find suitable position after " + MAX_ATTEMPTS + " attempts");
        Vector2 lastScaledX = new Vector2(WorldXRange.x * scale, WorldXRange.y * scale);
        Vector2 lastScaledY = new Vector2(WorldYRange.x * scale, WorldYRange.y * scale);
        float lastX = Random.Range(lastScaledX.x, lastScaledX.y) + WorldCenterPoint.x;
        float lastY = Random.Range(lastScaledY.x, lastScaledY.y) + WorldCenterPoint.y;
        float lastZ = WorldCenterPoint.z + zOffset;
        Vector3 lastPosition = new Vector3(lastX, lastY, lastZ);
        _funnelLastPositions[currentFunnel] = lastPosition; // 即使是最後的嘗試位置也要記錄
        return lastPosition;
    }

    IEnumerator MoveToPositionFacingDirection(Transform funnel, Vector3 targetPosition)
    {
        while (Vector3.Distance(funnel.position, targetPosition) > 0.1f)
        {
            // 計算移動方向
            Vector3 moveDirection = (targetPosition - funnel.position).normalized;
            
            // 移動
            funnel.position = Vector3.MoveTowards(funnel.position, targetPosition, MovementSpeed * Time.deltaTime);
            
            // 如果有移動距離，則面向移動方向
            if (moveDirection != Vector3.zero)
            {
                Quaternion targetRotation = Quaternion.LookRotation(moveDirection);
                funnel.rotation = Quaternion.RotateTowards(funnel.rotation, targetRotation, RotationSpeed * Time.deltaTime);
            }
            
            yield return null;
        }
    }

    void StartSequentialMoveToStandBy()
    {
        _activeCoroutineCount = 0;

        // 開始移動協程
        for (int i = 0; i < Funnels.Count; i++)
        {
            var funnel = Funnels[i];
            if (funnel != null)
            {
                _activeCoroutineCount++;
                var coroutine = StartCoroutine(MoveToStandByCoroutine(funnel, i * 0.2f));
                _attackCoroutines.Add(coroutine);
            }
        }
    }

    // 計算角錐體頂點的位置
    private Vector3 CalculatePyramidApex()
    {
        // 使用相似三角形原理計算頂點位置
        // 從 +Z 平面到 0 平面的縮放比例延伸到頂點
        float plusScale = Mathf.Lerp(MinScale, MaxScale, (WorldZOffset + WorldZOffset) / (WorldZOffset * 2));
        float zeroScale = Mathf.Lerp(MinScale, MaxScale, (0 + WorldZOffset) / (WorldZOffset * 2));

        // 計算縮放率的變化
        float scaleChangePerUnit = (plusScale - zeroScale) / WorldZOffset;
        
        // 延伸到縮放為0的位置
        float distanceToApex = zeroScale / scaleChangePerUnit;
        
        // 計算頂點的Z位置（從0平面再往-Z延伸）
        float apexZ = WorldCenterPoint.z - distanceToApex;
        
        return new Vector3(WorldCenterPoint.x, WorldCenterPoint.y, apexZ);
    }

    IEnumerator MoveToStandByCoroutine(Transform funnel, float startDelay)
    {
        yield return new WaitForSeconds(startDelay);
        // 計算目標位置（世界座標）
        Vector3 targetPosition = GetRandomPositionOnPlane(WorldZOffset, funnel);
        
        funnel.transform.SetParent(null);
        // 移動到目標位置
        yield return StartCoroutine(MoveToPositionFacingDirection(funnel, targetPosition));

        // 計算角錐體頂點位置
        Vector3 apexPosition = CalculatePyramidApex();
        
        // 計算從當前位置指向頂點的方向
        Vector3 directionToApex = (apexPosition - funnel.position).normalized;
        Quaternion targetRotation = Quaternion.LookRotation(directionToApex);
        
        while (Quaternion.Angle(funnel.rotation, targetRotation) > 0.1f)
        {
            funnel.rotation = Quaternion.RotateTowards(funnel.rotation, targetRotation, RotationSpeed * Time.deltaTime);
            yield return null;
        }

        StartCoroutine(StandByRaycastCoroutine(funnel));
        _activeCoroutineCount--;
    }

    IEnumerator StandByRaycastCoroutine(Transform funnel)
    {
        _funnelLastShootTime[funnel] = 0f;

        while (Mode == FunnelMode.StandBy)
        {
            // 進行射線檢測
            RaycastHit hit;
            bool rayHit = Physics.Raycast(funnel.position, funnel.forward, out hit, StandByRaycastDistance);           
            if (rayHit && hit.collider.CompareTag("Player"))
            {
                float currentTime = Time.time;
                if (!_funnelLastShootTime.ContainsKey(funnel) || 
                    currentTime - _funnelLastShootTime[funnel] >= StandByShootCooldown)
                {
                    Debug.Log($"Funnel {funnel.name} 對 {hit.collider.name} 進行射擊");
                    if (BulletPrefab != null)
                    {
                        Instantiate(BulletPrefab, funnel.position, funnel.rotation);
                        _funnelLastShootTime[funnel] = currentTime;
                    }
                }
            }          
            yield return new WaitForSeconds(0.1f);
        }
    }

    void OnDestroy()
    {
        // 確保在物件銷毀時清理所有記錄
        _funnelLastPositions.Clear();
        foreach (var marker in _funnelAimMarkers.Values)
        {
            if (marker != null)
            {
                Destroy(marker);
            }
        }
        _funnelAimMarkers.Clear();
    }

    void OnDrawGizmos()
    {
        Vector3 center = WorldCenterPoint == Vector3.zero ? transform.position : WorldCenterPoint;

        // 計算三個平面的縮放係數
        float plusScale = Mathf.Lerp(MinScale, MaxScale, (WorldZOffset + WorldZOffset) / (WorldZOffset * 2)); // +Z平面
        float zeroScale = Mathf.Lerp(MinScale, MaxScale, (0 + WorldZOffset) / (WorldZOffset * 2));           // 0平面
        float minusScale = Mathf.Lerp(MinScale, MaxScale, (-WorldZOffset + WorldZOffset) / (WorldZOffset * 2)); // -Z平面

        // 計算每個平面的縮放後範圍
        Vector2 plusX = new Vector2(WorldXRange.x * plusScale, WorldXRange.y * plusScale);
        Vector2 plusY = new Vector2(WorldYRange.x * plusScale, WorldYRange.y * plusScale);

        Vector2 zeroX = new Vector2(WorldXRange.x * zeroScale, WorldXRange.y * zeroScale);
        Vector2 zeroY = new Vector2(WorldYRange.x * zeroScale, WorldYRange.y * zeroScale);

        Vector2 minusX = new Vector2(WorldXRange.x * minusScale, WorldXRange.y * minusScale);
        Vector2 minusY = new Vector2(WorldYRange.x * minusScale, WorldYRange.y * minusScale);

        // 計算各平面的頂點
        Vector3[] plusCorners = new Vector3[4];
        Vector3[] zeroCorners = new Vector3[4];
        Vector3[] minusCorners = new Vector3[4];

        // +Z平面（綠色）
        plusCorners[0] = new Vector3(center.x + plusX.x, center.y + plusY.x, center.z + WorldZOffset);
        plusCorners[1] = new Vector3(center.x + plusX.x, center.y + plusY.y, center.z + WorldZOffset);
        plusCorners[2] = new Vector3(center.x + plusX.y, center.y + plusY.y, center.z + WorldZOffset);
        plusCorners[3] = new Vector3(center.x + plusX.y, center.y + plusY.x, center.z + WorldZOffset);

        // 0平面（黃色）
        zeroCorners[0] = new Vector3(center.x + zeroX.x, center.y + zeroY.x, center.z);
        zeroCorners[1] = new Vector3(center.x + zeroX.x, center.y + zeroY.y, center.z);
        zeroCorners[2] = new Vector3(center.x + zeroX.y, center.y + zeroY.y, center.z);
        zeroCorners[3] = new Vector3(center.x + zeroX.y, center.y + zeroY.x, center.z);

        // -Z平面（紅色）
        minusCorners[0] = new Vector3(center.x + minusX.x, center.y + minusY.x, center.z - WorldZOffset);
        minusCorners[1] = new Vector3(center.x + minusX.x, center.y + minusY.y, center.z - WorldZOffset);
        minusCorners[2] = new Vector3(center.x + minusX.y, center.y + minusY.y, center.z - WorldZOffset);
        minusCorners[3] = new Vector3(center.x + minusX.y, center.y + minusY.x, center.z - WorldZOffset);

        // 畫出各平面
        // +Z平面（綠色）
        Gizmos.color = new Color(0, 1, 0, 0.3f); // 半透明綠色
        DrawPlane(plusCorners);

        // 0平面（黃色）
        Gizmos.color = new Color(1, 1, 0, 0.3f); // 半透明黃色
        DrawPlane(zeroCorners);

        // -Z平面（紅色）
        Gizmos.color = new Color(1, 0, 0, 0.3f); // 半透明紅色
        DrawPlane(minusCorners);

        // 畫出連接線
        Gizmos.color = new Color(1, 1, 1, 0.5f); // 半透明白色
        for (int i = 0; i < 4; i++)
        {
            Gizmos.DrawLine(plusCorners[i], zeroCorners[i]);
            Gizmos.DrawLine(zeroCorners[i], minusCorners[i]);
        }

        // 繪製角錐體頂點
        Vector3 apex = CalculatePyramidApex();
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(apex, 0.2f);

        // 從每個平面的角落畫線到頂點
        Gizmos.color = new Color(1, 1, 1, 0.5f);
        Vector3[] apexCorners = new Vector3[4];
        
        // 從+Z平面的角落畫線到頂點
        Vector2 scaledPlusX = new Vector2(WorldXRange.x * plusScale, WorldXRange.y * plusScale);
        Vector2 scaledPlusY = new Vector2(WorldYRange.x * plusScale, WorldYRange.y * plusScale);
        apexCorners[0] = new Vector3(center.x + scaledPlusX.x, center.y + scaledPlusY.x, center.z + WorldZOffset);
        apexCorners[1] = new Vector3(center.x + scaledPlusX.x, center.y + scaledPlusY.y, center.z + WorldZOffset);
        apexCorners[2] = new Vector3(center.x + scaledPlusX.y, center.y + scaledPlusY.y, center.z + WorldZOffset);
        apexCorners[3] = new Vector3(center.x + scaledPlusX.y, center.y + scaledPlusY.x, center.z + WorldZOffset);
        
        for (int i = 0; i < 4; i++)
        {
            Gizmos.DrawLine(apexCorners[i], apex);
        }
    }

    private void DrawPlane(Vector3[] corners)
    {
        // 畫出平面的邊框
        for (int i = 0; i < 4; i++)
        {
            Gizmos.DrawLine(corners[i], corners[(i + 1) % 4]);
        }

        // 畫出平面（使用線條形成網格）
        Gizmos.DrawLine(corners[0], corners[2]); // 對角線
        Gizmos.DrawLine(corners[1], corners[3]); // 對角線
    }
} 