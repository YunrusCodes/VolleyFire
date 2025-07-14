using UnityEngine;
 
public abstract class EnemyBehavior : MonoBehaviour
{
    public abstract void Init(EnemyController controller);
    public abstract void Tick(); // 每幀呼叫

    // 波次移動階段，可由子類覆寫
    public virtual void OnWaveMove() { }
    // 波次開始行動階段，可由子類覆寫
    public virtual void OnWaveStart() { }
}