using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEditor.SearchService;
using UnityEditor;
using VolleyFire.Funnel;

public class RobotBehavior : EnemyBehavior
{
    #region Enums & Constants
    public enum RobotMode { Idle = 0, GunMode = 1, SwordMode = 2 }
    public int MAX_BULLETS = 10;
    public float SummonFunnelHealth = 500f;
    #endregion

    #region Serialized Fields
    [Header("Mode Settings")]
    public RobotMode mode = RobotMode.GunMode;

    [Header("移動參數")]
    public float moveSpeed = 5f;
    public float moveDistance = 3f;
    public int targetPointCount = 3;
    public float turnSpeed = 5f;

    [Header("標記設置")]
    public Color markerColor = Color.red;
    public float markerSize = 0.3f;

    [Header("邊界設置")]
    [Tooltip("x: 上限, y: 下限")]
    public Vector2 boundaryX = new Vector2(8f, -8f);
    public Vector2 boundaryY = new Vector2(4f, -4f);

    [Header("射擊設置")]
    [SerializeField] private GameObject gunBulletPrefab;
    [SerializeField] private float fireRate = 1f;

    [Header("劍模式設置")]
    public Vector3 slashDistance = new Vector3(0f, 0f, 3f);
    public float swordMoveSpeed = 8f;
    public float returnSpeed = 10f;
    [SerializeField] private bool isReturning = false;
    [SerializeField] private Transform swordTransform;
    [SerializeField] private Transform gunTransform;
    [SerializeField] private Transform pistolTransform;
    [Header("巡邏點距離限制")]
    public float minTargetPointDistance = 2f;
    #endregion

    #region Private Fields
    private EnemyController controller;
    private float nextFireTime;
    private int bulletsFired = 0;
    private bool slashing = false;
    private bool drawshooting = false;
    private Vector3 initialPosition;
    private Quaternion initialRotation;
    private Transform playerTransform;
    private bool hasCalculatedSwordPosition = false;
    private Vector3 desiredSwordPosition;
    private bool hasSlashed = false;
    private RobotMode lastAttackMode = RobotMode.SwordMode;

    // 狀態標記
    public bool slashbool = false;
    public bool sheathbool = false;
    public bool drawshootbool = false;

    // 巡邏相關
    private List<Vector3> targetPoints = new List<Vector3>();
    private List<GameObject> targetMarkers = new List<GameObject>();
    private int currentTargetIndex = 0;
    private Vector3 moveDirection;
    private Vector3 targetDirection;
    private bool hasUpdatedMidway = false;
    private Vector3 swordStartPosition;
    private float swordStartToTargetDistance;
    private float postShootWaitTimer = 0f; // 新增：射擊後等待計時器
    private bool isPostShootWaiting = false; // 新增：是否正在等待
    #endregion

    public FunnelSystem funnelSystem;

    #region Initialization
    public override void Init(EnemyController controller)
    {
        this.controller = controller;
        initialPosition = transform.position;
        initialRotation = transform.rotation;
        SetupPlayerReferences();
        ResetState();
        GenerateTargetPoints();
    }

    private void SetupPlayerReferences()
    {
        playerTransform = GameObject.FindGameObjectWithTag("Player")?.transform;
        if (playerTransform == null)
        {
            Debug.LogWarning("找不到標記為 'Player' 的物件！");
        }
    }

    private void ResetState()
    {
        slashing = false;
        drawshooting = false;
        slashbool = false;
        sheathbool = false;
        drawshootbool = false;
        currentTargetIndex = 0;
        bulletsFired = 0;
        hasSlashed = false;
        ClearMarkers();
    }

    private void OnDestroy()
    {
        ClearMarkers();
    }

    private void ClearMarkers()
    {
        foreach (var marker in targetMarkers)
        {
            if (marker != null)
            {
                Destroy(marker);
            }
        }
        targetMarkers.Clear();
    }
    #endregion

