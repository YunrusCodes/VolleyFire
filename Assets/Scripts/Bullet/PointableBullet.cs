using UnityEngine;

public class PointableBullet : BulletBehavior
{
    [SerializeField] private int damage = 1;
    private Vector3 targetPosition;
    private bool initialized = false;

    public override void Initialize(FirePoint firePoint)
    {
        base.Initialize(firePoint);
        if (firePoint.PointedTarget.HasValue)
        {
            targetPosition = firePoint.PointedTarget.Value;
            direction = (targetPosition - transform.position).normalized;
            Debug.Log("有目標 " + direction);
        }
        else
        {
            // 沒有目標時，預設往前方
            Debug.Log("沒有目標 預設往前方");
            targetPosition = transform.position + transform.forward * 100f;
            direction = transform.forward;
            initialized = true;
        }
        initialized = true;
    }

    protected override void Move()
    {
        if (!useRigidbody)
        {
            Debug.Log(direction);
            transform.position += direction * speed * Time.deltaTime;
        }
        // 如果使用Rigidbody，移動邏輯已在Initialize中設置
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Enemy"))
        {
            var enemyHealth = collision.gameObject.GetComponent<EnemyHealth>();
            if (enemyHealth != null)
            {
                enemyHealth.TakeDamage(damage);
            }
            DestroyBullet();
        }
    }
} 