using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// 玩家控制器 - 負責處理玩家的移動、射擊和機體控制。
/// 此版本不使用 Rigidbody，改為直接操作 Transform 來移動。
/// 按住左鍵可自動連射（IsPressed 連續偵測）。
/// </summary>
public class PlayerController : MonoBehaviour
{
    #region 欄位與屬性
    [Header("血量設定")]
    [SerializeField] private PlayerHealth playerHealth;
    public PlayerHealth PlayerHealth => playerHealth;

    [Header("移動設定")]
    [SerializeField] private float moveSpeed = 15f;       // 基礎移動速度（每秒位移量）
    [SerializeField] private float maxSpeed = 25f;        // 速度上限

    [Header("邊界設定")]
    [SerializeField] private float xBoundary = 20f;       // X 軸左右邊界
    [SerializeField] private float yBoundary = 15f;       // Y 軸上下邊界

    [Header("組件引用")]
    [SerializeField] private WeaponSystem weaponSystem;   // 武器系統
    [SerializeField] private Transform cameraTarget;      // 攝影機跟隨目標

    [Header("機體設定")]
    [SerializeField] private GameObject playerShip;       // 玩家操控的機體（含模型）

    [Header("輸入設定")]
    [SerializeField] private string moveActionName = "Player/Move";     // 移動動作名稱（Action 路徑）
    [SerializeField] private string attackActionName = "Player/Attack"; // 攻擊動作名稱（Action 路徑）
    [SerializeField] private PlayerInput playerInput;                     // PlayerInput（由 Inspector 指定）

    [Header("準心設定")]
    public Image crosshairImage;
    public Sprite normalCrosshairSprite;      // 無目標時的準心圖片
    public Sprite preciseTargetSprite;        // 精準判定（射線命中）的準心圖片
    public Sprite looseTargetSprite;          // 寬鬆判定（檢測盒命中）的準心圖片
    public Image targetCrosshairImage;        // 目標準心圖片
    [SerializeField] private float crosshairRayDistance = 100f; // 射線距離可在 Inspector 設定
    private bool isPreciseTarget = false;      // 是否為精準命中

    [Header("偵測線")]
    [SerializeField] private LineRenderer mainLineRenderer;  // 原本的LineRenderer改名
    [SerializeField] private LineRenderer boxLineRenderer1;  // 碰撞盒用
    [SerializeField] private LineRenderer boxLineRenderer2;  // 碰撞盒用
    [SerializeField] private LineRenderer boxLineRenderer3;  // 碰撞盒用
    [SerializeField] private LineRenderer targetLineRenderer; // 目標連接線
    [SerializeField] private bool showMainLine = true;      // 顯示主要瞄準線
    [SerializeField] private bool showBoxLines = true;      // 顯示碰撞盒線條
    [SerializeField] private bool showTargetLine = true;    // 顯示目標連接線
    [SerializeField] private float boxDetectionLength = 100f;  // 檢測盒長度
    [SerializeField] private float boxDetectionHeight = 0.2f;  // 檢測盒高度
    [SerializeField] private float boxDetectionForward = 100f; // 檢測盒前方延伸距離
    [SerializeField] private LayerMask detectionLayerMask = 1;  // 預設為 Default layer (layer 0)

    // ------- 私有狀態 -------
    private Vector2 moveInput;           // 移動輸入（-1 ~ 1）
    private Vector3 currentVelocity;     // 目前速度（世界座標）
    private Camera mainCamera;           // 主攝影機
    private InputAction moveAction;      // Move Action 參考
    private InputAction attackAction;    // Attack Action 參考
    private Transform currentTarget;     // 目前瞄準的目標
    
    // 全域射擊控制
    private static bool globalFireEnabled = true;
    public static bool GlobalFireEnabled => globalFireEnabled;

    [SerializeField] private GameObject crashEffect;
    
    public enum WeaponState { Normal, Disabled }
    private WeaponState weaponState = WeaponState.Normal;
    
    #endregion

    #region Unity 生命週期