    #region Movement System
    private void GenerateTargetPoints()
    {
        targetPoints.Clear();
        ClearMarkers();

        // 產生起點（機器人當前位置）
        Vector3 start = transform.position;
        targetPoints.Add(start);

        // 產生第一個目標點
        Vector3 next = GenerateNewPatrolPoint(start);
        targetPoints.Add(next);

        if (targetPoints.Count > 1)
        {
            targetDirection = (targetPoints[1] - targetPoints[0]).normalized;
            moveDirection = targetDirection;
        }
    }

    private Vector3 GenerateNewPatrolPoint(Vector3 from)
    {
        for (int i = 0; i < 30; i++) // 最多嘗試30次
        {
            float angle = Random.Range(0f, 360f);
            float rad = angle * Mathf.Deg2Rad;

            // 計算該方向下，from到邊界的最大距離
            float maxDist = float.MaxValue;
            // X方向
            if (Mathf.Cos(rad) > 0)
                maxDist = Mathf.Min(maxDist, (boundaryX.x - from.x) / Mathf.Cos(rad));
            else if (Mathf.Cos(rad) < 0)
                maxDist = Mathf.Min(maxDist, (boundaryX.y - from.x) / Mathf.Cos(rad));
            // Y方向
            if (Mathf.Sin(rad) > 0)
                maxDist = Mathf.Min(maxDist, (boundaryY.x - from.y) / Mathf.Sin(rad));
            else if (Mathf.Sin(rad) < 0)
                maxDist = Mathf.Min(maxDist, (boundaryY.y - from.y) / Mathf.Sin(rad));

            float minDist = minTargetPointDistance;
            if (maxDist < minDist) continue;
            float dist = Random.Range(minDist, maxDist);
            Vector3 offset = new Vector3(Mathf.Cos(rad), Mathf.Sin(rad), 0f) * dist;
            Vector3 candidate = from + offset;
            if (IsWithinBoundary(candidate))
                return candidate;
        }
        // 如果找不到，直接回傳原點
        return from;
    }

    private bool IsWithinBoundary(Vector3 position)
    {
        // 直接使用世界座標檢查邊界
        return position.x >= boundaryX.y && position.x <= boundaryX.x &&
               position.y >= boundaryY.y && position.y <= boundaryY.x;
    }

    #endregion

    #region Combat System
    private void FireBullet()
    {
        if (gunBulletPrefab != null && gunTransform != null)
        {
            Instantiate(gunBulletPrefab, gunTransform.position, gunTransform.rotation);
            // Debug繪製z軸正方向線條
            Debug.DrawLine(gunTransform.position, gunTransform.position + gunTransform.forward * 500f, Color.red, 1.0f);
            // EditorApplication.isPaused = true;
        }
    }

    private void TargetLock(Transform weaponTransform, Vector3? offset = null)
    {
        if (playerTransform == null || weaponTransform == null) return;
        Vector3 realOffset = offset ?? Vector3.zero;
        Vector3 lockPoint = playerTransform.position + realOffset;
        // Step 1: 槍口指向玩家的方向（世界座標）
        Vector3 toTarget = lockPoint - weaponTransform.position;
        if (toTarget == Vector3.zero) return;

        // Step 2: 指定一個你要「頭朝上的方向」 → 使用世界 Y 軸
        Vector3 stableUp = Vector3.up;

        // Step 3: 使用 LookRotation(forward, up) → 算出讓 weapon 對準玩家的旋轉
        Quaternion desiredWeaponRotation = Quaternion.LookRotation(toTarget.normalized, stableUp);

        // Step 4: 計算旋轉差 → 槍應該怎麼轉 → 套用到父物件（AI 機器人）
        Quaternion rotationDelta = desiredWeaponRotation * Quaternion.Inverse(weaponTransform.rotation);

        // Step 5: 更新 AI 本體（transform）的旋轉
        transform.rotation = Quaternion.Lerp( transform.rotation, desiredWeaponRotation, 0.25f);

        // --- Debug 用 ---
        Debug.DrawRay(weaponTransform.position, weaponTransform.forward * 5f, Color.red);   // 槍口方向
        Debug.DrawLine(weaponTransform.position, lockPoint, Color.green);    // 瞄準線
    }





