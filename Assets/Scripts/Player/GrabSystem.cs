using UnityEngine;
using UnityEngine.InputSystem;
using VolleyFire;
using System.Collections.Generic; // Added for Dictionary

/// <summary>
/// 抓取系統 - 負責處理物件的抓取與移動功能
/// 按下右鍵時，射線偵測 ControllAble 標籤的物件，並將其移動到 ControllPoint
/// </summary>
public class GrabSystem : MonoBehaviour
{
    [Header("抓取點設定")]
    [SerializeField] private Transform controllPoint;        // 抓取目標位置
    [SerializeField] private float grabDistance = 100f;      // 抓取射線距離
    [SerializeField] private LineRenderer lineRenderer;      // 連線渲染器
    [SerializeField] private float lineWidth = 0.1f;        // 連線寬度
    [SerializeField] private LineRenderer targetLineRenderer; // 目標指示線渲染器
    [SerializeField] private float targetLineLength = 200f;  // 目標指示線長度

    [Header("音效設定")]
    [SerializeField] private AudioSource audioSource;        // 音效來源
    [SerializeField] private AudioClip grabSound;           // 抓取音效
    [SerializeField] private AudioClip releaseSound;        // 釋放音效

    [Header("Input System 設定")]
    [SerializeField] private PlayerInput playerInput;        // PlayerInput 組件
    [SerializeField] private string grabActionName = "Player/Grab"; // 抓取動作名稱

    [Header("移動設定")]
    [SerializeField] private bool useSmoothMovement = true;  // 是否使用平滑移動
    [SerializeField] private float moveSpeed = 5f;           // 移動速度（平滑移動時使用）

    [Header("UI 設定")]
    [SerializeField] private GameObject grableIconPrefab;    // 可抓取物件的 UI 圖示預製體
    [SerializeField] private Transform uiCanvas;            // UI Canvas
    [SerializeField] private float iconOffset = 50f;        // 圖示偏移距離

    [Header("特效設定")]
    [SerializeField] private GameObject grabEffect;         // 抓取特效物件

    [Header("其他引用")]
    [SerializeField] private PlayerController playerController; // 玩家控制器引用

    // 私有變數
    private InputAction grabAction;      // 抓取動作參考
    private Camera mainCamera;           // 主攝影機
    private GameObject currentGrabbedObject; // 目前抓取的物件
    [SerializeField] private bool isGrabbing = false;     // 是否正在抓取
    private ControllableObject currentControllable; // 目前控制的物件
    private Dictionary<GameObject, GameObject> grableIcons = new Dictionary<GameObject, GameObject>(); // 可抓取物件及其圖示的對應
    private HashSet<GameObject> releasedObjects = new HashSet<GameObject>(); // 已釋放的物件列表

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

        if (uiCanvas == null)
        {
            Debug.LogWarning("請在 Inspector 中指定 UI Canvas！");
        }

        // 初始化 LineRenderer
        if (lineRenderer == null)
        {
            lineRenderer = gameObject.AddComponent<LineRenderer>();
        }

        // 初始化目標指示線 LineRenderer
        if (targetLineRenderer == null)
        {
            targetLineRenderer = gameObject.AddComponent<LineRenderer>();
        }

        // 初始化 AudioSource
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        InitializeLineRenderer();
        InitializeTargetLineRenderer();
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
        // 如果射擊被禁用，強制釋放物體
        if (!PlayerController.GlobalFireEnabled && isGrabbing)
        {
            ReleaseGrabbedObject();
            return;
        }

        HandleGrabInput();
        
        // 如果正在抓取且使用平滑移動，更新物件位置
        if (isGrabbing && useSmoothMovement && currentGrabbedObject != null)
        {
            UpdateGrabbedObjectPosition();
        }

        // 更新連線位置
        if (isGrabbing && currentGrabbedObject != null)
        {
            UpdateLinePosition();
            UpdateTargetLinePosition();
        }

