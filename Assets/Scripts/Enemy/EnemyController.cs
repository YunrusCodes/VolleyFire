using UnityEngine;
using System.Collections.Generic;

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
    }

    // 波次移動階段呼叫
    public void OnWaveMove()
    {
        behavior?.OnWaveMove();
    }

    // 波次開始行動時呼叫
    public void OnWaveStart()
    {
        behavior?.Init(this);
        behavior?.OnWaveStart();
    }

    // 波次進行中每幀呼叫
    public void WaveProcessing()
    {
        behavior?.Tick();
    }
    
    public BaseHealth GetHealth() => health;
    public EnemyBehavior GetBehavior() => behavior;
}