using UnityEngine;

public class EnemyController : MonoBehaviour
{
    [Header("移動設定")]
    [SerializeField] private float moveSpeed = 8f;
    [SerializeField] private float rotationSpeed = 80f;
    [SerializeField] private Vector3 moveDirection = Vector3.back;
    [SerializeField] private float acceleration = 20f;
    [SerializeField] private float maxSpeed = 15f;
    
    [Header("3D移動模式")]
    [SerializeField] private MovementPattern movementPattern = MovementPattern.Straight;
    [SerializeField] private float patternRadius = 10f;
    [SerializeField] private float patternSpeed = 2f;
    
    [Header("邊界設定")]
    [SerializeField] private float destroyDistance = 50f;
    [SerializeField] private Transform playerTarget;
    
    [Header("武器設定")]
    [SerializeField] private bool canShoot = true;
    [SerializeField] private float shootInterval = 2f;
    [SerializeField] private WeaponSystem weaponSystem;
    
    [Header("AI設定")]
    [SerializeField] private float trackingRange = 20f;
    [SerializeField] private float trackingStrength = 0.5f;
    
    private Rigidbody rb;
    private float nextShootTime;
    private Vector3 currentVelocity;
    private Vector3 patternCenter;
    private float patternTime;
    
    public enum MovementPattern
    {
        Straight,
        Circle,
        SineWave,
        Spiral,
        ChasePlayer
    }
    
    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        
        if (weaponSystem == null)
            weaponSystem = GetComponent<WeaponSystem>();
            
        if (playerTarget == null)
            playerTarget = GameObject.FindGameObjectWithTag("Player")?.transform;
            