    #endregion

    #region Mode Behaviors
    private void GunModeBehavior()
    {
        if (isPostShootWaiting)
        {
            postShootWaitTimer -= Time.deltaTime;
            if (postShootWaitTimer <= 0f)
            {
                isPostShootWaiting = false;
            }
            return; // 等待期間不移動不轉動
        }

        if (playerTransform != null && gunTransform != null)
        {
            // 射擊前，先以抵達點為基準做TargetLock
            Vector3 originalPosition = transform.position;
            transform.position = targetPoints[1];
            TargetLock(gunTransform, new Vector3(8,-3,1));
            transform.position = originalPosition;
        }

        if (targetPoints.Count < 2) return;
        Vector3 toTarget = targetPoints[1] - transform.position;
        float totalDistance = Vector3.Distance(targetPoints[0], targetPoints[1]);
        float fromStart = Vector3.Distance(transform.position, targetPoints[0]);

        // 抵達
        if (fromStart >= totalDistance * 0.99f)
        {
            transform.position = targetPoints[1];
            // 射擊
            if (bulletsFired < MAX_BULLETS && animator.GetCurrentAnimatorStateInfo(0).normalizedTime >= 1f)
            {
                FireBullet();
                bulletsFired++;
                isPostShootWaiting = true; // 新增：啟動等待
                postShootWaitTimer = 0.01f; // 新增：設定等待0.5秒
                if (bulletsFired >= MAX_BULLETS)
                {
                    sheathbool = true;
                    bulletsFired = 0;
                }
            }
            // 到達目標點，移除
            targetPoints.RemoveAt(0);
            // 以剩下的點為基準產生新點
            Vector3 last = targetPoints[0];
            Vector3 newPoint = GenerateNewPatrolPoint(last);
            targetPoints.Add(newPoint);
            // 更新方向
            targetDirection = (targetPoints[1] - targetPoints[0]).normalized;
            moveDirection = targetDirection;
            return;
        }

        targetDirection = (targetPoints[1] - targetPoints[0]).normalized;
        moveDirection = Vector3.Lerp(moveDirection, targetDirection, turnSpeed * Time.deltaTime);
        moveDirection.Normalize();
        // 用Lerp方式移動到目標點
        transform.position = Vector3.Lerp(transform.position, targetPoints[1], moveSpeed * Time.deltaTime);
    }

    private void HandleIdleMode()
    {
        AnimatorStateInfo IdleLayer = animator.GetCurrentAnimatorStateInfo(0);
        
        if (IdleLayer.IsName("Idle") && IdleLayer.normalizedTime >= 0.25f)
        {
            mode = lastAttackMode == RobotMode.SwordMode ? RobotMode.GunMode : RobotMode.SwordMode;
            lastAttackMode = mode;
            return;
        }

        if (playerTransform != null)
        {
            Vector3 directionToPlayer = playerTransform.position - transform.position;
            directionToPlayer.y = 0;

            if (directionToPlayer != Vector3.zero)
            {
                Quaternion targetRotation = Quaternion.LookRotation(directionToPlayer, Vector3.up);
                transform.rotation = Quaternion.Lerp(transform.rotation, targetRotation, turnSpeed * Time.deltaTime);
            }
        }

        ResetState();
    }

