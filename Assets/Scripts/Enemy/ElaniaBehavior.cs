using UnityEngine;
using System.Collections.Generic;

public class ElaniaBehavior : EnemyBehavior
{
    [Header("移動參數")]
    public float moveSpeed = 5f;
    public float turnSpeed = 3f;
    [Tooltip("距離目標點多近開始減速")]
    public float slowdownDistance = 5f;
    [Tooltip("最低速度比例 (0-1)")]
    [Range(0, 1)]
    public float minSpeedRatio = 0.2f;
    [Tooltip("加農砲發射時的水平移動速度")]
    public float cannonSideMoveSpeed = 3f;
    private int horizontalMoveDirection = 1;  // 1 = 右, -1 = 左
    [Tooltip("x: 上限, y: 下限")]
    public Vector2 boundaryX = new Vector2(8f, -8f);
    public Vector2 boundaryY = new Vector2(4f, -4f);
    
    [Header("加農砲設置")]
    public GameObject cannonRayPrefab;
    public List<Transform> cannonRaySpawnPoints = new List<Transform>();
    public List<GameObject> cannonRayInstances = new List<GameObject>();
    public float cannonFireInterval = 2f;
    private float cannonFireTimer = 0f;
    [Header("攻擊設置")]
    public int shotCountBeforeMove = 3;  // 發射幾次後移動
    private int currentShotCount = 0;

    private EnemyController controller;
    private Vector3 targetPosition;
    private bool isMoving = true;
    private bool movingRight = true;  // 控制水平移動方向
    private bool hasReachedTarget = false;
    
    // 加農砲狀態追蹤
    private float cannonStateTimer = 0f;   // 狀態計時器
    public bool CHARGE = false;           // 充能狀態
    public bool ACTIVE = false;           // 作用狀態
    private bool lastCHARGE = false;      // 上一次的充能狀態
    private bool lastACTIVE = false;      // 上一次的作用狀態

    public override void Init(EnemyController controller)
    {
        this.controller = controller;
        // 初始化加農砲實例列表
        cannonRayInstances = new List<GameObject>(new GameObject[cannonRaySpawnPoints.Count]);
        // 初始化加農砲狀態
        cannonStateTimer = 0f;
        CHARGE = false;
        ACTIVE = false;
        lastCHARGE = false;
        lastACTIVE = false;
        
        // 初始化旋轉目標
        if (GameObject.FindGameObjectWithTag("Player") != null)
        {
            rotationTarget = GameObject.FindGameObjectWithTag("Player").transform.position;
        }
        else
        {
            rotationTarget = transform.position + transform.forward;
        }
        currentShotCount = 0;
        SetNewRandomTarget();
    }

    public override void Tick()
    {
        // 保存當前狀態
        lastCHARGE = CHARGE;
        lastACTIVE = ACTIVE;

        if (controller.GetHealth().IsDead())
        {
            foreach (var cannon in cannonRaySpawnPoints)
            {
                cannon.gameObject.SetActive(false);
            }
            OnHealthDeath();
            return;
        }

        HandleMovement();    // 處理移動
        HandleRotation();    // 處理旋轉
        HandleAttack();      // 處理攻擊
    }

    private void HandleMovement()
    {
        // 檢查狀態變化
        if (lastCHARGE && !CHARGE && !lastACTIVE && ACTIVE)
        {
            // 當 CHARGE 從 true 變成 false，且 ACTIVE 從 false 變成 true 時
            // 隨機決定移動方向
            horizontalMoveDirection = Random.value < 0.5f ? -1 : 1;
        }

        if (CHARGE)
        {
            // 充能時停止移動
            return;
        }
        else if (ACTIVE)
        {
            // 作用時水平移動
            Vector3 sideMovement = new Vector3(horizontalMoveDirection * cannonSideMoveSpeed * Time.deltaTime, 0, 0);
            transform.position += sideMovement;
        }
        else
        {
            // 非充能非作用時的移動
            if (!isMoving)
            {
                SetNewRandomTarget();
            }
            MoveToTarget();
        }
    }

