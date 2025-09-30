using UnityEngine;

public class PointableBullet : BulletBehavior
{
    [SerializeField] private string targetTag;
    [SerializeField] private int damage = 1;
    private Transform targetTransform;
    private EnemyBehavior targetEnemy;  // 新增：存取目標的 EnemyBehavior
    private bool initialized = false;
    private GameObject m_Effect = null;

    protected override void BehaviorOnStart()
    {
        initialized = false;
        targetTransform = null;
        targetEnemy = null;  // 初始化
        direction = transform.forward;
        m_Effect = this.explosionPrefab;
    }

    public override void Initialize(FirePoint firePoint)
    {
        base.Initialize(firePoint);
        if (firePoint.PointedTarget != null)
        {
            targetTransform = firePoint.PointedTarget;
            targetEnemy = targetTransform.GetComponent<EnemyBehavior>();  // 取得 EnemyBehavior
            direction = (targetTransform.position - transform.position).normalized;
        }
        initialized = true;
    }

    protected override void Move()
    {
        // 檢查目標是否已經離開（被擊敗）
        if (targetEnemy != null && targetEnemy.IsLeaving())
        {
            // 目標已離開，直線前進
            transform.position += direction * speed * Time.deltaTime;
            return;
        }

        if (targetTransform != null)
        {
            Vector3 toTarget = targetTransform.position - transform.position;
            float distToTarget = toTarget.magnitude;
            float moveDist = speed * Time.deltaTime;

            if (distToTarget == 0f)
            {
                DestroyBullet();
                return;
            }
            else if (moveDist < distToTarget)
            {
                direction = toTarget.normalized;
                transform.position += direction * moveDist;
            }
            else
            {
                transform.position = targetTransform.position;
            }
        }
        else
        {
            // 沒有目標就直線前進
            transform.position += direction * speed * Time.deltaTime;
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag(targetTag))
        {
            var targetHealth = collision.gameObject.GetComponent<BaseHealth>();
            if (targetHealth != null)
            {
                targetHealth.TakeDamage(damage);
            }
            DestroyBullet();
        }
    }
} 