    private void HandleGunMode()
    {
        // 進入槍模式時如果沒有巡邏點就產生
        if (mode == RobotMode.GunMode && targetPoints.Count < 2)
        {
            GenerateTargetPoints();
        }
        if (sheathbool)
        {
            animator.SetBool("DrawingGun", false);
            sheathbool = false;
            mode = RobotMode.Idle;
            // 離開槍模式時清空巡邏點與標記
            targetPoints.Clear();
            foreach (var marker in targetMarkers)
            {
                if (marker != null) Destroy(marker);
            }
            targetMarkers.Clear();
        }
        else if (!animator.GetBool("DrawingGun"))
        {
            animator.SetBool("DrawingGun", true);
        }
    }

    private void HandleSwordMode()
    {
        AnimatorStateInfo SlashLayer = animator.GetCurrentAnimatorStateInfo(1);
        
        if (drawshootbool && !slashing)
        {
            animator.SetTrigger("DrawAndShoot");
            drawshootbool = false;
            drawshooting = true;
        }
        else if (drawshooting && SlashLayer.normalizedTime >= 1f)
        {
            drawshooting = false;
        }

        if (isReturning)
        {
            HandleReturning();
            return;
        }

        if (animator.GetCurrentAnimatorStateInfo(0).normalizedTime >= 1f)
        {
            HandleSwordAttack();
        }

        HandleSwordAnimationStates(SlashLayer);
    }

    private void HandleReturning()
    {
        Vector3 directionToStart = initialPosition - transform.position;
        directionToStart.y = 0;
        float distanceToStart = directionToStart.magnitude;

        TargetLock(pistolTransform);
        
        if (distanceToStart > 0.1f)
        {
            transform.position += directionToStart.normalized * returnSpeed * Time.deltaTime;
        }
        else if (!animator.GetCurrentAnimatorStateInfo(0).IsName("TakeSwordShoot"))
        {
            transform.position = new Vector3(initialPosition.x, transform.position.y, initialPosition.z);
            isReturning = false;
            animator.SetBool("DrawingSword", false);
            mode = RobotMode.Idle;
            ResetState();
        }
    }

    private void HandleSwordAttack()
    {
        if (playerTransform == null || isReturning || swordTransform == null) return;

        Vector3 targetPosition = playerTransform.position;

        if (!hasCalculatedSwordPosition)
        {
            desiredSwordPosition = targetPosition + slashDistance;
            hasCalculatedSwordPosition = true;
            hasUpdatedMidway = false;
            swordStartPosition = swordTransform.position;
            swordStartToTargetDistance = (desiredSwordPosition - swordStartPosition).magnitude;
        }

        Vector3 swordToTarget = desiredSwordPosition - swordTransform.position;
        float swordDistanceToTarget = swordToTarget.magnitude;
        Vector3 swordDirection = swordToTarget.normalized;

        if (!hasUpdatedMidway && swordStartToTargetDistance > 0f && swordDistanceToTarget <= swordStartToTargetDistance / 2f)
        {
            desiredSwordPosition = playerTransform.position + slashDistance;
            hasUpdatedMidway = true;
        }

        // 計算劍和機器人本體的相對位置偏移
        Vector3 swordOffset = swordTransform.position - transform.position;
        // 從劍的目標位置反推機器人本體應該在的位置
        Vector3 desiredRobotPosition = desiredSwordPosition - swordOffset;
        
        Vector3 directionToPlayer = targetPosition - transform.position;
        directionToPlayer.y = 0;
        
        if (directionToPlayer != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(directionToPlayer, Vector3.up);
            transform.rotation = Quaternion.Lerp(transform.rotation, targetRotation, turnSpeed * Time.deltaTime);
        }

        if (!slashing && !slashbool && !isReturning)
        {
            // 使用 Lerp 實現平滑移動
            transform.position = Vector3.Lerp(transform.position, desiredRobotPosition, swordMoveSpeed * Time.deltaTime);
        }

        if (swordDistanceToTarget <= 0.1f && !slashing)
        {
            if (!isReturning && !slashing) slashbool = true;
        }

        if (animator.GetCurrentAnimatorStateInfo(0).IsName("TakeSwordShoot"))
        {
            UpdatePistolRotation(targetPosition);
        }
    }

