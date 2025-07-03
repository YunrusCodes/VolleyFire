using UnityEngine;
 
public class PlayerHealth : BaseHealth
{
    // 這裡可擴充玩家專屬的受傷、死亡行為
    // 目前直接繼承 BaseHealth 的邏輯
    protected override void Die()
    {
        if (isDead) return;
        isDead = true;
        gameObject.SetActive(false);
    }
} 