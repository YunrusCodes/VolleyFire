using UnityEngine;

public class PointableBullet : BulletBehavior
{
    [SerializeField] private string targetTag;
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
        }
        else
        {
            // 沒有目標時，預設往前方
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
            transform.position += direction * speed * Time.deltaTime;
        }
        // 如果使用Rigidbody，移動邏輯已在Initialize中設置
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