    private void UpdatePistolRotation(Vector3 targetPosition)
    {
        Vector3 localTargetPosition = transform.InverseTransformPoint(targetPosition);
        Vector3 localPistolPosition = transform.InverseTransformPoint(pistolTransform.position);
        Vector3 localDirectionToPlayer = localTargetPosition - localPistolPosition;

        float yaw = Mathf.Atan2(localDirectionToPlayer.x, localDirectionToPlayer.z) * Mathf.Rad2Deg;
        float pitch = -Mathf.Atan2(localDirectionToPlayer.y, new Vector2(localDirectionToPlayer.x, localDirectionToPlayer.z).magnitude) * Mathf.Rad2Deg;

        Quaternion targetRotation = transform.rotation * Quaternion.Euler(pitch, yaw, 0);
        pistolTransform.rotation = Quaternion.Lerp(pistolTransform.rotation, targetRotation, turnSpeed * Time.deltaTime);
    }

    private void HandleSwordAnimationStates(AnimatorStateInfo SlashLayer)
    {
        if (SlashLayer.IsName("Slash"))
        {
            slashing = true;
        }
        else if (slashing)
        {
            slashing = false;
            isReturning = true;
            hasCalculatedSwordPosition = false;
            drawshootbool = true;
            hasUpdatedMidway = false;
        }
        else if (slashbool && !drawshooting && !hasSlashed)
        {
            animator.SetTrigger("Slash");
            slashbool = false;
            hasSlashed = true;
        }
        else if (sheathbool)
        {
            animator.SetBool("DrawingSword", false);
            sheathbool = false;
            mode = RobotMode.Idle;
        }
        else if (!animator.GetBool("DrawingSword"))
        {
            animator.SetBool("DrawingSword", true);
        }
    }
    #endregion

    #region Main Update
    public override void Tick()
    {
        if (animator == null) return;
        if(controller.GetHealth().GetCurrentHealth() <= SummonFunnelHealth) funnelSystem.SetEnableAction(true);
        if(controller.GetHealth().IsDead())
        {
            OnHealthDeath();
            funnelSystem.SetEnableAction(false);
            return;
        }

        switch (mode)
        {
            case RobotMode.Idle:
                HandleIdleMode();
                break;
            case RobotMode.GunMode:
                HandleGunMode();
                GunModeBehavior();
                break;
            case RobotMode.SwordMode:
                HandleSwordMode();
                break;
        }
    }
    #endregion

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        // 畫出巡邏邊界
        Vector3 min = new Vector3(boundaryX.y, boundaryY.y, transform.position.z);
        Vector3 max = new Vector3(boundaryX.x, boundaryY.x, transform.position.z);
        Vector3 p1 = new Vector3(min.x, min.y, min.z);
        Vector3 p2 = new Vector3(max.x, min.y, min.z);
        Vector3 p3 = new Vector3(max.x, max.y, min.z);
        Vector3 p4 = new Vector3(min.x, max.y, min.z);
        Color color = Color.yellow;
        Debug.DrawLine(p1, p2, color);
        Debug.DrawLine(p2, p3, color);
        Debug.DrawLine(p3, p4, color);
        Debug.DrawLine(p4, p1, color);

        // 畫出三個巡邏點的正三角形
        if (targetPoints != null && targetPoints.Count == 2)
        {
            Debug.DrawLine(targetPoints[0], targetPoints[1], color);
        }

        // 畫出巡邏路徑
        if (targetPoints != null && targetPoints.Count >= 2)
        {
            for (int i = 0; i < targetPoints.Count - 1; i++)
            {
                Debug.DrawLine(targetPoints[i], targetPoints[i + 1], color);
            }
        }
        // 畫出劍模式移動路線
        if (mode == RobotMode.SwordMode && swordTransform != null && hasCalculatedSwordPosition)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(transform.position, desiredSwordPosition);
        }
    }
#endif
}

