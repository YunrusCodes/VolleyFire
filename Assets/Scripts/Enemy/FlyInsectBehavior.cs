using UnityEngine;

public class FlyInsectBehavior : EnemyBehavior
{
    public float circleRadius = 2f;
    public float angularSpeed = 2f;
    public float moveSpeed = 1.5f;
    public float xMin = -8f, xMax = 8f;
    public float yMin = -4f, yMax = 4f;
    public float directionChangeInterval = 1.5f;
    public float impactSpeed = 10f;
    public int impactDamage = 1;
    public float hoverTime = 5f;
    private EnemyController controller;
    private Vector2 moveDir;
    private float directionTimer;
    private Vector2 baseCenter;
    private Vector2 circleCenter;
    private float angle;
    private float hoverTimer;
    private bool isImpacting = false;
    private Transform playerTarget;
    private Vector3 impactTargetPosition;
    private bool isFleeing = false;
    private float fleeTimer = 0f;
    private Vector3 preImpactPosition;
    private bool isReturning = false;
    private bool isSwinging = false;
    private Vector2 swingDir;
    private Vector2 swingTarget;
    public float swingSpeed = 5f; // 可調整
    private bool isDead = false;
    private bool isRotatingBack = false;  // 是否正在轉回原始方向

    [Header("攻擊動畫設定")]
    public float attackPrepareTime = 1f;    // 攻擊準備時間
    public float rotationSpeed = 5f;        // 轉向速度
    private float attackPrepareTimer = 0f;   // 攻擊準備計時器
    private bool isPreparing = false;        // 是否正在準備攻擊

    public override void Init(EnemyController controller)
    {
        base.Init(controller);
        this.controller = controller;
        animator = GetComponent<Animator>();
        baseCenter = transform.position;
        // Swing 狀態初始化
        isSwinging = true;
        float randAngle = Random.Range(0f, Mathf.PI * 2f);
        swingDir = new Vector2(Mathf.Cos(randAngle), Mathf.Sin(randAngle)).normalized;
        swingTarget = baseCenter + swingDir * circleRadius;
        // 其餘初始化
        circleCenter = baseCenter;
        angle = randAngle;
        PickRandomDirection();
        directionTimer = directionChangeInterval;
        hoverTimer = hoverTime;
        isImpacting = false;
        isPreparing = false;
        attackPrepareTimer = 0f;
    }

    public override void Tick()
    {
        if (controller.GetHealth().IsDead())
        {
            OnHealthDeath();
            return;
        }

        // Swing 狀態
        if (isSwinging)
        {
            Vector2 currentPos = transform.position;
            Vector2 dir = (swingTarget - currentPos).normalized;
            float dist = Vector2.Distance(currentPos, swingTarget);
            float moveStep = swingSpeed * Time.deltaTime;
            if (moveStep >= dist)
            {
                transform.position = new Vector3(swingTarget.x, swingTarget.y, transform.position.z);
                isSwinging = false;
                // 設定盤旋起始角度，讓盤旋起點與swingDir一致
                circleCenter = baseCenter;
                angle = Mathf.Atan2(swingDir.y, swingDir.x);
            }
            else
            {
                Vector2 nextPos = currentPos + dir * moveStep;
                transform.position = new Vector3(nextPos.x, nextPos.y, transform.position.z);
            }
            return;
        }

        // 返回後的轉向狀態
        if (isRotatingBack)
        {
            HandleRotateBack();
            return;
        }

        if (isReturning)
        {
            Vector3 dir = (preImpactPosition - transform.position).normalized;
            float dist = Vector3.Distance(transform.position, preImpactPosition);
            float moveStep = impactSpeed * Time.deltaTime;
            if (moveStep >= dist)
            {
                transform.position = preImpactPosition;
                isReturning = false;
                isRotatingBack = true;  // 開始轉回-Z方向
            }
            else
            {
                transform.position += dir * moveStep;
            }
            return;
        }

        if (!isImpacting && !isPreparing)
        {
            HoverBehavior();
            hoverTimer -= Time.deltaTime;
            if (hoverTimer <= 0f)
            {
                playerTarget = GameObject.FindGameObjectWithTag("Player")?.transform;
                if (playerTarget != null)
                {
                    preImpactPosition = transform.position;
                    impactTargetPosition = playerTarget.position;
                    // 開始準備攻擊
                    StartAttackPreparation();
                }
                else
                {
                    hoverTimer = hoverTime; // 如果找不到玩家，重置計時器
                }
            }
            return;
        }

        // 處理攻擊準備階段
        if (isPreparing)
        {
            HandleAttackPreparation();
            return;
        }

        // Impacting 狀態
        if (isImpacting)
        {
            ImpactBehavior();
        }
    }

