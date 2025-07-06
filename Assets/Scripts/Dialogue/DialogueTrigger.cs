using UnityEngine;

/// <summary>
/// 對話觸發器 - 定義對話觸發的條件和參數
/// </summary>
[System.Serializable]
public class DialogueTrigger
{
    [Header("對話設定")]
    public string nodeName;           // Yarn 節點名稱
    public bool pauseGame = false;    // 是否暫停遊戲
    public float delay = 0f;         // 延遲觸發時間
    
    [Header("觸發條件")]
    public DialogueTriggerType triggerType = DialogueTriggerType.Manual;
    public float healthThreshold = 0.5f;  // 血量閾值（百分比）
    public int waveIndex = -1;       // 波次索引（-1表示任意波次）
    
    [Header("觸發設定")]
    public bool triggerOnce = true;   // 是否只觸發一次
    public bool isTriggered = false; // 是否已觸發過
    
    public DialogueTrigger(string nodeName, DialogueTriggerType type = DialogueTriggerType.Manual)
    {
        this.nodeName = nodeName;
        this.triggerType = type;
    }
    
    /// <summary>
    /// 檢查是否可以觸發
    /// </summary>
    public bool CanTrigger()
    {
        if (triggerOnce && isTriggered) return false;
        return true;
    }
    
    /// <summary>
    /// 標記為已觸發
    /// </summary>
    public void MarkAsTriggered()
    {
        isTriggered = true;
    }
    
    /// <summary>
    /// 重置觸發狀態
    /// </summary>
    public void ResetTrigger()
    {
        isTriggered = false;
    }
}

/// <summary>
/// 對話觸發類型
/// </summary>
public enum DialogueTriggerType
{
    Manual,         // 手動觸發
    WaveStart,      // 波次開始時
    WaveMove,       // 波次移動時
    WaveProcessing, // 波次進行中
    HealthThreshold, // 血量閾值
    EnemyDeath,     // 敵人死亡時
    PlayerDamage     // 玩家受傷時
} 