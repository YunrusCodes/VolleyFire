using UnityEngine;
using System.Collections;

public class DroneBehavior : EnemyBehavior
{
    [Header("盤旋參數")]
    public float circleRadius = 2f;
    public float angularSpeed = 2f;
    public float moveSpeed = 1.5f;
    public float xMin = -8f, xMax = 8f;
    public float yMin = -4f, yMax = 4f;
    public float directionChangeInterval = 1.5f;

    [Header("飛彈參數")]
    public GameObject missilePrefab;
    public float fireInterval = 2f;
    public float missileSpeed = 10f;

    private EnemyController controller;
    private Vector2 moveDir;
    private float directionTimer;
    private Vector2 baseCenter;
    private Vector2 circleCenter;
    private float angle;
    private float fireTimer;
    private bool isFiring = false;
    private bool isMovingToAlign = false;
    private Vector3 alignTargetPos;
    private float alignLerpSpeed = 5f; // 可在 Inspector 調整對齊速度

    private enum DroneState { Hover, Fire, Swing }
    private DroneState currentState = DroneState.Hover;
    private Vector3 swingTargetPos;
    private float swingLerpSpeed = 5f; // 可調整
    private System.Action onSwingComplete; // Swing結束後要做的事

    public override void Init(EnemyController controller)
    {
        this.controller = controller;
        baseCenter = transform.position;
        circleCenter = baseCenter;
        angle = Random.Range(0f, Mathf.PI * 2f);
        PickRandomDirection();
        directionTimer = directionChangeInterval;
        fireTimer = fireInterval;
    }

    public override void Tick()
    {
        if (controller.GetHealth().IsDead())
        {
            Destroy(gameObject);
            return;
        }

        if (currentState == DroneState.Swing)
        {
            SwingToTarget();
            return;
        }

        if (currentState == DroneState.Fire)
        {
            if (isMovingToAlign)
            {
                MoveToAlignWithPlayer();
            }
            // FireMissileBurst 會自動切換狀態
            return;
        }

        if (currentState == DroneState.Hover)
        {
            HoverBehavior();
            fireTimer -= Time.deltaTime;
            if (fireTimer <= 0f)
            {
                if (Random.value < 0.5f)
                {
                    // 50% 機率繼續 Hover
                    fireTimer = fireInterval;
                    // 進入 Swing，目標為隨機 hover 範圍內一點
                    swingTargetPos = GetRandomHoverPosition();
                    onSwingComplete = () => { currentState = DroneState.Hover; };
                    currentState = DroneState.Swing;
                }
                else
                {
                    // 50% 機率進入 Fire
                    var player = GameObject.FindGameObjectWithTag("Player");
                    if (player != null)
                    {
                        alignTargetPos = new Vector3(player.transform.position.x, player.transform.position.y, transform.position.z);
                        swingTargetPos = alignTargetPos;
                        onSwingComplete = () => { isMovingToAlign = true; currentState = DroneState.Fire; };
                        currentState = DroneState.Swing;
                    }
                    fireTimer = fireInterval;
                }
            }
        }
    }

    private void HoverBehavior()
    {
        // 隨機換方向（只影響 x 軸）
        directionTimer -= Time.deltaTime;
        if (directionTimer <= 0f)
        {
            moveDir = Random.value > 0.5f ? Vector2.right : Vector2.left;
            directionTimer = directionChangeInterval;
        }

        // 嘗試移動
        Vector2 currentPos2D = new Vector2(transform.position.x, transform.position.y);
        Vector2 nextPos2D = currentPos2D + moveDir * moveSpeed * Time.deltaTime;

        // 判斷是否超出範圍（以 baseCenter 為中心）
        float xOffset = nextPos2D.x - baseCenter.x;
        if (Mathf.Abs(xOffset) > circleRadius)
        {
            moveDir.x = -moveDir.x; // 反彈
            nextPos2D.x = Mathf.Clamp(nextPos2D.x, baseCenter.x - circleRadius, baseCenter.x + circleRadius);
        }

        // y 固定或微幅擺動
        nextPos2D.y = currentPos2D.y; // 或 baseCenter.y + Mathf.Sin(Time.time * angularSpeed) * 0.5f;

        // 實際移動
        transform.position = new Vector3(nextPos2D.x, nextPos2D.y, transform.position.z);
    }

    private void PickRandomDirection()
    {
        float randAngle = Random.Range(0f, Mathf.PI * 2f);
        moveDir = new Vector2(Mathf.Cos(randAngle), Mathf.Sin(randAngle)).normalized;
    }

    private void FireMissileAtPlayer()
    {
        var player = GameObject.FindGameObjectWithTag("Player");
        if (player == null || missilePrefab == null) return;

        Vector3 firePos = transform.position;
        Vector3 targetPos = player.transform.position;
        Vector3 dir = (targetPos - firePos).normalized;

        // 實例化飛彈
        GameObject missile = GameObject.Instantiate(missilePrefab, firePos, Quaternion.LookRotation(dir));
        // 設定方向與速度（假設 BulletBehavior 有 SetDirection/SetSpeed 方法）
        var bullet = missile.GetComponent<BulletBehavior>();
        if (bullet != null)
        {
            bullet.SetDirection(dir);
            bullet.SetSpeed(missileSpeed);
        }
    }

    private void MoveToAlignWithPlayer()
    {
        // 使用 Lerp 平滑移動
        transform.position = Vector3.Lerp(transform.position, alignTargetPos, alignLerpSpeed * Time.deltaTime);
        // 當距離足夠接近時，視為到達
        if (Vector3.Distance(transform.position, alignTargetPos) < 0.05f)
        {
            transform.position = alignTargetPos;
            isMovingToAlign = false;
            StartCoroutine(FireMissileBurst());
        }
    }

    private IEnumerator FireMissileBurst()
    {
        isFiring = true;
        int burstCount = Random.Range(1, 6);
        for (int i = 0; i < burstCount; i++)
        {
            FireMissileAtPlayer();
            yield return new WaitForSeconds(0.1f);
        }
        isFiring = false;

        // Fire 結束後隨機決定下個狀態
        if (Random.value < 0.5f)
        {
            // 50% 機率繼續 Fire
            var player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                alignTargetPos = new Vector3(player.transform.position.x, player.transform.position.y, transform.position.z);
                swingTargetPos = alignTargetPos;
                onSwingComplete = () => { isMovingToAlign = true; currentState = DroneState.Fire; };
                currentState = DroneState.Swing;
            }
        }
        else
        {
            // 50% 機率回到 Hover
            swingTargetPos = GetRandomHoverPosition();
            onSwingComplete = () => { currentState = DroneState.Hover; };
            currentState = DroneState.Swing;
        }
    }

    private void SwingToTarget()
    {
        transform.position = Vector3.Lerp(transform.position, swingTargetPos, swingLerpSpeed * Time.deltaTime);
        if (Vector3.Distance(transform.position, swingTargetPos) < 0.05f)
        {
            transform.position = swingTargetPos;
            currentState = DroneState.Hover; // 預設
            onSwingComplete?.Invoke();
            onSwingComplete = null;
        }
    }

    private Vector3 GetRandomHoverPosition()
    {
        float x = baseCenter.x + Random.Range(-circleRadius, circleRadius);
        float y = baseCenter.y; // 或加微幅擺動
        return new Vector3(x, y, transform.position.z);
    }
} 