    private void HoverBehavior()
    {
        // 隨機換方向
        directionTimer -= Time.deltaTime;
        if (directionTimer <= 0f)
        {
            PickRandomDirection();
            directionTimer = directionChangeInterval;
        }
        // 盤旋中心也隨機平移，限制在以 baseCenter 為中心的邊界內
        circleCenter += moveDir * moveSpeed * Time.deltaTime;
        circleCenter.x = Mathf.Clamp(circleCenter.x, baseCenter.x + xMin, baseCenter.x + xMax);
        circleCenter.y = Mathf.Clamp(circleCenter.y, baseCenter.y + yMin, baseCenter.y + yMax);
        // 盤旋角度推進
        angle += angularSpeed * Time.deltaTime;
        // 計算盤旋位置
        float x = circleCenter.x + Mathf.Cos(angle) * circleRadius;
        float y = circleCenter.y + Mathf.Sin(angle) * circleRadius;
        Vector3 pos = transform.position;
        pos.x = x;
        pos.y = y;
        // 邊界反彈（針對盤旋位置）
        if (pos.x < baseCenter.x + xMin || pos.x > baseCenter.x + xMax)
        {
            moveDir.x = -moveDir.x;
            pos.x = Mathf.Clamp(pos.x, baseCenter.x + xMin, baseCenter.x + xMax);
            circleCenter.x = pos.x - Mathf.Cos(angle) * circleRadius;
        }
        if (pos.y < baseCenter.y + yMin || pos.y > baseCenter.y + yMax)
        {
            moveDir.y = -moveDir.y;
            pos.y = Mathf.Clamp(pos.y, baseCenter.y + yMin, baseCenter.y + yMax);
            circleCenter.y = pos.y - Mathf.Sin(angle) * circleRadius;
        }
        transform.position = pos;
    }

    private void ImpactBehavior()
    {
        // 只朝 impactTargetPosition 衝刺
        Vector3 dir = (impactTargetPosition - transform.position).normalized;
        float dist = Vector3.Distance(transform.position, impactTargetPosition);
        float moveStep = impactSpeed * Time.deltaTime;
        if (moveStep >= dist)
        {
            transform.position = impactTargetPosition;
            isImpacting = false;
            isReturning = true;
        }
        else
        {
            transform.position += dir * moveStep;
        }
    }

    private void PickRandomDirection()
    {
        float randAngle = Random.Range(0f, Mathf.PI * 2f);
        moveDir = new Vector2(Mathf.Cos(randAngle), Mathf.Sin(randAngle)).normalized;
    }

    private void StartAttackPreparation()
    {
        isPreparing = true;
        attackPrepareTimer = attackPrepareTime;
        animator.SetBool("Attack", true);
    }

    private void HandleAttackPreparation()
    {
        // 更新朝向
        if (playerTarget != null)
        {
            // 3D空間中看向目標
            Vector3 directionToPlayer = (playerTarget.position - transform.position).normalized;
            // 計算目標旋轉
            Quaternion targetRotation = Quaternion.LookRotation(directionToPlayer);
            // 使用 Lerp 平滑轉向
            transform.rotation = Quaternion.Lerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
            
            // 更新目標位置（以防玩家移動）
            impactTargetPosition = playerTarget.position;
        }

        attackPrepareTimer -= Time.deltaTime;
        if (attackPrepareTimer <= 0f)
        {
            // 檢查是否已經基本面向目標
            if (playerTarget != null)
            {
                Vector3 directionToPlayer = (playerTarget.position - transform.position).normalized;
                float angleToTarget = Vector3.Angle(transform.forward, directionToPlayer);
                
                // 如果還沒有基本面向目標（角度差大於5度），重置計時器
                if (angleToTarget > 5f)
                {
                    attackPrepareTimer = 0.1f; // 給予短暫的額外時間繼續轉向
                    return;
                }
            }

            // 準備完成，開始衝撞
            isPreparing = false;
            isImpacting = true;
        }
    }

    private void HandleRotateBack()
    {
        // 計算目標旋轉（面向-Z方向）
        Quaternion targetRotation = Quaternion.LookRotation(Vector3.back);
        // 使用 Lerp 平滑轉向
        transform.rotation = Quaternion.Lerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);

        // 檢查是否已經基本面向-Z
        float angleToTarget = Vector3.Angle(transform.forward, Vector3.back);
        if (angleToTarget <= 5f)
        {
            isRotatingBack = false;
            hoverTimer = hoverTime;
            animator.SetBool("Attack", false);
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (!isImpacting) return;
        if (collision.gameObject.CompareTag("Player"))
        {
            var health = collision.gameObject.GetComponent<IHealth>();
            if (health != null)
            {
                health.TakeDamage(impactDamage);
            }
            // 開始沿原路徑飛回 preImpactPosition
            isImpacting = false;
            isReturning = true;
        }
    }

    public override void OnWaveMove()
    {
        // 波次移動階段，可加特效或待機動畫
    }

    public override void OnWaveStart()
    {
        // 波次開始行動階段，可加初始化或特效
    }

} 