    [Header("旋轉設置")]
    public float activeRotateSpeed = 1f;  // 作用時的旋轉速度
    private Vector3 rotationTarget;        // 儲存旋轉目標位置

    private void RotateTowards(Vector3 targetPosition, float rotateSpeed)
    {
        // 計算朝向目標的方向
        Vector3 direction = (targetPosition - transform.position).normalized;
        
        // 讓物件的 forward 朝向目標，up 使用世界的 Vector3.up
        Quaternion targetRotation = Quaternion.LookRotation(direction, Vector3.up);
        
        // 平滑旋轉
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotateSpeed * Time.deltaTime);
    }

    [Header("作用時的上升設置")]
    public float upwardMoveSpeed = 20f;  // 向上移動的速度

    private void HandleRotation()
    {
        if (CHARGE && !ACTIVE)
        {
            // 充能狀態時，更新目標為玩家位置
            rotationTarget = GameObject.FindGameObjectWithTag("Player").transform.position;
        }
        else if (!CHARGE && ACTIVE)
        {
            // 作用狀態時，目標點持續往上移動
            rotationTarget += Vector3.up *0.085f;
        }

        // 根據狀態使用不同的旋轉速度
        // float currentTurnSpeed = ACTIVE ? activeRotateSpeed : turnSpeed;
        
        // 使用儲存的目標位置進行旋轉
        RotateTowards(rotationTarget, activeRotateSpeed);
    }

    private void HandleAttack()
    {
        // 檢查是否有實例存在
        bool hasInstance = false;
        foreach (var ray in cannonRayInstances)
        {
            if (ray != null && ray.activeSelf)
            {
                hasInstance = true;
                break;
            }
        }

        // 沒有實例時，重置所有狀態
        if (!hasInstance)
        {
            CHARGE = false;
            ACTIVE = false;
            cannonStateTimer = 0f;

            // 當兩個狀態都是 false，且冷卻時間到時，創建新實例
            cannonFireTimer += Time.deltaTime;
            if (cannonFireTimer >= cannonFireInterval)
            {
                FireCannonRayAtPlayer();
                cannonFireTimer = 0f;
            }
            return;
        }

        // 有實例時的狀態處理
        if (!CHARGE && !ACTIVE)
        {
            // 新實例出現，進入充能狀態
            CHARGE = true;
            cannonStateTimer = 0f;
        }
        else if (CHARGE)
        {
            // 充能中，檢查時間
            cannonStateTimer += Time.deltaTime;
            var cannonRay = cannonRayPrefab.GetComponent<CannonRay>();
            if (cannonStateTimer >= cannonRay.GetChargingTime())
            {
                // 充能完成，進入作用狀態
                CHARGE = false;
                ACTIVE = true;
            }
        }
    }

    private void MoveToTarget()
    {
        // 計算到目標的距離
        float distance = Vector3.Distance(transform.position, targetPosition);
        
        if (distance > 0.1f)
        {
            // 計算速度係數（在減速距離內逐漸減速）
            float speedRatio = 1f;
            if (distance < slowdownDistance)
            {
                // 使用平滑的線性插值計算速度比例
                speedRatio = Mathf.Lerp(minSpeedRatio, 1f, distance / slowdownDistance);
                
                // 如果速度已經降到最低，直接找新的目標點
                if (Mathf.Approximately(speedRatio, minSpeedRatio))
                {
                    SetNewRandomTarget();
                    return;
                }
            }

            // 移動向目標（使用計算後的速度）
            Vector3 moveDirection = (targetPosition - transform.position).normalized;
            float currentSpeed = moveSpeed * speedRatio;
            transform.position += moveDirection * currentSpeed * Time.deltaTime;
        }
        else
        {
            // 到達目標點後，直接設置新的目標點
            SetNewRandomTarget();
        }
    }


    [Header("目標點設置")]
    public float minDistanceToNewTarget = 10f;  // 新目標點最小距離
    public int maxAttempts = 30;  // 最大嘗試次數

    private void SetNewRandomTarget()
    {
        Vector3 currentPos = transform.position;
        Vector3 newTarget = currentPos;
        float distance = 0f;
        int attempts = 0;

        // 嘗試找到符合距離要求的新目標點
        while (distance < minDistanceToNewTarget && attempts < maxAttempts)
        {
            float randomX = Random.Range(boundaryX.y, boundaryX.x);
            float randomY = Random.Range(boundaryY.y, boundaryY.x);
            newTarget = new Vector3(randomX, randomY, transform.position.z);
            
            distance = Vector3.Distance(currentPos, newTarget);
            attempts++;
        }

        // 如果找不到合適的點，就用最後一次生成的點
        targetPosition = newTarget;
        isMoving = true;
    }

    private void FireCannonRayAtPlayer()
    {
        if (cannonRayPrefab == null || cannonRaySpawnPoints.Count == 0) return;

        // 檢查是否可以發射新的加農砲
        bool canFire = true;
        foreach (var ray in cannonRayInstances)
        {
            if (ray != null && ray.activeSelf)
            {
                canFire = false;
                break;
            }
        }

        if (!canFire) return;  // 如果還有活躍的加農砲，不要發射新的

        // 確保 cannonRayInstances 長度正確
        while (cannonRayInstances.Count < cannonRaySpawnPoints.Count)
            cannonRayInstances.Add(null);

        // 發射加農砲
        for (int i = 0; i < cannonRaySpawnPoints.Count; i++)
        {
            Transform spawnPoint = cannonRaySpawnPoints[i];
            GameObject ray = Instantiate(cannonRayPrefab, spawnPoint.position, spawnPoint.rotation);
            cannonRayInstances[i] = ray;
            
            var bullet = ray.GetComponent<BulletBehavior>();
            if (bullet != null)
            {
                bullet.SetDirection(spawnPoint.forward);
            }
            
            var cannonRay = ray.GetComponent<CannonRay>();
            if (cannonRay != null)
            {
                cannonRay.SetSpawnPoint(spawnPoint);
                // 訂閱銷毀事件
                cannonRay.OnDestroyed += OnCannonRayDestroyed;
            }
        }


        cannonStateTimer = 0f;

        // 增加射擊計數
        currentShotCount++;
        
        // 如果達到指定的射擊次數，重新選擇目標點
        if (currentShotCount >= shotCountBeforeMove)
        {
            currentShotCount = 0;
            SetNewRandomTarget();
        }
    }

    private void OnCannonRayDestroyed(GameObject ray)
    {
        // 從列表中移除被銷毀的加農砲
        int index = cannonRayInstances.IndexOf(ray);
        if (index != -1)
        {
            cannonRayInstances[index] = null;
        }
    }

    private void OnDestroy()
    {
        // 清理所有事件訂閱
        foreach (var ray in cannonRayInstances)
        {
            if (ray != null)
            {
                var cannonRay = ray.GetComponent<CannonRay>();
                if (cannonRay != null)
                {
                    cannonRay.OnDestroyed -= OnCannonRayDestroyed;
                }
            }
        }
    }

    private void OnDrawGizmos()
    {
        // 畫出活動範圍邊界
        Vector3 min = new Vector3(boundaryX.y, boundaryY.y, transform.position.z);
        Vector3 max = new Vector3(boundaryX.x, boundaryY.x, transform.position.z);
        
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(new Vector3(min.x, min.y, min.z), new Vector3(max.x, min.y, min.z));
        Gizmos.DrawLine(new Vector3(max.x, min.y, min.z), new Vector3(max.x, max.y, min.z));
        Gizmos.DrawLine(new Vector3(max.x, max.y, min.z), new Vector3(min.x, max.y, min.z));
        Gizmos.DrawLine(new Vector3(min.x, max.y, min.z), new Vector3(min.x, min.y, min.z));
        
        // 畫出目標點
        if (!hasReachedTarget)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(targetPosition, 0.3f);
        }
    }
}
