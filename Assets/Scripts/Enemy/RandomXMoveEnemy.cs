using UnityEngine;

public class RandomXMoveEnemy : EnemyBehavior
{
    public float speed = 5f;
    public float xAmplitude = 3f;
    public float xFrequency = 1f;
    public float xMin = -8f;
    public float xMax = 8f;
    private EnemyController controller;
    private float timeOffset;
    private float baseX;

    public override void Init(EnemyController controller)
    {
        this.controller = controller;
        timeOffset = Random.Range(0f, 100f); // 避免所有敵人同步
        baseX = transform.position.x;
    }

    public override void Tick()
    {
        if (controller.GetHealth().IsDead())
        {
            Destroy(gameObject);
            return;
        }
        float x = baseX + Mathf.Sin((Time.time + timeOffset) * xFrequency) * xAmplitude;
        x = Mathf.Clamp(x, xMin, xMax);
        Vector3 pos = transform.position;
        pos.x = x;
        transform.position = pos;
    }
} 