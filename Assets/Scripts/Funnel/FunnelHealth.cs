using UnityEngine;

public class FunnelHealth : BaseHealth
{
    protected override void Die()
    {
        if (isDead) return;
        
        // 呼叫基底類別的 Die 方法
        base.Die();
        
        // 銷毀遊戲物件
        Destroy(gameObject);
    }
} 