    private void Awake()
    {
        mainCamera = Camera.main;

        // 自動取得 WeaponSystem
        if (weaponSystem == null)
            weaponSystem = GetComponent<WeaponSystem>();

        // 若未指定 CameraTarget，預設使用自身 Transform
        if (cameraTarget == null)
            cameraTarget = transform;

        InitializeInputSystem();
    }

    private void Start()
    {
        // 若未指定 PlayerShip，嘗試使用自身 GameObject
        if (playerShip == null)
            playerShip = gameObject;
        playerShip.tag = "Player";

        // 隱藏並鎖定滑鼠游標
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Confined; // 或 CursorLockMode.Locked
    }

    private void OnDestroy()
    {
        // 停用輸入 Action，避免記憶體洩漏
        moveAction?.Disable();
        attackAction?.Disable();
    }
    
    private void Update()
    {
        if (playerHealth.IsDead()){
            crashEffect.SetActive(true);
            return;
        }

        // ✅ 修復：若啟用全域射擊，並且狀態仍為 Disabled，自動恢復主武器
        if (globalFireEnabled && weaponState == WeaponState.Disabled)
        {
            weaponState = WeaponState.Normal;
        }

        HandleInput();     // 取得輸入
        HandleShooting();  // 處理射擊（可連射）
        HandleMovement();  // 直接位移
        ClampPosition();   // 限制邊界
        UpdateCrosshairAndRay();
    }