        patternCenter = transform.position;
        patternTime = 0f;
    }
    
    private void Start()
    {
        // 設置初始旋轉
        if (moveDirection != Vector3.zero)
        {
            transform.rotation = Quaternion.LookRotation(moveDirection);
        }
    }
    
    private void Update()
    {
        HandleMovement();
        HandleShooting();
        CheckBounds();
        UpdatePattern();
    }
    
    private void HandleMovement()
    {
        Vector3 targetVelocity = Vector3.zero;
        
        switch (movementPattern)
        {
            case MovementPattern.Straight:
                targetVelocity = moveDirection * moveSpeed;
                break;
                
            case MovementPattern.Circle:
                targetVelocity = CalculateCircleMovement();
                break;
                
            case MovementPattern.SineWave:
                targetVelocity = CalculateSineWaveMovement();
                break;
                
            case MovementPattern.Spiral:
                targetVelocity = CalculateSpiralMovement();
                break;
                
            case MovementPattern.ChasePlayer:
                targetVelocity = CalculateChaseMovement();
                break;
        }
        
        // 平滑加速
        currentVelocity = Vector3.MoveTowards(currentVelocity, targetVelocity, acceleration * Time.deltaTime);
        
        // 限制最大速度
        if (currentVelocity.magnitude > maxSpeed)
        {
            currentVelocity = currentVelocity.normalized * maxSpeed;
        }
        
        rb.linearVelocity = currentVelocity;
        
        // 更新旋轉
        if (currentVelocity.magnitude > 0.1f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(currentVelocity.normalized);
            transform.rotation = Quaternion.RotateTowards(
                transform.rotation, 
                targetRotation, 
                rotationSpeed * Time.deltaTime
            );
        }
    }
    
    private Vector3 CalculateCircleMovement()
    {
        float angle = patternTime * patternSpeed;
        Vector3 offset = new Vector3(
            Mathf.Cos(angle) * patternRadius,
            0,
            Mathf.Sin(angle) * patternRadius
        );
        
        Vector3 targetPos = patternCenter + offset;
        return (targetPos - transform.position).normalized * moveSpeed;
    }
    
    private Vector3 CalculateSineWaveMovement()
    {
        float x = Mathf.Sin(patternTime * patternSpeed) * patternRadius;
        Vector3 targetPos = patternCenter + new Vector3(x, 0, -patternTime * moveSpeed);
        
        return (targetPos - transform.position).normalized * moveSpeed;
    }
    
    private Vector3 CalculateSpiralMovement()
    {
        float angle = patternTime * patternSpeed;
        float radius = patternTime * 0.5f;
        
        Vector3 offset = new Vector3(
            Mathf.Cos(angle) * radius,
            0,
            Mathf.Sin(angle) * radius
        );
        
        Vector3 targetPos = patternCenter + offset;
        return (targetPos - transform.position).normalized * moveSpeed;
    }
    
    private Vector3 CalculateChaseMovement()
    {
        if (playerTarget == null)
            return moveDirection * moveSpeed;
            
        Vector3 directionToPlayer = (playerTarget.position - transform.position).normalized;
        float distanceToPlayer = Vector3.Distance(transform.position, playerTarget.position);
        
        if (distanceToPlayer <= trackingRange)
        {
            // 混合追蹤和原始方向
            Vector3 chaseDirection = Vector3.Lerp(moveDirection, directionToPlayer, trackingStrength);
            return chaseDirection * moveSpeed;
        }
        
        return moveDirection * moveSpeed;
    }
    
    private void UpdatePattern()
    {
        patternTime += Time.deltaTime;
    }
    
    private void HandleShooting()
    {
        if (!canShoot || weaponSystem == null)
            return;
        
        if (Time.time >= nextShootTime)
        {
            weaponSystem.Fire();
            nextShootTime = Time.time + shootInterval;
        }
    }
    
    private void CheckBounds()
    {
        // 檢查是否距離玩家太遠
        if (playerTarget != null)
        {
            float distance = Vector3.Distance(transform.position, playerTarget.position);
            if (distance > destroyDistance)
            {
                Destroy(gameObject);
                return;
            }
        }
        
        // 檢查是否移動到邊界外
        if (transform.position.magnitude > destroyDistance)
        {
            Destroy(gameObject);
        }
    }
    
    // 公開方法供外部調用
    public void SetMoveDirection(Vector3 direction)
    {
        moveDirection = direction.normalized;
    }
    
    public void SetMoveSpeed(float speed)
    {
        moveSpeed = speed;
    }
    
    public void SetCanShoot(bool canShoot)
    {
        this.canShoot = canShoot;
    }
    
    public void SetShootInterval(float interval)
    {
        shootInterval = interval;
    }
    
    public void SetMovementPattern(MovementPattern pattern)
    {
        movementPattern = pattern;
    }
    
    public void SetPatternRadius(float radius)
    {
        patternRadius = radius;
    }
    
    public void SetPatternSpeed(float speed)
    {
        patternSpeed = speed;
    }
    
    public void SetPlayerTarget(Transform target)
    {
        playerTarget = target;
    }
    
    public void SetTrackingRange(float range)
    {
        trackingRange = range;
    }
    
    public void SetTrackingStrength(float strength)
    {
        trackingStrength = Mathf.Clamp01(strength);
    }
    
    // 設置模式中心點
    public void SetPatternCenter(Vector3 center)
    {
        patternCenter = center;
    }
    
    // 重置模式時間
    public void ResetPatternTime()
    {
        patternTime = 0f;
    }
    
    // 獲取當前移動方向
    public Vector3 GetCurrentDirection()
    {
        return currentVelocity.normalized;
    }
    
    // 獲取當前速度
    public float GetCurrentSpeed()
    {
        return currentVelocity.magnitude;
    }
    
    // 簡單的AI行為方法
    public void MoveTowardsPlayer()
    {
        if (playerTarget == null) return;
        
        Vector3 direction = (playerTarget.position - transform.position).normalized;
        SetMoveDirection(direction);
        SetMovementPattern(MovementPattern.ChasePlayer);
    }
    
    public void MoveInCircle(Vector3 center, float radius)
    {
        SetPatternCenter(center);
        SetPatternRadius(radius);
        SetMovementPattern(MovementPattern.Circle);
    }
    
    public void MoveInSineWave(Vector3 startPos, float amplitude)
    {
        SetPatternCenter(startPos);
        SetPatternRadius(amplitude);
        SetMovementPattern(MovementPattern.SineWave);
    }
} 