using UnityEngine;

public class PointableBullet : BulletBehavior
{
    [SerializeField] private string targetTag;
    [SerializeField] private int damage = 1;
    private Vector3 targetPosition;
    private bool initialized = false;
    private GameObject m_Effect = null;

    protected override void BehaviorOnStart()
    {
        // 初始化基本設定
        initialized = false;
        
        // 預設往前方
        targetPosition = transform.position + transform.forward * 100f;
        direction = transform.forward;
        m_Effect = this.explosionPrefab;
        this.explosionPrefab = null;
        
    }

    public override void Initialize(FirePoint firePoint)
    {
        base.Initialize(firePoint);
        if (firePoint.PointedTarget != null)
        {
            targetPosition = firePoint.PointedTarget.position;
            direction = (targetPosition - transform.position).normalized;
        }
        initialized = true;
    }

    protected override void Move()
    {
        if (firePoint.PointedTarget != null)
        {
            Vector3 toTarget = firePoint.PointedTarget.position - transform.position;
            float distToTarget = toTarget.magnitude;
            float moveDist = speed * Time.deltaTime;
            // 若已經抵達目標點，不再 LookAt
            if (distToTarget == 0f)
            {
                // 可選：抵達目標後保持靜止
                // return;
                // 或保持原方向前進（什麼都不做）
            }
            else if (moveDist < distToTarget)
            {
                // 只有移動距離小於目標距離時才追蹤 LookAt
                direction = toTarget.normalized;
                transform.position += direction * moveDist;
            }
            else
            {
                // 如果本幀移動距離大於等於到目標距離，直接移動到目標點
                transform.position = firePoint.PointedTarget.position;
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
            this.explosionPrefab = m_Effect;
            var targetHealth = collision.gameObject.GetComponent<BaseHealth>();
            if (targetHealth != null)
            {
                targetHealth.TakeDamage(damage);
            }
            DestroyBullet();
        }
    }
} 