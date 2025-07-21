using UnityEngine;
 
public abstract class EnemyBehavior : MonoBehaviour
{
    [SerializeField] protected Animator animator;
    public bool isLeaving = false;
    public bool IsLeaving() { return isLeaving; }

    public virtual void Init(EnemyController controller)
    {
        animator = GetComponent<Animator>();
    }
    public abstract void Tick(); // 每幀呼叫

    // 波次移動階段，可由子類覆寫
    public virtual void OnWaveMove() { }
    // 波次開始行動階段，可由子類覆寫
    public virtual void OnWaveStart() { }
    public virtual void OnHealthDeath() {
        if (animator != null)
        {
            animator.SetTrigger("Die");
        }
    }
}