    private void UpdateCrosshairAndRay()
    {
        if (mainCamera == null || crosshairImage == null) return;

        // 讓準心跟隨滑鼠
        Vector2 mousePos = Mouse.current.position.ReadValue();
        crosshairImage.rectTransform.position = mousePos;

        // 如果射擊被禁用，直接使用普通準心並返回
        if (!globalFireEnabled)
        {
            if (normalCrosshairSprite != null)
            {
                crosshairImage.sprite = normalCrosshairSprite;
                crosshairImage.rectTransform.rotation = Quaternion.identity;
            }
            if (targetCrosshairImage != null)
            {
                targetCrosshairImage.gameObject.SetActive(false);
            }
            return;
        }

        // 重置目標狀態
        currentTarget = null;
        isPreciseTarget = false;

        // 射線偵測
        Ray ray = mainCamera.ScreenPointToRay(mousePos);
        RaycastHit[] hits = Physics.RaycastAll(ray, crosshairRayDistance);

        foreach (var hit in hits)
        {
            // 忽略 Player 與 Beam
            if (hit.collider.CompareTag("Player") || hit.collider.CompareTag("Beam")) continue;
            // 只偵測敵人
            if (!hit.collider.CompareTag("Enemy")) continue;
            
            currentTarget = hit.transform;
            isPreciseTarget = true;  // 射線直接命中為精準判定
            break;
        }

        // 如果射線沒有命中，檢查檢測盒
        if (currentTarget == null && playerShip != null)
        {
            // 計算 Ray 與玩家飛船 z 平面的交點
            float t = (playerShip.transform.position.z - ray.origin.z) / ray.direction.z;
            Vector3 intersection = ray.origin + ray.direction * t;

            // 計算方向向量
            Vector3 dir = (intersection - playerShip.transform.position).normalized;

            // 計算檢測盒參數
            Vector3 boxStart = playerShip.transform.position;
            Vector3 boxEnd = boxStart + dir * boxDetectionLength;
            Vector3 boxCenter = (boxStart + boxEnd) * 0.5f + ray.direction * (boxDetectionForward * 0.5f);
            
            // 計算檢測盒大小
            Vector3 boxSize = new Vector3(
                boxDetectionLength,  // 長度（X軸）
                boxDetectionHeight,  // 高度（Y軸）
                boxDetectionForward  // 深度（Z軸）
            );

            // 計算檢測盒旋轉
            float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            Quaternion boxRotation = Quaternion.LookRotation(ray.direction) * Quaternion.Euler(0, 0, angle);

            // 執行檢測
            Collider[] hitColliders = Physics.OverlapBox(
                boxCenter,
                boxSize * 0.5f,
                boxRotation,
                detectionLayerMask
            );

            // 在檢測盒中尋找最近的敵人
            if (hitColliders.Length > 0)
            {
                float nearestDistance = float.MaxValue;
                Transform nearestEnemy = null;

                foreach (var hitCollider in hitColliders)
                {
                    if (!hitCollider.CompareTag("Enemy")) continue;

                    float distance = Vector3.Distance(playerShip.transform.position, hitCollider.transform.position);
                    if (distance < nearestDistance)
                    {
                        nearestDistance = distance;
                        nearestEnemy = hitCollider.transform;
                    }
                }

                currentTarget = nearestEnemy;
                if (currentTarget != null)
                {
                    isPreciseTarget = false;  // 檢測盒命中為寬鬆判定
                }
            }
        }

        // 更新準心顯示
        if (targetCrosshairImage != null)
        {
            if (currentTarget != null)
            {
                // 將目標的世界座標轉換為螢幕座標
                Vector3 screenPos = mainCamera.WorldToScreenPoint(currentTarget.position);
                
                // 如果目標在攝影機前方才顯示準心
                if (screenPos.z > 0)
                {
                    // 根據判定類型切換準心圖片
                    if (isPreciseTarget && preciseTargetSprite != null)
                    {
                        crosshairImage.sprite = preciseTargetSprite;
                        // 精準瞄準時，Y軸朝上，且不顯示目標準心
                        crosshairImage.rectTransform.rotation = Quaternion.identity;
                        targetCrosshairImage.gameObject.SetActive(false);
                    }
                    else if (!isPreciseTarget && looseTargetSprite != null)
                    {
                        crosshairImage.sprite = looseTargetSprite;
                        // 寬鬆判定時，準心指向目標，且顯示目標準心
                        Vector2 direction = new Vector2(screenPos.x - mousePos.x, screenPos.y - mousePos.y);
                        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
                        crosshairImage.rectTransform.rotation = Quaternion.Euler(0, 0, angle - 90);
                        targetCrosshairImage.gameObject.SetActive(true);
                        targetCrosshairImage.rectTransform.position = new Vector2(screenPos.x, screenPos.y);
                    }
                }
                else
                {
                    // 切換回普通準心圖片
                    if (normalCrosshairSprite != null)
                        crosshairImage.sprite = normalCrosshairSprite;

                    targetCrosshairImage.gameObject.SetActive(false);
                    crosshairImage.rectTransform.rotation = Quaternion.identity;
                }
            }
            else
            {
                // 切換回普通準心圖片
                if (normalCrosshairSprite != null)
                    crosshairImage.sprite = normalCrosshairSprite;

                targetCrosshairImage.gameObject.SetActive(false);
                crosshairImage.rectTransform.rotation = Quaternion.identity;
            }
        }

        // 更新 LineRenderer 顯示
        UpdateLineRenderers(ray);
    }

    private void UpdateLineRenderers(Ray ray)
    {
        if (playerShip == null) return;

        // 畫出從 Ray 起點到玩家飛船的線（綠色）
        Debug.DrawLine(ray.origin, playerShip.transform.position, Color.green);

        // 計算 Ray 與玩家飛船 z 平面的交點
        float t = (playerShip.transform.position.z - ray.origin.z) / ray.direction.z;
        Vector3 intersection = ray.origin + ray.direction * t;

        // 計算方向向量
        Vector3 dir = (intersection - playerShip.transform.position).normalized;

        // 更新主要瞄準線
        if (mainLineRenderer != null)
        {
            mainLineRenderer.enabled = showMainLine;
            mainLineRenderer.gameObject.SetActive(showMainLine);
            if (showMainLine)
            {
                float length = Vector3.Distance(playerShip.transform.position, intersection);
                Vector3 extendedEnd = playerShip.transform.position + dir * length * 10f;

                mainLineRenderer.positionCount = 2;
                mainLineRenderer.SetPosition(0, playerShip.transform.position);
                mainLineRenderer.SetPosition(1, extendedEnd);
                mainLineRenderer.startColor = Color.yellow;
                mainLineRenderer.endColor = Color.yellow;
            }
        }

        // 更新檢測盒線條
        UpdateBoxLines(dir, ray);

        // 更新目標連接線
        if (targetLineRenderer != null)
        {
            targetLineRenderer.enabled = showTargetLine;
            targetLineRenderer.gameObject.SetActive(showTargetLine);
        }
    }

