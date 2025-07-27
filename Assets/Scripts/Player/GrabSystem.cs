using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 抓取系統 - 負責處理物件的抓取與移動功能
/// 按下右鍵時，射線偵測 ControllAble 標籤的物件，並將其移動到 ControllPoint
/// </summary>
public class GrabSystem : MonoBehaviour
{
    [Header("抓取點設定")]
    [SerializeField] private Transform controllPoint;        // 抓取目標位置
    [SerializeField] private float grabDistance = 100f;      // 抓取射線距離

    [Header("Input System 設定")]
    [SerializeField] private PlayerInput playerInput;        // PlayerInput 組件
    [SerializeField] private string grabActionName = "Player/Grab"; // 抓取動作名稱

    [Header("移動設定")]
    [SerializeField] private bool useSmoothMovement = true;  // 是否使用平滑移動
    [SerializeField] private float moveSpeed = 5f;           // 移動速度（平滑移動時使用）

    // 私有變數
    private InputAction grabAction;      // 抓取動作參考
    private Camera mainCamera;           // 主攝影機
    private GameObject currentGrabbedObject; // 目前抓取的物件
    private bool isGrabbing = false;     // 是否正在抓取
    private Vector3? releaseTargetPosition = null; // 釋放後的目標座標

    #region Unity 生命週期

    private void Awake()
    {
        mainCamera = Camera.main;
        
        // 檢查必要組件
        if (controllPoint == null)
        {
            Debug.LogWarning("請在 Inspector 中指定 ControllPoint！");
        }
        
        if (playerInput == null)
        {
            Debug.LogError("請在 Inspector 中指定 PlayerInput 組件！");
            return;
        }

        InitializeInputSystem();
    }

    private void OnEnable()
    {
        grabAction?.Enable();
    }

    private void OnDisable()
    {
        grabAction?.Disable();
    }

    private void Update()
    {
        HandleGrabInput();
        
        // 如果正在抓取且使用平滑移動，更新物件位置
        if (isGrabbing && useSmoothMovement && currentGrabbedObject != null)
        {
            UpdateGrabbedObjectPosition();
        }

        // 釋放後自動移動
        if (releaseTargetPosition.HasValue && currentGrabbedObject != null)
        {
            Vector3 target = releaseTargetPosition.Value;
            Vector3 current = currentGrabbedObject.transform.position;
            Vector3 newPos = Vector3.Lerp(current, target, moveSpeed * Time.deltaTime);
            currentGrabbedObject.transform.position = newPos;

            if (Vector3.Distance(current, target) < 0.05f)
            {
                currentGrabbedObject.transform.position = target;
                currentGrabbedObject = null;
                releaseTargetPosition = null;
            }
        }
    }

    private void OnDestroy()
    {
        grabAction?.Disable();
    }

    #endregion

    #region 輸入系統初始化

    /// <summary>
    /// 初始化輸入系統，取得抓取動作參考
    /// </summary>
    private void InitializeInputSystem()
    {
        grabAction = playerInput.actions.FindAction(grabActionName);
        
        if (grabAction == null)
        {
            Debug.LogError($"找不到抓取動作：{grabActionName}");
        }
    }

    #endregion

    #region 抓取處理

    /// <summary>
    /// 處理抓取輸入
    /// </summary>
    private void HandleGrabInput()
    {
        if (grabAction == null) return;

        // 按下右鍵時嘗試抓取
        if (grabAction.WasPressedThisFrame())
        {
            TryGrab();
        }
        
        // 放開右鍵時釋放物件
        if (grabAction.WasReleasedThisFrame())
        {
            ReleaseGrabbedObject();
        }
    }

    /// <summary>
    /// 嘗試抓取物件
    /// </summary>
    private void TryGrab()
    {
        if (controllPoint == null) return;

        Vector2 mousePos = Mouse.current.position.ReadValue();
        Ray ray = mainCamera.ScreenPointToRay(mousePos);

        if (Physics.Raycast(ray, out RaycastHit hit, grabDistance))
        {
            if (hit.collider.CompareTag("ControllAble"))
            {
                GrabObject(hit.collider.gameObject);
            }
        }
    }

