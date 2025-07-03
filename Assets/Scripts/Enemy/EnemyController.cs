using UnityEngine;

[RequireComponent(typeof(EnemyHealth))]
public class EnemyController : MonoBehaviour
{
    [SerializeField] private EnemyBehavior behavior;
    [SerializeField] private BaseHealth health;

    private void Awake()
    {
        // 自動設置 tag
        gameObject.tag = "Enemy";

        if (behavior == null)
            behavior = GetComponent<EnemyBehavior>();
        if (health == null)
            health = GetComponent<BaseHealth>();

        behavior?.Init(this);
    }

    private void Update()
    {
        behavior?.Tick();
    }

    public BaseHealth GetHealth() => health;
    public EnemyBehavior GetBehavior() => behavior;
}