    private void UpdateBoxLines(Vector3 dir, Ray ray)
    {
        if (boxLineRenderer1 == null || boxLineRenderer2 == null || boxLineRenderer3 == null) return;

        boxLineRenderer1.enabled = showBoxLines;
        boxLineRenderer2.enabled = showBoxLines;
        boxLineRenderer3.enabled = showBoxLines;

        // 設置物件啟用狀態
        boxLineRenderer1.gameObject.SetActive(showBoxLines);
        boxLineRenderer2.gameObject.SetActive(showBoxLines);
        boxLineRenderer3.gameObject.SetActive(showBoxLines);

        if (showBoxLines)
        {
            Vector3 boxStart = playerShip.transform.position;
            Vector3 boxEnd = boxStart + dir * boxDetectionLength;
            Vector3 boxCenter = (boxStart + boxEnd) * 0.5f + ray.direction * (boxDetectionForward * 0.5f);
            
            Vector3 boxSize = new Vector3(
                boxDetectionLength,
                boxDetectionHeight,
                boxDetectionForward
            );

            float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            Quaternion boxRotation = Quaternion.LookRotation(ray.direction) * Quaternion.Euler(0, 0, angle);

            Vector3 up = boxRotation * Vector3.up;
            Vector3 right = boxRotation * Vector3.right;
            Vector3 halfSize = boxSize * 0.5f;

            Color lineColor = Color.green;
            boxLineRenderer1.startColor = boxLineRenderer1.endColor = lineColor;
            boxLineRenderer2.startColor = boxLineRenderer2.endColor = lineColor;
            boxLineRenderer3.startColor = boxLineRenderer3.endColor = lineColor;

            // 第一條線：中心軸
            boxLineRenderer1.positionCount = 2;
            boxLineRenderer1.SetPosition(0, boxCenter - right * halfSize.x);
            boxLineRenderer1.SetPosition(1, boxCenter + right * halfSize.x);

            // 第二條線：上邊
            boxLineRenderer2.positionCount = 2;
            boxLineRenderer2.SetPosition(0, boxCenter - right * halfSize.x + up * halfSize.y);
            boxLineRenderer2.SetPosition(1, boxCenter + right * halfSize.x + up * halfSize.y);

            // 第三條線：下邊
            boxLineRenderer3.positionCount = 2;
            boxLineRenderer3.SetPosition(0, boxCenter - right * halfSize.x - up * halfSize.y);
            boxLineRenderer3.SetPosition(1, boxCenter + right * halfSize.x - up * halfSize.y);
        }
    }

    #endregion

    #region 輸入與動作

    /// <summary>
    /// 初始化新的輸入系統，取出對應的 Action。
    /// </summary>
    private void InitializeInputSystem()
    {
        if (playerInput == null)
        {
            Debug.LogError("請在 Inspector 中設置 PlayerInput 組件！");
            return;
        }

        moveAction = playerInput.actions.FindAction(moveActionName);
        attackAction = playerInput.actions.FindAction(attackActionName);

        if (moveAction == null)
            Debug.LogError($"找不到移動動作：{moveActionName}");
        if (attackAction == null)
            Debug.LogError($"找不到攻擊動作：{attackActionName}");
    }

    /// <summary>
    /// 讀取玩家輸入的 Move 值。
    /// </summary>
    private void HandleInput()
    {
        if (moveAction != null)
            moveInput = moveAction.ReadValue<Vector2>();
    }