        // 更新可抓取物件的 UI 圖示
        UpdateGrableIcons();
    }

    private void OnDestroy()
    {
        // 清理所有圖示
        foreach (var icon in grableIcons.Values)
        {
            if (icon != null)
            {
                Destroy(icon);
            }
        }
        grableIcons.Clear();
        releasedObjects.Clear();

        // 停用輸入 Action
        grabAction?.Disable();
    }

    private void InitializeInputSystem()
    {
        grabAction = playerInput.actions.FindAction(grabActionName);
        
        if (grabAction == null)
        {
            Debug.LogError($"找不到抓取動作：{grabActionName}");
        }
    }

    private void InitializeLineRenderer()
    {
        lineRenderer.positionCount = 2;
        lineRenderer.startWidth = lineWidth;
        lineRenderer.endWidth = lineWidth;
        lineRenderer.enabled = false;
    }

    private void InitializeTargetLineRenderer()
    {
        if (targetLineRenderer != null)
        {
            targetLineRenderer.positionCount = 2;
            targetLineRenderer.startWidth = lineWidth;
            targetLineRenderer.endWidth = lineWidth;
            targetLineRenderer.enabled = false;
        }
    }

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

    private void TryGrab()
    {
        // 檢查是否允許射擊，如果不允許則不能抓取
        if (!PlayerController.GlobalFireEnabled) return;
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

    private void GrabObject(GameObject objectToGrab)
    {
        // 移除被抓取物件的圖示
        if (grableIcons.TryGetValue(objectToGrab, out GameObject icon))
        {
            Destroy(icon);
        }

        // 啟用抓取特效
        if (grabEffect != null)
        {
            grabEffect.SetActive(true);
        }

        currentGrabbedObject = objectToGrab;
        isGrabbing = true;

        // 設定 ControllableObject 狀態
        currentControllable = objectToGrab.GetComponent<ControllableObject>();
        if (currentControllable != null)
        {
            currentControllable.SetControlState(ControllableObject.ControlState.Controlled);
        }

        // 立即設為 ControllPoint 的子物件
        currentGrabbedObject.transform.SetParent(controllPoint);

        if (!useSmoothMovement)
        {
            // 即時移動：直接設定到本地座標 (0,0,0)
            currentGrabbedObject.transform.localPosition = Vector3.zero;
        }

        // 啟用連線
        if (lineRenderer != null)
        {
            lineRenderer.enabled = true;
            UpdateLinePosition();
        }

        // 啟用目標指示線
        if (targetLineRenderer != null)
        {
            targetLineRenderer.enabled = true;
            UpdateTargetLinePosition();
        }

        // 播放抓取音效
        if (audioSource != null && grabSound != null)
        {
            audioSource.Stop();
            audioSource.clip = grabSound;
            audioSource.Play();
        }

        Debug.Log($"已抓取物件：{objectToGrab.name}");
    }

    private void ReleaseGrabbedObject()
    {
        if (currentGrabbedObject != null)
        {
            // 取得目標位置
            Transform target = null;
            if (playerController != null)
            {
                target = playerController.GetCurrentTarget();
            }

            // 計算釋放方向
            Vector3 releaseDirection;
            if (target != null)
            {
                releaseDirection = (target.position - currentGrabbedObject.transform.position).normalized;
            }
            else
            {
                releaseDirection = Vector3.forward; // 如果沒有目標，預設向前
            }

            // 設定 ControllableObject 狀態為釋放
            if (currentControllable != null)
            {
                currentControllable.SetControlState(ControllableObject.ControlState.Released);
                // 設定移動方向
                currentControllable.SetDirection(releaseDirection);
                currentControllable = null;
            }

            // 解除父子關係
            currentGrabbedObject.transform.SetParent(null);
            Debug.Log($"釋放物件：{currentGrabbedObject.name}");
            currentGrabbedObject = null;

            // 關閉連線
            if (lineRenderer != null)
            {
                lineRenderer.enabled = false;
            }

            // 關閉目標指示線
            if (targetLineRenderer != null)
            {
                targetLineRenderer.enabled = false;
            }

            // 處理音效
            if (audioSource != null)
            {
                audioSource.Stop();
                if (releaseSound != null)
                {
                    audioSource.clip = releaseSound;
                    audioSource.Play();
                }
            }

            // 關閉抓取特效
            if (grabEffect != null)
            {
                grabEffect.SetActive(false);
            }
        }
        isGrabbing = false;
    }

    private void UpdateGrabbedObjectPosition()
    {
        if (currentGrabbedObject == null || controllPoint == null) return;

        Vector3 targetLocalPosition = Vector3.zero; // 目標本地座標 (0,0,0)
        Vector3 currentLocalPosition = currentGrabbedObject.transform.localPosition;
        
        // 使用 Lerp 進行平滑移動到本地座標
        Vector3 newLocalPosition = Vector3.Lerp(currentLocalPosition, targetLocalPosition, moveSpeed * Time.deltaTime);
        currentGrabbedObject.transform.localPosition = newLocalPosition;
        
        // 當距離很小時，直接設定到目標位置
        if (Vector3.Distance(currentLocalPosition, targetLocalPosition) < 0.5f)
        {
            currentGrabbedObject.transform.localPosition = targetLocalPosition;
            
            // 停止抓取音效
            if (audioSource != null && audioSource.clip == grabSound)
            {
                audioSource.Stop();
            }
        }
    }

    private void UpdateLinePosition()
    {
        if (lineRenderer != null && controllPoint != null)
        {
            lineRenderer.SetPosition(0, controllPoint.position);
            lineRenderer.SetPosition(1, currentGrabbedObject.transform.position);
        }
    }

    private void UpdateTargetLinePosition()
    {
        if (targetLineRenderer != null && controllPoint != null && currentGrabbedObject != null && playerController != null)
        {
            Vector3 startPos = currentGrabbedObject.transform.position;
            Transform target = playerController.GetCurrentTarget();
            Vector3 endPos = target != null ? target.position : startPos + controllPoint.forward * targetLineLength;
            
            targetLineRenderer.SetPosition(0, startPos);
            targetLineRenderer.SetPosition(1, endPos);
        }
    }

    private void UpdateGrableIcons()
    {
        if (mainCamera == null || uiCanvas == null || grableIconPrefab == null) return;

        // 找出所有可抓取物件
        GameObject[] grableObjects = GameObject.FindGameObjectsWithTag("ControllAble");
        HashSet<GameObject> currentObjects = new HashSet<GameObject>();
        // 清理不再存在的物件的圖示
        List<GameObject> objectsToRemove = new List<GameObject>();
        foreach (var pair in grableIcons)
        {
            if (!currentObjects.Contains(pair.Key))
            {
                if (pair.Value != null)
                {
                    Destroy(pair.Value);
                }
                objectsToRemove.Add(pair.Key);
            }
        }

        foreach (var obj in objectsToRemove)
        {
            grableIcons.Remove(obj);
        }
        foreach (var grableObject in grableObjects)
        {
            // 如果物件正在被抓取，跳過
            if (grableObject == currentGrabbedObject) continue;
            if (grableObject.GetComponent<ControllableObject>().currentState != ControllableObject.ControlState.Uncontrolled) continue;

            currentObjects.Add(grableObject);

            // 將物件位置轉換為螢幕座標
            Vector3 screenPos = mainCamera.WorldToScreenPoint(grableObject.transform.position);

            // 如果物件在攝影機前方且在螢幕範圍內
            if (screenPos.z > 0 && IsInScreen(screenPos))
            {
                // 如果圖示不存在，創建新的
                if (!grableIcons.ContainsKey(grableObject))
                {
                    GameObject icon = Instantiate(grableIconPrefab, uiCanvas);
                    grableIcons.Add(grableObject, icon);
                }

                // 更新圖示位置
                grableIcons[grableObject].transform.position = screenPos;
                
                // 只有在沒有抓取物件時才顯示圖示
                grableIcons[grableObject].SetActive(!isGrabbing);
            }
        }


    }

    private bool IsInScreen(Vector3 screenPos)
    {
        return screenPos.x >= 0 && screenPos.x <= Screen.width &&
               screenPos.y >= 0 && screenPos.y <= Screen.height;
    }

    public void SetControllPoint(Transform newControllPoint)
    {
        controllPoint = newControllPoint;
    }

    public void SetSmoothMovement(bool useSmooth)
    {
        useSmoothMovement = useSmooth;
    }

    public void SetMoveSpeed(float speed)
    {
        moveSpeed = speed;
    }

    public bool IsGrabbing()
    {
        return isGrabbing;
    }

    public GameObject GetCurrentGrabbedObject()
    {
        return currentGrabbedObject;
    }
} 