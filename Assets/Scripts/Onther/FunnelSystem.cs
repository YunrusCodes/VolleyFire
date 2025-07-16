using System.Collections.Generic;
using UnityEngine;
using System.Collections;

public class FunnelSystem : MonoBehaviour
{
    public bool enableAction = false;
    public void SetEnableAction(bool value){
        Debug.Log("SetEnableAction: " + value);
        if(value == enableAction) return;
        enableAction = value;
        ApplyMode(FunnelMode.StandBy);
    }
    public void Attack(){
        Debug.Log("Attack");
        if(!enableAction || (Mode == FunnelMode.StandBy && _activeCoroutineCount > 0)) return;
        ApplyMode(FunnelMode.AttackPattern);
    }
    public void StandBy(){
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
    public float WorldLookAtOffset = 2.0f; // 注視平面Z軸偏移
    public Vector3 WorldCenterPoint; // 世界座標中心點

    public GameObject BulletPrefab;
    public GameObject AimMarkerPrefab;
    public float MovementSpeed = 5f;
    public float RotationSpeed = 180f;
    public Material RayMaterial; // 新增：射線的材質

    public float StandByRaycastDistance = 400f; // 射線檢測距離
    public float StandByShootCooldown = 1.5f; // 射擊冷卻時間
    public LayerMask RaycastLayer; // 射線檢測層級

    private FunnelMode _lastMode;
    private List<Coroutine> _attackCoroutines = new List<Coroutine>();
    private int _activeCoroutineCount;
    private Dictionary<Transform, float> _funnelLastShootTime = new Dictionary<Transform, float>();
    private Dictionary<Transform, GameObject> _funnelAimMarkers = new Dictionary<Transform, GameObject>(); // 新增：儲存每個Funnel的瞄準標記

    void Start()
    {
        _lastMode = Mode;
        RaycastLayer = LayerMask.GetMask("Default");
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

    void SetAllAtNegativeZ()
    {
        foreach (var funnel in Funnels)
        {
            if (funnel != null)
            {
                float x = Random.Range(WorldXRange.x, WorldXRange.y);
                float y = Random.Range(WorldYRange.x, WorldYRange.y);
                Vector3 local = new Vector3(x, y, -WorldZOffset);
                Vector3 world = transform.TransformPoint(local);
                funnel.position = world;
                funnel.rotation = transform.rotation;
            }
        }
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
        Vector3 masterZTarget = GetRandomPositionOnPlane(0);
        yield return StartCoroutine(MoveToPositionFacingDirection(funnel, masterZTarget));

        // 2. 移動到+Z平面，同時漸進轉向玩家
        Vector3 positiveZTarget = GetRandomPositionOnPlane(-WorldZOffset);
        yield return StartCoroutine(MoveToPositionWithRotationLerp(funnel, positiveZTarget, true));
        
        // 在+Z平面停留0.5秒
        yield return new WaitForSeconds(0.5f);
        
        // 3. 立即發射子彈
        if (BulletPrefab != null)
        {
            Instantiate(BulletPrefab, funnel.position, funnel.rotation);
        }

        // 4. 移動回Master.z平面，漸進轉向移動方向
        // masterZTarget = GetRandomPositionOnPlane(0);
        // yield return StartCoroutine(MoveToPositionWithRotationLerp(funnel, masterZTarget, false));

        // 5. 最後移動回-Z平面
        Vector3 negativeZTarget = GetRandomPositionOnPlane(-WorldZOffset);
        yield return StartCoroutine(MoveToPositionFacingDirection(funnel, negativeZTarget));

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

    Vector3 GetRandomPositionOnPlane(float zOffset)
    {
        float x = Random.Range(WorldXRange.x, WorldXRange.y) + WorldCenterPoint.x;
        float y = Random.Range(WorldYRange.x, WorldYRange.y) + WorldCenterPoint.y;
        float z = WorldCenterPoint.z + zOffset;
        return new Vector3(x, y, z);
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

    IEnumerator MoveToStandByCoroutine(Transform funnel, float startDelay)
    {
        yield return new WaitForSeconds(startDelay);
        // 計算目標位置（世界座標）
        float x = Random.Range(WorldXRange.x, WorldXRange.y) + WorldCenterPoint.x;
        float y = Random.Range(WorldYRange.x, WorldYRange.y) + WorldCenterPoint.y;
        float z = WorldCenterPoint.z + WorldZOffset;
        Vector3 targetPosition = new Vector3(x, y, z);
        
        funnel.transform.SetParent(null);
        // 移動到目標位置
        yield return StartCoroutine(MoveToPositionFacingDirection(funnel, targetPosition));

        // 轉向世界座標的-z方向
        Vector3 worldBackward = Vector3.back; // 世界座標的-z方向
        Quaternion targetRotation = Quaternion.LookRotation(worldBackward);
        
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
        GameObject aimMarker = null;
        if (AimMarkerPrefab != null)
        {
            aimMarker = Instantiate(AimMarkerPrefab);
            _funnelAimMarkers[funnel] = aimMarker;
            aimMarker.SetActive(false); // 初始時設為不可見
        }

        while (Mode == FunnelMode.StandBy)
        {
            // 進行射線檢測
            RaycastHit hit;
            bool rayHit = Physics.Raycast(funnel.position, funnel.forward, out hit, StandByRaycastDistance);

            if (rayHit)
            {
                // 如果射線擊中的不是注視平面，則隱藏瞄準標記
                if (hit.distance < WorldLookAtOffset)
                {
                    if (aimMarker != null)
                    {
                        aimMarker.SetActive(false);
                    }
                }
                else
                {
                    // 計算射線與注視平面的交點
                    Vector3 rayOrigin = funnel.position;
                    Vector3 rayDirection = funnel.forward;
                    float planeDistance = WorldCenterPoint.z + WorldLookAtOffset - rayOrigin.z;
                    
                    // 計算射線與平面的交點
                    float t = planeDistance / rayDirection.z;
                    Vector3 planeIntersection = rayOrigin + rayDirection * t;

                    // 限制交點在允許範圍內
                    planeIntersection.x = Mathf.Clamp(planeIntersection.x, 
                        WorldCenterPoint.x + WorldXRange.x, 
                        WorldCenterPoint.x + WorldXRange.y);
                    planeIntersection.y = Mathf.Clamp(planeIntersection.y, 
                        WorldCenterPoint.y + WorldYRange.x, 
                        WorldCenterPoint.y + WorldYRange.y);
                    planeIntersection.z = WorldCenterPoint.z + WorldLookAtOffset;

                    if (aimMarker != null)
                    {
                        aimMarker.SetActive(true);
                        aimMarker.transform.position = planeIntersection;
                        aimMarker.transform.rotation = Quaternion.LookRotation(-rayDirection);
                    }
                }

                if (hit.collider.CompareTag("Player"))
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
            }
            else
            {
                // 如果射線沒有擊中任何物體，顯示瞄準標記
                Vector3 rayOrigin = funnel.position;
                Vector3 rayDirection = funnel.forward;
                float planeDistance = WorldCenterPoint.z + WorldLookAtOffset - rayOrigin.z;
                
                float t = planeDistance / rayDirection.z;
                Vector3 planeIntersection = rayOrigin + rayDirection * t;

                planeIntersection.x = Mathf.Clamp(planeIntersection.x, 
                    WorldCenterPoint.x + WorldXRange.x, 
                    WorldCenterPoint.x + WorldXRange.y);
                planeIntersection.y = Mathf.Clamp(planeIntersection.y, 
                    WorldCenterPoint.y + WorldYRange.x, 
                    WorldCenterPoint.y + WorldYRange.y);
                planeIntersection.z = WorldCenterPoint.z + WorldLookAtOffset;

                if (aimMarker != null)
                {
                    aimMarker.SetActive(true);
                    aimMarker.transform.position = planeIntersection;
                    aimMarker.transform.rotation = Quaternion.LookRotation(-rayDirection);
                }
            }

            yield return new WaitForSeconds(0.1f);
        }

        if (aimMarker != null)
        {
            Destroy(aimMarker);
            _funnelAimMarkers.Remove(funnel);
        }
    }

    void OnDestroy()
    {
        // 確保在物件銷毀時清理所有瞄準標記
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

        // 計算世界座標的平面角點
        Vector3[] plusCorners = new Vector3[4];
        Vector3[] minusCorners = new Vector3[4];
        Vector3[] lookAtCorners = new Vector3[4];

        // +Z平面（綠色）
        plusCorners[0] = new Vector3(center.x + WorldXRange.x, center.y + WorldYRange.x, center.z + WorldZOffset);
        plusCorners[1] = new Vector3(center.x + WorldXRange.x, center.y + WorldYRange.y, center.z + WorldZOffset);
        plusCorners[2] = new Vector3(center.x + WorldXRange.y, center.y + WorldYRange.y, center.z + WorldZOffset);
        plusCorners[3] = new Vector3(center.x + WorldXRange.y, center.y + WorldYRange.x, center.z + WorldZOffset);

        // -Z平面（紅色）
        minusCorners[0] = new Vector3(center.x + WorldXRange.x, center.y + WorldYRange.x, center.z - WorldZOffset);
        minusCorners[1] = new Vector3(center.x + WorldXRange.x, center.y + WorldYRange.y, center.z - WorldZOffset);
        minusCorners[2] = new Vector3(center.x + WorldXRange.y, center.y + WorldYRange.y, center.z - WorldZOffset);
        minusCorners[3] = new Vector3(center.x + WorldXRange.y, center.y + WorldYRange.x, center.z - WorldZOffset);

        // 注視平面（藍色）
        lookAtCorners[0] = new Vector3(center.x + WorldXRange.x, center.y + WorldYRange.x, center.z + WorldLookAtOffset);
        lookAtCorners[1] = new Vector3(center.x + WorldXRange.x, center.y + WorldYRange.y, center.z + WorldLookAtOffset);
        lookAtCorners[2] = new Vector3(center.x + WorldXRange.y, center.y + WorldYRange.y, center.z + WorldLookAtOffset);
        lookAtCorners[3] = new Vector3(center.x + WorldXRange.y, center.y + WorldYRange.x, center.z + WorldLookAtOffset);

        // 畫出各平面
        Debug.DrawLine(plusCorners[0], plusCorners[1], Color.green);
        Debug.DrawLine(plusCorners[1], plusCorners[2], Color.green);
        Debug.DrawLine(plusCorners[2], plusCorners[3], Color.green);
        Debug.DrawLine(plusCorners[3], plusCorners[0], Color.green);

        Debug.DrawLine(minusCorners[0], minusCorners[1], Color.red);
        Debug.DrawLine(minusCorners[1], minusCorners[2], Color.red);
        Debug.DrawLine(minusCorners[2], minusCorners[3], Color.red);
        Debug.DrawLine(minusCorners[3], minusCorners[0], Color.red);

        Debug.DrawLine(lookAtCorners[0], lookAtCorners[1], Color.blue);
        Debug.DrawLine(lookAtCorners[1], lookAtCorners[2], Color.blue);
        Debug.DrawLine(lookAtCorners[2], lookAtCorners[3], Color.blue);
        Debug.DrawLine(lookAtCorners[3], lookAtCorners[0], Color.blue);
    }
} 