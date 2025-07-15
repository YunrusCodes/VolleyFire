using UnityEngine;

public class SlashArea : BulletBehavior
{
    [SerializeField] private string targetTag;
    [SerializeField] private float damage = 1f;

    protected override void BehaviorOnStart()
    {
        // 劍痕的初始化
    }

    protected override void Move()
    {
        // 劍痕的移動
    }

    protected void OnCollisionEnter(Collision collision)
    {
        Debug.Log("targetTag: " + targetTag);
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
