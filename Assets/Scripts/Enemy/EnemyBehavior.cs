using UnityEngine;
 
public abstract class EnemyBehavior : MonoBehaviour
{
    public abstract void Init(EnemyController controller);
    public abstract void Tick(); // 每幀呼叫
} 