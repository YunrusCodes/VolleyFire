using UnityEngine;

public class FederalAircraft : EnemyBehavior
{
    [Header("子彈Prefab")]
    public GameObject bulletPrefab;
    public float fireInterval = 1.5f;
    public float bulletSpeed = 10f;
    private float fireTimer = 0f;
    private EnemyController controller;
    [Header("環繞半徑 r")]
    public float r = 5f;
    [Header("父物件索引值(僅顯示)")]
    public int siblingIndex;
    [Header("Lerp移動速度")]
    public float lerpSpeed = 5f;
    [Header("xy偏移值")]
    public float xOffset = 0f;
    public float yOffset = 0f;
    [Header("縮放半徑模式")]
    public bool shrinkRadiusMode = false;
    public float minRadius = 2f;
    private bool shrinking = true;
    private float originalRadius;
    private float baseAngle;
    private float currentAngle;
    private float angleUpdateTimer = 0f;
    private bool reachedMaxRadius = false;

    public override void Init(EnemyController controller)
    {
        this.controller = controller;
        fireTimer = 0f;
        if (transform.parent == null) return;
        int siblingCount = transform.parent.childCount;
        if (siblingCount == 0) return;
        siblingIndex = transform.GetSiblingIndex();
        baseAngle = 2 * Mathf.PI * siblingIndex / siblingCount;
        currentAngle = baseAngle;
        originalRadius = r;
        // 不直接定位，讓Tick中的Lerp來移動到最大半徑
        r = originalRadius;
        reachedMaxRadius = false;
    }

    public override void Tick()
    {
        if (controller != null && controller.GetHealth().IsDead())
        {
            gameObject.SetActive(false);
            return;
        }
        MoveToCirclePosition();
        fireTimer += Time.deltaTime;
        if (fireTimer >= fireInterval)
        {
            FireBullet();
            fireTimer = 0f;
        }
    }

    private void MoveToCirclePosition()
    {
        if (transform.parent == null) return;
        int siblingCount = transform.parent.childCount;
        if (siblingCount == 0) return;
        siblingIndex = transform.GetSiblingIndex();
        angleUpdateTimer += Time.deltaTime;
        if (angleUpdateTimer >= 1f)
        {
            currentAngle += 1f;
            angleUpdateTimer = 0f;
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
} 