    /// <summary>
    /// 抓取指定物件
    /// </summary>
    /// <param name="objectToGrab">要抓取的物件</param>
    private void GrabObject(GameObject objectToGrab)
    {
        currentGrabbedObject = objectToGrab;
        isGrabbing = true;

        // 立即設為 ControllPoint 的子物件
        currentGrabbedObject.transform.SetParent(controllPoint);

        if (!useSmoothMovement)
        {
            // 即時移動：直接設定到本地座標 (0,0,0)
            currentGrabbedObject.transform.localPosition = Vector3.zero;
        }
        // 如果使用平滑移動，讓 UpdateGrabbedObjectPosition 處理

        Debug.Log($"已抓取物件：{objectToGrab.name}");
    }

    /// <summary>
    /// 釋放目前抓取的物件
    /// </summary>
    private void ReleaseGrabbedObject()
    {
        if (currentGrabbedObject != null)
        {
            // 取得 PlayerController 的目標座標
            PlayerController playerController = FindObjectOfType<PlayerController>();
            if (playerController != null)
            {
                Transform target = playerController.GetCurrentTarget();
                if (target != null)
                {
                    releaseTargetPosition = target.position;
                }
                else
                {
                    releaseTargetPosition = null;
                }
            }
            else
            {
                releaseTargetPosition = null;
            }

            // 解除父子關係
            currentGrabbedObject.transform.SetParent(null);
            Debug.Log($"釋放物件：{currentGrabbedObject.name}");
        }
        isGrabbing = false;
    }

    /// <summary>
    /// 更新被抓取物件的位置（平滑移動到本地座標 0,0,0）
    /// </summary>
    private void UpdateGrabbedObjectPosition()
    {
        if (currentGrabbedObject == null || controllPoint == null) return;

        Vector3 targetLocalPosition = Vector3.zero; // 目標本地座標 (0,0,0)
        Vector3 currentLocalPosition = currentGrabbedObject.transform.localPosition;
        
        // 使用 Lerp 進行平滑移動到本地座標
        Vector3 newLocalPosition = Vector3.Lerp(currentLocalPosition, targetLocalPosition, moveSpeed * Time.deltaTime);
        currentGrabbedObject.transform.localPosition = newLocalPosition;
        
        // 當距離很小時，停止移動
        if (Vector3.Distance(currentLocalPosition, targetLocalPosition) < 0.01f)
        {
            currentGrabbedObject.transform.localPosition = targetLocalPosition;
            isGrabbing = false; // 停止抓取狀態
        }
    }

    #endregion

    #region 公開方法

    /// <summary>
    /// 設定抓取點
    /// </summary>
    /// <param name="newControllPoint">新的抓取點</param>
    public void SetControllPoint(Transform newControllPoint)
    {
        controllPoint = newControllPoint;
    }

    /// <summary>
    /// 設定是否使用平滑移動
    /// </summary>
    /// <param name="useSmooth">是否使用平滑移動</param>
    public void SetSmoothMovement(bool useSmooth)
    {
        useSmoothMovement = useSmooth;
    }

    /// <summary>
    /// 設定移動速度
    /// </summary>
    /// <param name="speed">移動速度</param>
    public void SetMoveSpeed(float speed)
    {
        moveSpeed = speed;
    }

    /// <summary>
    /// 取得目前是否正在抓取
    /// </summary>
    /// <returns>是否正在抓取</returns>
    public bool IsGrabbing()
    {
        return isGrabbing;
    }

    /// <summary>
    /// 取得目前抓取的物件
    /// </summary>
    /// <returns>目前抓取的物件，如果沒有則返回 null</returns>
    public GameObject GetCurrentGrabbedObject()
    {
        return currentGrabbedObject;
    }

    #endregion
} 