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

    [Header("移動設定")]
    [SerializeField] private float moveSpeed = 15f;       // 基礎移動速度（每秒位移量）
    [SerializeField] private float maxSpeed = 25f;        // 速度上限
    [SerializeField] private float acceleration = 50f;    // 加速度（單位：m/s²）
    [SerializeField] private float deceleration = 30f;    // 減速度（單位：m/s²）

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
    [SerializeField] private string lockActionName = "Player/Lock";     // 鎖定動作名稱（Action 路徑）
    [SerializeField] private PlayerInput playerInput;                     // PlayerInput（由 Inspector 指定）

    [Header("準心設定")]
    public Image crosshairImage;
    public Color crosshairNormalColor = Color.green;
    public Color crosshairTargetColor = Color.red;
    public Color crosshairLockColor = Color.blue;    // 新增：鎖定狀態的顏色

    [SerializeField] private float crosshairRayDistance = 100f; // 射線距離可在 Inspector 設定

    // ------- 私有狀態 -------
    private Vector2 moveInput;           // 移動輸入（-1 ~ 1）
    private Vector3 currentVelocity;     // 目前速度（世界座標）
    private Camera mainCamera;           // 主攝影機
    private InputAction moveAction;      // Move Action 參考
    private InputAction attackAction;    // Attack Action 參考
    private InputAction lockAction;      // Lock Action 參考
    private Vector3 targetPosition;       // 新增：目前準心指向的目標世界座標
    [SerializeField] private Transform lockedTarget;       // 鎖定的目標
    private bool isLocked;              // 是否處於鎖定狀態

    public GameObject FireTarget;
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
        lockAction?.Disable();
    }

    private void Update()
    {
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
        crosshairImage.rectTransform.position = Mouse.current.position.ReadValue();

        // 射線偵測
        Ray ray = mainCamera.ScreenPointToRay(Mouse.current.position.ReadValue());
        RaycastHit[] hits = Physics.RaycastAll(ray, crosshairRayDistance);

        bool hitTarget = false;
        float drawDistance = crosshairRayDistance;
        targetPosition = ray.origin + ray.direction * crosshairRayDistance; // 預設為極限距離

        // 處理鎖定功能
        if (lockAction != null)
        {
            // 當按下右鍵時
            if (lockAction.WasPressedThisFrame() && lockedTarget == null)
            {
                foreach (var hit in hits)
                {
                    if (hit.collider.gameObject == playerShip) continue;
                    lockedTarget = hit.transform;
                    isLocked = true;
                    break;
                }
            }
            // 當放開右鍵時
            else if (lockAction.WasReleasedThisFrame())
            {
                isLocked = false;
                lockedTarget = null;
            }
        }
        
        Transform targetFind = null;
        foreach (var hit in hits)
        {
            // 忽略自己
            if (hit.collider.gameObject == playerShip)
                continue;

            // 撞到其他物件（敵人、障礙物等）
            hitTarget = true;
            targetFind = hit.transform;      
            drawDistance = hit.distance;
            break;
        }

        // 根據鎖定狀態設定目標位置
        if (isLocked)
        {
            if (lockedTarget == null)
                lockedTarget = targetFind;
            
            // 計算射線與Z平面的交點
            float t = (lockedTarget.position.z - ray.origin.z) / ray.direction.z;
            targetPosition = ray.origin + ray.direction * t;
            Debug.Log(targetPosition.z);
        }
        else if (targetFind != null)
        {
            lockedTarget = null;
            targetPosition = targetFind.position;
        }

        if (hitTarget)
        {
            Debug.DrawRay(ray.origin, ray.direction * drawDistance, Color.red, 0f, false);
            // 根據鎖定狀態決定準心顏色
            crosshairImage.color = isLocked ? crosshairLockColor : crosshairTargetColor;
        }
        else
        {
            Debug.DrawRay(ray.origin, ray.direction * crosshairRayDistance, Color.green, 0f, false);
            // 根據鎖定狀態決定準心顏色
            crosshairImage.color = isLocked ? crosshairLockColor : crosshairNormalColor;
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
        lockAction = playerInput.actions.FindAction(lockActionName);

        if (moveAction == null)
            Debug.LogError($"找不到移動動作：{moveActionName}");
        if (attackAction == null)
            Debug.LogError($"找不到攻擊動作：{attackActionName}");
        if (lockAction == null)
            Debug.LogError($"找不到鎖定動作：{lockActionName}");
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
        // 1. 計算目標速度
        Vector3 targetVelocity = new Vector3(moveInput.x, moveInput.y, 0f) * moveSpeed;

        // 2. 加速 / 減速
        if (moveInput.sqrMagnitude > 0.01f)
        {
            currentVelocity = Vector3.MoveTowards(currentVelocity, targetVelocity, acceleration * Time.deltaTime);
        }
        else
        {
            currentVelocity = Vector3.MoveTowards(currentVelocity, Vector3.zero, deceleration * Time.deltaTime);
        }

        // 3. 限制最大速度
        currentVelocity = Vector3.ClampMagnitude(currentVelocity, maxSpeed);

        // 4. 位移
        if (playerShip != null)
            playerShip.transform.position += currentVelocity * Time.deltaTime;
    }

    /// <summary>
    /// 處理射擊邏輯：按住左鍵連射。
    /// </summary>
    private void HandleShooting()
    {
        if (attackAction != null && attackAction.IsPressed())
        {
            // 直接使用 targetPosition 作為射擊目標
            weaponSystem.SetPointerTarget(targetPosition);
            
            FireTarget.transform.position = targetPosition;
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

    public void SetLockActionName(string actionName)
    {
        lockActionName = actionName;
        lockAction = playerInput?.actions.FindAction(lockActionName);
    }

    #endregion
}
