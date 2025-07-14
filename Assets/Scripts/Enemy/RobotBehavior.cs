using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class RobotBehavior : EnemyBehavior
{
    public enum RobotMode { Idle = 0, GunMode = 1, SwordMode = 2 }

    [Header("Mode Settings")]
    public RobotMode mode = RobotMode.GunMode;

    [Header("移動參數")]
    public float moveSpeed = 5f;        // 移動速度
    public float moveDistance = 3f;     // 每次移動的距離
    public int targetPointCount = 3;    // 目標點數量
    public float turnSpeed = 5f;        // 轉向速度

    [Header("標記設置")]
    public Color markerColor = Color.red;  // 標記顏色
    public float markerSize = 0.3f;        // 標記大小

    [Header("邊界設置")]
    public float boundaryX = 8f;     // X軸邊界 (+-8)
    public float boundaryY = 4f;     // Y軸邊界 (+-4)

    [Header("射擊設置")]
    [SerializeField] private GameObject gunBulletPrefab;  // 子彈預製體
    [SerializeField] private float fireRate = 1f;         // 每秒發射次數
    private float nextFireTime;                           // 下次發射時間

    // ──────────────────────────────────────────────────────────────
    private EnemyController controller;
    [SerializeField] private Animator animator;
    private bool slashing = false;
    private bool drawshooting = false;
    private Vector3 initialPosition;    // 儲存初始位置
    private Transform playerTransform;  // 玩家的 Transform
    [SerializeField] private Transform gunTransform;  // 槍的 Transform

    public bool slashbool = false;
    public bool sheathbool = false;
    public bool drawshootbool = false;

    // 巡邏相關變數
    private List<Vector3> targetPoints = new List<Vector3>();
    private List<GameObject> targetMarkers = new List<GameObject>();
    private int currentTargetIndex = 0;
    private Vector3 moveDirection;       // 當前移動方向
    private Vector3 targetDirection;     // 目標移動方向

    // ──────────────────────────────────────────────────────────────
    #region Init
    public override void Init(EnemyController controller)
    {
        this.controller = controller;
        initialPosition = transform.position;  // 儲存初始位置
        playerTransform = GameObject.FindGameObjectWithTag("Player")?.transform;
        if (playerTransform == null)
        {
            Debug.LogWarning("找不到標記為 'Player' 的物件！");
        }
        ResetState();
        GenerateTargetPoints();
    }

    private void ResetState()
    {
        slashing = false;
        drawshooting = false;
        slashbool = false;
        sheathbool = false;
        drawshootbool = false;
        currentTargetIndex = 0;

        // 清除所有標記
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

    // ──────────────────────────────────────────────────────────────
    #region Movement
    private void GenerateTargetPoints()
    {
        // 清除現有的點和標記
        targetPoints.Clear();
        ClearMarkers();

        Vector3 currentPos = transform.position;
        int attempts = 0;
        const int MAX_ATTEMPTS = 20;  // 最大嘗試次數

        while (targetPoints.Count < targetPointCount && attempts < MAX_ATTEMPTS)
        {
            // 生成隨機角度（0-360度）
            float randomAngle = Random.Range(0f, 360f);
            float randomRadian = randomAngle * Mathf.Deg2Rad;
            
            // 使用三角函數計算方向向量
            Vector3 direction = new Vector3(
                Mathf.Cos(randomRadian),
                Mathf.Sin(randomRadian),
                0f
            ).normalized;

            // 計算目標位置
            Vector3 newTarget = currentPos + direction * moveDistance;

            // 檢查是否在邊界內
            if (IsWithinBoundary(newTarget))
            {
                targetPoints.Add(newTarget);
                CreateTargetMarker(newTarget, targetPoints.Count);
                Debug.Log($"生成目標點 {targetPoints.Count}: {newTarget}, 角度: {randomAngle}度");
                currentPos = newTarget; // 從新的點繼續生成
            }

            attempts++;
        }

        // 如果生成的點不足，就用最後一個有效點補足
        while (targetPoints.Count < targetPointCount && targetPoints.Count > 0)
        {
            Vector3 lastPoint = targetPoints[targetPoints.Count - 1];
            targetPoints.Add(lastPoint);
            CreateTargetMarker(lastPoint, targetPoints.Count);
        }

        // 設置初始移動方向
        if (targetPoints.Count > 0)
        {
            targetDirection = (targetPoints[0] - transform.position).normalized;
            moveDirection = targetDirection;
            Debug.Log($"開始移動到第一個目標點: {targetPoints[0]}");
        }
    }

    private void CreateTargetMarker(Vector3 position, int index)
    {
        // 創建一個 Cube
        GameObject marker = GameObject.CreatePrimitive(PrimitiveType.Cube);
        marker.transform.position = position;
        marker.transform.localScale = Vector3.one * markerSize;
        
        // 設置材質和顏色
        var renderer = marker.GetComponent<Renderer>();
        if (renderer != null)
        {
            renderer.material = new Material(Shader.Find("Standard"));
            renderer.material.color = markerColor;
        }

        // 添加編號
        marker.name = $"TargetMarker_{index}";
        targetMarkers.Add(marker);
    }

    private bool IsWithinBoundary(Vector3 position)
    {
        // 計算相對於初始位置的偏移
        Vector3 offset = position - initialPosition;
        return offset.x >= -boundaryX && offset.x <= boundaryX &&
               offset.y >= -boundaryY && offset.y <= boundaryY;
    }

    private void PatrolMove()
    {
        if (mode != RobotMode.GunMode || targetPoints.Count == 0) return;

        // 更新朝向玩家
        if (playerTransform != null && gunTransform != null)
        {
            // 計算從槍到玩家的方向
            Vector3 directionToPlayer = playerTransform.position - gunTransform.position;
            
            // 計算需要的旋轉角度
            float yaw = Mathf.Atan2(directionToPlayer.x, directionToPlayer.z) * Mathf.Rad2Deg;
            float pitch = -Mathf.Atan2(directionToPlayer.y, new Vector2(directionToPlayer.x, directionToPlayer.z).magnitude) * Mathf.Rad2Deg;

            // 創建目標旋轉（只使用 pitch 和 yaw）
            Quaternion targetRotation = Quaternion.Euler(pitch, yaw, 0);
            
            // 平滑轉向
            transform.rotation = Quaternion.Lerp(transform.rotation, targetRotation, turnSpeed * Time.deltaTime);

            // 檢查是否可以發射
            if (Time.time >= nextFireTime)
            {
                FireBullet();
                nextFireTime = Time.time + 1f / fireRate;  // 設置下次發射時間
            }
        }

        // 計算到目標點的距離
        Vector3 toTarget = targetPoints[currentTargetIndex] - transform.position;
        float distanceToTarget = toTarget.magnitude;

        // 如果距離很近，切換到下一個目標
        if (distanceToTarget < 0.1f)
        {
            currentTargetIndex = (currentTargetIndex + 1) % targetPoints.Count;

            // 如果已經走完所有點，重新生成新的目標點
            if (currentTargetIndex == 0)
            {
                GenerateTargetPoints();
                return;
            }

            toTarget = targetPoints[currentTargetIndex] - transform.position;
            Debug.Log($"切換到目標點 {currentTargetIndex + 1}: {targetPoints[currentTargetIndex]}");

            // 更新標記顏色
            UpdateMarkersColor();
        }

        // 更新目標方向（因為移動會改變到目標的方向）
        targetDirection = toTarget.normalized;

        // 平滑過渡到新的移動方向
        moveDirection = Vector3.Lerp(moveDirection, targetDirection, turnSpeed * Time.deltaTime);
        moveDirection.Normalize(); // 確保是單位向量

        // 直接移動（只更新位置）
        transform.position += moveDirection * moveSpeed * Time.deltaTime;
        Debug.Log($"移動中 => 目標: {targetPoints[currentTargetIndex]}, 距離: {distanceToTarget:F2}, 當前方向: {moveDirection}, 目標方向: {targetDirection}");
    }

    private void UpdateMarkersColor()
    {
        // 更新所有標記的顏色
        for (int i = 0; i < targetMarkers.Count; i++)
        {
            if (targetMarkers[i] != null)
            {
                var renderer = targetMarkers[i].GetComponent<Renderer>();
                if (renderer != null)
                {
                    // 當前目標點設為紅色，其他設為原始顏色
                    renderer.material.color = (i == currentTargetIndex) ? Color.red : markerColor;
                }
            }
        }
    }
    #endregion

    // ──────────────────────────────────────────────────────────────
    #region Tick
    public override void Tick()
    {
        if (animator == null) return;

        // 處理移動
        PatrolMove();

        switch (mode)
        {
            case RobotMode.Idle:
                HandleIdleMode();
                break;
            case RobotMode.GunMode:
                HandleGunMode();
                break;
            case RobotMode.SwordMode:
                HandleSwordMode();
                break;
        }
    }
    #endregion

    // ──────────────────────────────────────────────────────────────
    #region Mode Handlers
    private void HandleIdleMode()
    {
        AnimatorStateInfo IdleLayer = animator.GetCurrentAnimatorStateInfo(0);
        if (IdleLayer.IsName("Idle"))
        {
            ResetState();
        }
    }

    private void HandleGunMode()
    {
        if (sheathbool)
        {
            animator.SetBool("DrawingGun", false);
            sheathbool = false;
            mode = 0;
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
        else if (drawshooting)
        {
            if (SlashLayer.normalizedTime >= 1f)
            {
                drawshooting = false;
            }
        }

        if (SlashLayer.IsName("Slash"))
        {
            slashing = true;
        }
        else if (slashing)
        {
            slashing = false;
        }
        else if (slashbool && !drawshooting)
        {
            animator.SetTrigger("Slash");
            slashbool = false;
        }
        else if (sheathbool)
        {
            animator.SetBool("DrawingSword", false);
            sheathbool = false;
            mode = 0;
        }
        else if (!animator.GetBool("DrawingSword"))
        {
            animator.SetBool("DrawingSword", true);
        }
    }
    #endregion

    private void FireBullet()
    {
        if (gunBulletPrefab != null && gunTransform != null)
        {
            // 在槍口位置生成子彈
            GameObject bullet = Instantiate(gunBulletPrefab, gunTransform.position, gunTransform.rotation);
        }
    }
}
