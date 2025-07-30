using UnityEngine;
using System.Collections.Generic;

public class FederalAircraft : EnemyBehavior
{
    // 靜態變數，用於確保每幀只執行一次移動
    private static bool hasMovedThisFrame = false;
    private static int frameCount = -1;

    [Header("邊界設定")]
    public Vector2 xRange = new Vector2(-10f, 10f);
    public Vector2 yRange = new Vector2(-10f, 10f);

    [Header("子彈Prefab")]
    public GameObject bulletPrefab;
    public float fireInterval = 1.5f;
    public float bulletSpeed = 10f;
    private float fireTimer = 0f;
    private EnemyController controller;
    
    [Header("環繞設定")]
    [Header("環繞半徑 r")]
    public float r = 5f;
    [Header("父物件索引值(僅顯示)")]
    public int siblingIndex;
    [Header("Lerp移動速度")]
    public float lerpSpeed = 5f;
    [Header("xy偏移值")]
    public float xOffset = 0f;
    public float yOffset = 0f;

    [Header("模式設定")]
    [Tooltip("是否啟用縮放半徑模式")]
    public bool shrinkRadiusMode = false;
    [Tooltip("是否啟用旋轉模式")]
    public bool rotationMode = true;
    [Tooltip("是否啟用平移模式")]
    public bool translationMode = false;
    [Tooltip("是否以圓形方式初始化位置")]
    public bool circularInitMode = true;

    [Header("縮放設定")]
    public float minRadius = 2f;
    private bool shrinking = true;
    private float originalRadius;

    [Header("旋轉設定")]
    public float rotationSpeed = 1f;
    private float baseAngle;
    private float currentAngle;
    private float angleUpdateTimer = 0f;
    private bool reachedMaxRadius = false;

    [Header("父物件移動設定")]
    public float parentMoveSpeed = 2f;
    public float moveInterval = 2f;
    private float moveTimer = 0f;
    private List<Vector2> remainingDirections;
    private Vector2 currentMoveDirection;

    private void InitializeDirections()
    {
        remainingDirections = new List<Vector2>
        {
            Vector2.up, Vector2.up,
            Vector2.down, Vector2.down,
            Vector2.left, Vector2.left,
            Vector2.right, Vector2.right
        };
    }

    private Vector2 GetRandomDirection()
    {
        if (remainingDirections.Count == 0)
        {
            InitializeDirections();
        }
        int randomIndex = Random.Range(0, remainingDirections.Count);
        Vector2 direction = remainingDirections[randomIndex];
        remainingDirections.RemoveAt(randomIndex);
        return direction;
    }

    public override void Init(EnemyController controller)
    {
        this.controller = controller;
        fireTimer = 0f;
        if (transform.parent == null) return;
        int siblingCount = transform.parent.childCount;
        if (siblingCount == 0) return;
        siblingIndex = transform.GetSiblingIndex();

        if (circularInitMode)
        {
            baseAngle = 2 * Mathf.PI * siblingIndex / siblingCount;
            currentAngle = baseAngle;
            originalRadius = r;
            r = originalRadius;
            reachedMaxRadius = false;
        }
        else
        {
            // 如果不使用圓形初始化，保持原始位置
            baseAngle = 0;
            currentAngle = 0;
            originalRadius = 0;
            r = 0;
            reachedMaxRadius = true;
        }

        InitializeDirections();
        moveTimer = moveInterval;
    }

    public override void Tick()
    {
        if (controller != null && controller.GetHealth().IsDead())
        {
            OnHealthDeath();
            return;
        }

        // 檢查是否為新的一幀
        if (frameCount != Time.frameCount)
        {
            frameCount = Time.frameCount;
            hasMovedThisFrame = false;
        }

        // 如果啟用平移模式且這一幀還沒有執行過移動
        if (translationMode && !hasMovedThisFrame && transform.parent != null)
        {
            HandleParentMovement();
            hasMovedThisFrame = true;
        }

        if (circularInitMode)
        {
            MoveToCirclePosition();
        }

        fireTimer += Time.deltaTime;
        if (fireTimer >= fireInterval)
        {
            FireBullet();
            fireTimer = 0f;
        }
    }

