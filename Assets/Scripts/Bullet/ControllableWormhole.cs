using UnityEngine;

public class ControllableWormhole : ControllableObject
{
    [Header("蟲洞設定")]
    [SerializeField] private float initialSpeed = 3f;       // 初始速度
    [SerializeField] private float acceleration = 0.5f;     // 加速度
    [SerializeField] private float maxSpeed = 15f;          // 最大速度
    [SerializeField] private float rotationSpeed = 90f;     // 轉向速度（度/秒）
    [SerializeField] private float trackingDelay = 0.5f;    // 開始追蹤前的延遲（秒）
    [SerializeField] private string[] targetTags = new string[] { "Enemy", "Wormhole" };  // 目標標籤

    private float currentSpeed;                             // 當前速度
    private float elapsedTime;                             // 已經過時間
    private Quaternion targetRotation;                      // 目標旋轉
    private bool isTracking = false;                        // 是否在追蹤中
    private Transform target;
    private bool reachedMaxSpeed = false;
    [SerializeField] private GameObject BlackBall;

    protected override void BehaviorOnStart()
    {
        base.BehaviorOnStart();
        // 設置初始狀態
        currentSpeed = initialSpeed;
        elapsedTime = 0f;
        isTracking = false;
        
        // 初始設定向上方向
        direction = Vector3.up;
        transform.rotation = Quaternion.LookRotation(direction);
        
        // 關閉 Rigidbody 控制（使用 Transform 移動以便更好控制）
        useRigidbody = false;
    }

    protected override void MoveUncontrolled()
    {
        elapsedTime += Time.deltaTime;
    }

    protected override void OnControlled()
    {
        base.OnControlled();
        
        BlackBall.SetActive(true);
        transform.rotation = Quaternion.LookRotation(Vector3.forward);
        if (useRigidbody && rb != null)
        {
            rb.linearVelocity = Vector3.zero;
        }
    }

    protected override void OnReleased()
    { 

    }

    protected override void MoveReleased()
    {
  
    }
}
