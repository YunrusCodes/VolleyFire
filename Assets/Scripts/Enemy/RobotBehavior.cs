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
    private int bulletsFired = 0;                         // 已發射子彈數
    private const int MAX_BULLETS = 100;                   // 最大發射子彈數

    [Header("劍模式設置")]
    public Vector3 slashDistance = new Vector3(0f, 0f, 3f);    // 斬擊距離
    public float swordMoveSpeed = 8f;   // 劍模式移動速度
    public float returnSpeed = 10f;     // 返回原位速度
    [SerializeField] private bool isReturning = false;   // 是否正在返回原位
    [SerializeField] private Transform swordTransform;   // 劍的Transform

    // ──────────────────────────────────────────────────────────────
    private EnemyController controller;
    [SerializeField] private Animator animator;
    private bool slashing = false;
    private bool drawshooting = false;
    private Vector3 initialPosition;    // 儲存初始位置
    private Quaternion initialRotation; // 儲存初始旋轉
    private Transform playerTransform;  // 玩家的 Transform
    [SerializeField] private Transform gunTransform;  // 槍的 Transform
    private Renderer playerRenderer;    // 玩家的 Renderer 組件
    [SerializeField] private Transform pistolTransform; // 手槍的 Transform

    bool hasCalculatedSwordPosition = false;
    private Vector3 desiredSwordPosition;
    public bool slashbool = false;
    public bool sheathbool = false;
    public bool drawshootbool = false;

    private bool hasSlashed = false; // 只斬擊一次

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
        initialRotation = transform.rotation;  // 儲存初始旋轉
        playerTransform = GameObject.FindGameObjectWithTag("Player")?.transform;
        if (playerTransform != null)
        {
            playerRenderer = playerTransform.GetComponent<Renderer>();
            if (playerRenderer == null)
            {
                // 如果沒有找到 Renderer，嘗試在子物件中尋找
                playerRenderer = playerTransform.GetComponentInChildren<Renderer>();
            }
            if (playerRenderer == null)
            {
                Debug.LogWarning("找不到玩家的 Renderer 組件！");
            }
        }
        else
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
        bulletsFired = 0;  // 重置子彈計數
        hasSlashed = false; // 重置斬擊旗標

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

    private void GunModeBehavior()
    {
        // 更新朝向玩家
        if (playerTransform != null && gunTransform != null)
        {
            Vector3 targetPosition;
            targetPosition = playerTransform.position;
            Debug.Log($"瞄準位置(使用Transform): {targetPosition}");

            // 將目標位置轉換到機器人的本地空間
            Vector3 localTargetPosition = transform.InverseTransformPoint(targetPosition);
            Vector3 localGunPosition = transform.InverseTransformPoint(gunTransform.position);
            
            // 在本地空間計算從槍到目標的方向
            Vector3 localDirectionToPlayer = localTargetPosition - localGunPosition;
            
            Debug.Log($"本地空間 - 槍位置: {localGunPosition}, 目標位置: {localTargetPosition}, 方向: {localDirectionToPlayer.normalized}");

            // 計算需要的旋轉角度（在本地空間）
            float yaw = Mathf.Atan2(localDirectionToPlayer.x, localDirectionToPlayer.z) * Mathf.Rad2Deg;
            float pitch = -Mathf.Atan2(localDirectionToPlayer.y, new Vector2(localDirectionToPlayer.x, localDirectionToPlayer.z).magnitude) * Mathf.Rad2Deg;

            Debug.Log($"旋轉角度 - Pitch: {pitch:F2}°, Yaw: {yaw:F2}°");

            // 創建目標旋轉（只使用 pitch 和 yaw）
            Quaternion targetRotation = transform.rotation * Quaternion.Euler(pitch, yaw, 0);
            
            // 平滑轉向
            transform.rotation = Quaternion.Lerp(transform.rotation, targetRotation, turnSpeed * Time.deltaTime);
            Debug.Log($"Animator State Info: {animator.GetCurrentAnimatorStateInfo(0).normalizedTime}");
            // 檢查是否可以發射
            if (Time.time >= nextFireTime && bulletsFired < MAX_BULLETS && animator.GetCurrentAnimatorStateInfo(0).normalizedTime >= 1f)
            {
                FireBullet();
                nextFireTime = Time.time + 1f / fireRate;  // 設置下次發射時間
                bulletsFired++;
                Debug.Log($"已發射子彈數: {bulletsFired}/{MAX_BULLETS}");

                // 檢查是否達到最大發射數
                if (bulletsFired >= MAX_BULLETS)
                {
                    Debug.Log("達到最大發射數，切換回idle模式");
                    sheathbool = true;
                    bulletsFired = 0;  // 重置子彈計數
                }
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

    // ──────────────────────────────────────────────────────────────
    #region Mode Handlers
    private void HandleIdleMode()
    {
        AnimatorStateInfo IdleLayer = animator.GetCurrentAnimatorStateInfo(0);
        
        // 檢查是否在 Idle 狀態且動畫播放超過一半
        if (IdleLayer.IsName("Idle") && IdleLayer.normalizedTime >= 0.5f)
        {
            mode = RobotMode.SwordMode;
            Debug.Log("Idle動畫播放超過一半，切換到SwordMode");
            return;
        }

        if (playerTransform != null)
        {
            // 計算看向玩家的方向
            Vector3 targetPosition;
            targetPosition = playerTransform.position;

            // 計算方向，但只在水平面上（Y軸）旋轉
            Vector3 directionToPlayer = targetPosition - transform.position;
            directionToPlayer.y = 0; // 保持垂直方向不變

            if (directionToPlayer != Vector3.zero)
            {
                // 創建一個只在 Y 軸上的旋轉
                Quaternion targetRotation = Quaternion.LookRotation(directionToPlayer, Vector3.up);
                
                // 平滑轉向
                transform.rotation = Quaternion.Lerp(transform.rotation, targetRotation, turnSpeed * Time.deltaTime);
            }
        }

        ResetState();
    }

    private void HandleGunMode()
    {
        if (sheathbool)
        {
            animator.SetBool("DrawingGun", false);
            sheathbool = false;
            mode = RobotMode.Idle;
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

        // 如果正在返回原位
        if (isReturning)
        {
            // 計算到原始位置的方向和距離
            Vector3 directionToStart = initialPosition - transform.position;
            directionToStart.y = 0;  // 保持在同一平面
            float distanceToStart = directionToStart.magnitude;

            Debug.Log($"正在返回原位，距離原點: {distanceToStart:F2}");
            TargetLock();
            if (distanceToStart > 0.1f)
            {
                // 移動向原點，但不改變朝向
                transform.position += directionToStart.normalized * returnSpeed * Time.deltaTime;
            }
            else
            {
                // 到達原點
                transform.position = new Vector3(initialPosition.x, transform.position.y, initialPosition.z);
                isReturning = false;
                mode = RobotMode.Idle;  // 回到idle狀態
                Debug.Log("已返回原位，切換到Idle模式");
            }

            return;
        }

        // 檢查動畫狀態是否準備完成
        if (animator.GetCurrentAnimatorStateInfo(0).normalizedTime >= 1f)
        {
            if (playerTransform != null && !isReturning && swordTransform != null)
            {
                // 計算目標位置（玩家前方固定距離）
                Vector3 targetPosition = playerTransform.position;

                // 只在第一次進來時計算
                if (!hasCalculatedSwordPosition)
                {
                    desiredSwordPosition = new Vector3(targetPosition.x + slashDistance.x, targetPosition.y + slashDistance.y, targetPosition.z + slashDistance.z);
                    hasCalculatedSwordPosition = true;
                }

                // 計算劍到目標點的方向和距離
                Vector3 swordToTarget = desiredSwordPosition - swordTransform.position;
                float swordDistanceToTarget = swordToTarget.magnitude;
                Vector3 swordDirection = swordToTarget.normalized;

                // 持續打印距離和方向資訊
                Debug.Log($"劍的目標位置: {desiredSwordPosition}");
                Debug.Log($"劍當前位置: {swordTransform.position}, 距離: {swordDistanceToTarget:F2}");
                Debug.Log($"移動方向: {swordDirection}");

                // 始終讓機器人看向玩家
                Vector3 directionToPlayer = targetPosition - transform.position;
                directionToPlayer.y = 0; // 保持垂直方向不變
                if (directionToPlayer != Vector3.zero)
                {
                    Quaternion targetRotation = Quaternion.LookRotation(directionToPlayer, Vector3.up);
                    transform.rotation = Quaternion.Lerp(transform.rotation, targetRotation, turnSpeed * Time.deltaTime);
                }

                // 只有在不是斬擊狀態時才移動
                if (!slashing && !slashbool && !isReturning)
                {
                    // 根據劍到目標的方向移動機器人，但不改變朝向
                    transform.position += swordDirection * swordMoveSpeed * Time.deltaTime;
                }

                // 檢查劍是否到達目標位置
                if (swordDistanceToTarget <= 0.1f && !slashing)
                {
                    if(!isReturning && !slashing) slashbool = true;
                    Debug.Log($"劍到達目標位置！距離: {swordDistanceToTarget:F2}");
                }
            }


            if (animator.GetCurrentAnimatorStateInfo(0).IsName("TakeSwordShoot"))
            {
                Debug.Log("fixing");
                Vector3 targetPosition = playerTransform.position;

                // 計算本地空間的目標位置
                Vector3 localTargetPosition = transform.InverseTransformPoint(targetPosition);
                Vector3 localPistolPosition = transform.InverseTransformPoint(pistolTransform.position);

                Vector3 localDirectionToPlayer = localTargetPosition - localPistolPosition;

                float yaw = Mathf.Atan2(localDirectionToPlayer.x, localDirectionToPlayer.z) * Mathf.Rad2Deg;
                float pitch = -Mathf.Atan2(localDirectionToPlayer.y, new Vector2(localDirectionToPlayer.x, localDirectionToPlayer.z).magnitude) * Mathf.Rad2Deg;

                Quaternion targetRotation = transform.rotation * Quaternion.Euler(pitch, yaw, 0);

                pistolTransform.rotation = Quaternion.Lerp(pistolTransform.rotation, targetRotation, turnSpeed * Time.deltaTime);
            }
        }

        if (SlashLayer.IsName("Slash"))
        {
            slashing = true;
            Debug.Log("開始斬擊動作，停止移動");
        }
        else if (slashing)
        {
            slashing = false;
            isReturning = true; // 斬擊結束才開始返回
            hasCalculatedSwordPosition = false; // 斬擊結束後重設
            drawshootbool = true; // 開始返回時設為 true
            Debug.Log("斬擊結束，開始返回原位");
        }
        else if (slashbool && !drawshooting && !hasSlashed)
        {
            animator.SetTrigger("Slash");
            slashbool = false;
            hasSlashed = true; // 斬擊過後設為 true
            Debug.Log("觸發斬擊，等待動畫結束再返回");
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

    private void FireBullet()
    {
        if (gunBulletPrefab != null && gunTransform != null)
        {
            // 在槍口位置生成子彈
            GameObject bullet = Instantiate(gunBulletPrefab, gunTransform.position, gunTransform.rotation);
        }
    }
    void TargetLock()
    {
        Debug.Log("TargetLock");
        if (playerTransform == null || pistolTransform == null) { Debug.Log("PLAYER OR PISTOL IS NULL"); return; }
        Vector3 targetPosition = playerTransform.position;

        // 1. 計算手槍到玩家的世界方向
        Vector3 pistolToPlayer = targetPosition - pistolTransform.position;
        if (pistolToPlayer == Vector3.zero) return;

        // 2. 計算本體需要的旋轉，讓手槍 forward 指向玩家
        // 取得手槍在本體下的 local 方向
        Vector3 localPistolPos = transform.InverseTransformPoint(pistolTransform.position);
        Vector3 localTargetPos = transform.InverseTransformPoint(targetPosition);
        Vector3 localDir = (localTargetPos - localPistolPos).normalized;

        // 3. 用 LookRotation 產生本體的 local 旋轉
        Quaternion localTargetRot = Quaternion.LookRotation(localDir, Vector3.up);

        // 4. 轉回世界空間
        Quaternion worldTargetRot = transform.rotation * localTargetRot;

        // 5. 平滑旋轉本體
        transform.rotation = Quaternion.Lerp(transform.rotation, worldTargetRot, 0.25f);
    }
}
