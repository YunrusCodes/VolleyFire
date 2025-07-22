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

    protected void OnTriggerEnter(Collider other)
    {
        Debug.Log("targetTag: " + targetTag);
        if (other.CompareTag(targetTag))
        {
            var targetHealth = other.GetComponent<BaseHealth>();
            Debug.Log("targetHealth: " + targetHealth);
            if (targetHealth != null)
            {
                Debug.Log("targetHealth is not null : " + other.name);
                targetHealth.TakeDamage(damage);
            }
            else
            {
                Debug.Log("targetHealth is null : " + other.name);
            }
            DestroyBullet();
        }
        else
        {
            Debug.Log("targetTag is not " + targetTag + " : " + other.tag);
        }
    }

}
