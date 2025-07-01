using UnityEngine;

/// <summary>
/// 筆直前行子彈 - 繼承自BulletBehavior
/// </summary>
public class StraightBullet : BulletBehavior
{
    [SerializeField] private string targetTag;

    protected override void Move()
    {
        // 如果不使用Rigidbody，使用Transform移動
        if (!useRigidbody)
        {
            transform.position += direction * speed * Time.deltaTime;
        }
        // 如果使用Rigidbody，移動邏輯已在Initialize中設置
    }

    protected void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Enemy"))
        {
            DestroyBullet();
        }
    }
    
} 