    /// <summary>
    /// 移動處理：不使用 Rigidbody，直接改變 Transform.position。
    /// </summary>
    private void HandleMovement()
    {
        // 直接計算移動速度
        currentVelocity = new Vector3(moveInput.x, moveInput.y, 0f) * moveSpeed;

        // 限制最大速度
        currentVelocity = Vector3.ClampMagnitude(currentVelocity, maxSpeed);

        // 位移
        if (playerShip != null)
            playerShip.transform.position += currentVelocity * Time.deltaTime;
    }

    /// <summary>
    /// 處理射擊邏輯：按住左鍵連射。
    /// </summary>
    private void HandleShooting()
    {
        // 狀態機控制主武器
        if (weaponState != WeaponState.Normal)
        {
            return;
        }
        if (!globalFireEnabled)
        {
            return;
        }
        if (attackAction != null && attackAction.IsPressed())
        {
            Transform pointerTarget = currentTarget;
            
            // 若沒有目標，建立一個臨時目標在射線終點
            if (pointerTarget == null)
            {
                Vector3 endPoint = mainCamera.ScreenPointToRay(Mouse.current.position.ReadValue()).origin + 
                                 mainCamera.ScreenPointToRay(Mouse.current.position.ReadValue()).direction * crosshairRayDistance;
                GameObject tempTarget = new GameObject("TempBulletTarget");
                tempTarget.transform.position = endPoint;
                pointerTarget = tempTarget.transform;
                // 可選：自動銷毀這個臨時物件
                Destroy(tempTarget, 2f);
            }
            
            weaponSystem.SetPointerTarget(pointerTarget);
            weaponSystem?.Fire();
        }
    }

    /// <summary>
    /// 限制玩家位置在指定邊界內。
    /// </summary>
    private void ClampPosition()
    {
        if (playerShip == null) return;

        Vector3 pos = playerShip.transform.position;
        pos.x = Mathf.Clamp(pos.x, -xBoundary, xBoundary);
        pos.y = Mathf.Clamp(pos.y, -yBoundary, yBoundary);
        playerShip.transform.position = pos;
    }

    #endregion

    #region 全域射擊控制

    /// <summary>
    /// 啟用或禁用全域射擊功能
    /// </summary>
    /// <param name="enabled">是否啟用射擊</param>
    public static void EnableFire(bool enabled)
    {
        globalFireEnabled = enabled;
        Debug.Log($"全域射擊功能已{(enabled ? "啟用" : "禁用")}");
    }

    /// <summary>
    /// 獲取當前全域射擊狀態
    /// </summary>
    /// <returns>是否啟用射擊</returns>
    public static bool IsFireEnabled()
    {
        return globalFireEnabled;
    }

    #endregion

    #region 公開方法

    public Vector2 GetMoveInput() => moveInput;
    public float   GetCurrentSpeed() => currentVelocity.magnitude;
    public Vector3 GetCurrentVelocity() => currentVelocity;

    public void SetBoundaries(float x, float y)
    {
        xBoundary = x;
        yBoundary = y;
    }

    public GameObject GetPlayerShip() => playerShip;

    public void SetMoveActionName(string actionName)
    {
        moveActionName = actionName;
        moveAction = playerInput?.actions.FindAction(moveActionName);
    }

    public void SetAttackActionName(string actionName)
    {
        attackActionName = actionName;
        attackAction = playerInput?.actions.FindAction(attackActionName);
    }

    public Transform GetCurrentTarget()
    {
        if (currentTarget != null)
            return currentTarget;

        // 沒有目標時，回傳滑鼠射線終點
        if (mainCamera != null)
        {
            Vector3 endPoint = mainCamera.ScreenPointToRay(Mouse.current.position.ReadValue()).origin +
                               mainCamera.ScreenPointToRay(Mouse.current.position.ReadValue()).direction * crosshairRayDistance;
            GameObject tempTarget = new GameObject("TempBulletTarget");
            tempTarget.transform.position = endPoint;
            Destroy(tempTarget, 2f);
            return tempTarget.transform;
        }
        return null;
    }

    #endregion
}