    private void HandleParentMovement()
    {
        moveTimer += Time.deltaTime;

        if (moveTimer >= moveInterval)
        {
            currentMoveDirection = GetRandomDirection();
            moveTimer = 0f;
        }

        Vector3 newPosition = transform.parent.position + new Vector3(
            currentMoveDirection.x * parentMoveSpeed * Time.deltaTime,
            currentMoveDirection.y * parentMoveSpeed * Time.deltaTime,
            0
        );

        // 檢查並限制在邊界內
        newPosition.x = Mathf.Clamp(newPosition.x, xRange.x, xRange.y);
        newPosition.y = Mathf.Clamp(newPosition.y, yRange.x, yRange.y);

        // 如果碰到邊界，改變方向
        if (newPosition.x <= xRange.x || newPosition.x >= xRange.y ||
            newPosition.y <= yRange.x || newPosition.y >= yRange.y)
        {
            // 如果碰到左右邊界
            if (newPosition.x <= xRange.x || newPosition.x >= xRange.y)
            {
                currentMoveDirection.x = -currentMoveDirection.x;
            }
            // 如果碰到上下邊界
            if (newPosition.y <= yRange.x || newPosition.y >= yRange.y)
            {
                currentMoveDirection.y = -currentMoveDirection.y;
            }
        }

        transform.parent.position = newPosition;
    }

    private void MoveToCirclePosition()
    {
        if (transform.parent == null) return;
        int siblingCount = transform.parent.childCount;
        if (siblingCount == 0) return;
        siblingIndex = transform.GetSiblingIndex();

        // 只在旋轉模式開啟時更新角度
        if (rotationMode)
        {
            angleUpdateTimer += Time.deltaTime;
            if (angleUpdateTimer >= rotationSpeed)
            {
                currentAngle += 1f;
                angleUpdateTimer = 0f;
            }
        }

        float displayRadius = r;
        float z = transform.parent.GetChild(0).localPosition.z;

        // 嚴格檢查半徑流程
        if (!reachedMaxRadius)
        {
            r = Mathf.Lerp(r, originalRadius, lerpSpeed * Time.deltaTime);
            if (Mathf.Abs(r - originalRadius) < 0.05f)
            {
                r = originalRadius;
                reachedMaxRadius = true;
                shrinking = true;
            }
        }
        else if (shrinkRadiusMode)
        {
            if (shrinking)
            {
                r = Mathf.Lerp(r, minRadius, lerpSpeed * Time.deltaTime);
                if (Mathf.Abs(r - minRadius) < 0.05f)
                {
                    r = minRadius;
                    shrinking = false;
                }
            }
            else
            {
                r = Mathf.Lerp(r, originalRadius, lerpSpeed * Time.deltaTime);
                if (Mathf.Abs(r - originalRadius) < 0.05f)
                {
                    r = originalRadius;
                    shrinking = true;
                }
            }
        }
        else
        {
            r = originalRadius;
        }
        displayRadius = r;

        // 計算位置
        float x = displayRadius * Mathf.Cos(currentAngle) + xOffset;
        float y = displayRadius * Mathf.Sin(currentAngle) + yOffset;
        Vector3 targetPos = new Vector3(x, y, z);
        transform.localPosition = Vector3.Lerp(transform.localPosition, targetPos, lerpSpeed * Time.deltaTime);
    }

    private void FireBullet()
    {
        if (bulletPrefab == null) return;
        GameObject bullet = GameObject.Instantiate(bulletPrefab, transform.position, transform.rotation);
        var bulletBehavior = bullet.GetComponent<BulletBehavior>();
        if (bulletBehavior != null)
        {
            bulletBehavior.SetDirection(transform.forward); // 與自己同方向
            bulletBehavior.SetSpeed(bulletSpeed);
        }
    }

    public override void OnWaveMove() { }
    public override void OnWaveStart() { }

    public override void OnHealthDeath()
    {
        isLeaving = true;
        base.OnHealthDeath();
    }

    void OnDrawGizmos()
    {
        // 繪製邊界框
        Gizmos.color = Color.yellow;
        float z = transform.position.z;
        Vector3 p1 = new Vector3(xRange.x, yRange.x, z);
        Vector3 p2 = new Vector3(xRange.x, yRange.y, z);
        Vector3 p3 = new Vector3(xRange.y, yRange.y, z);
        Vector3 p4 = new Vector3(xRange.y, yRange.x, z);
        Gizmos.DrawLine(p1, p2);
        Gizmos.DrawLine(p2, p3);
        Gizmos.DrawLine(p3, p4);
        Gizmos.DrawLine(p4, p1);
    }
} 