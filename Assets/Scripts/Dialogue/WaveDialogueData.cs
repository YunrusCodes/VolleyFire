using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 波次對話數據 - 管理單一類型的對話列表
/// </summary>
[System.Serializable]
public class WaveDialogueData
{
    [Header("對話設定")]
    public List<string> dialogues = new List<string>();
    
    [Header("觸發設定")]
    public bool enabled = true;
    
    public WaveDialogueData()
    {
        dialogues = new List<string>();
        enabled = true;
    }
    
    /// <summary>
    /// 觸發所有對話
    /// </summary>
    public void TriggerDialogues()
    {
        if (!enabled || DialogueManager.Instance == null) return;
        
        foreach (var dialogue in dialogues)
        {
            DialogueManager.Instance.TriggerDialogue(dialogue);
        }
    }
    
    /// <summary>
    /// 觸發對話並等待完成
    /// </summary>
    public void TriggerDialoguesAndWait()
    {
        if (!enabled || DialogueManager.Instance == null) return;
        
        bool hasTriggeredDialogue = false;
        
        foreach (var dialogue in dialogues)
        {
            DialogueManager.Instance.TriggerDialogue(dialogue);
            hasTriggeredDialogue = true;
        }
        
        if (hasTriggeredDialogue)
        {
            // 開始等待協程
            var enemyWave = MonoBehaviour.FindObjectOfType<EnemyWave>();
            if (enemyWave != null)
            {
                enemyWave.SetWaitingForDialogue(true);
                enemyWave.StartCoroutine(WaitForDialogueComplete());
            }
        }
    }
    
    /// <summary>
    /// 等待對話完成的協程
    /// </summary>
    private System.Collections.IEnumerator WaitForDialogueComplete()
    {
        while (DialogueManager.Instance.IsDialogueActive())
        {
            yield return null;
        }
        
        // 通知 EnemyWave 對話完成
        var enemyWave = MonoBehaviour.FindObjectOfType<EnemyWave>();
        if (enemyWave != null)
        {
            enemyWave.SetWaitingForDialogue(false);
            enemyWave.OnDialogueComplete();
        }
    }
}

/// <summary>
/// 敵人血量觸發對話數據
/// </summary>
[System.Serializable]
public class EnemyHealthDialogueData
{
    [Header("觸發條件")]
    public BaseHealth targetHealth;
    public float healthThreshold = 50f;
    
    [Header("對話設定")]
    public List<string> dialogues = new List<string>();
    
    [Header("觸發設定")]
    public bool enabled = true;
    private bool triggered = false; // 避免重複觸發
    public bool Triggered => triggered; // 只讀屬性
    
    public EnemyHealthDialogueData()
    {
        dialogues = new List<string>();
        enabled = true;
        triggered = false;
    }
    
    /// <summary>
    /// 檢查並觸發血量對話
    /// </summary>
    public void CheckAndTriggerDialogue()
    {
        if (!enabled || triggered || targetHealth == null || DialogueManager.Instance == null) return;
        
        if (targetHealth.GetCurrentHealth() <= healthThreshold)
        {
            triggered = true;
            
            foreach (var dialogue in dialogues)
            {
                DialogueManager.Instance.TriggerDialogue(dialogue);
            }
        }
    }
    
    /// <summary>
    /// 重置觸發狀態
    /// </summary>
    public void ResetTrigger()
    {
        triggered = false;
    }
}

/// <summary>
/// 波次進行時間觸發對話數據
/// </summary>
[System.Serializable]
public class WaveProcessTimeDialogueData
{
    [Header("觸發條件")]
    public float triggerTime = 5f; // 觸發時間（秒）
    
    [Header("對話設定")]
    public List<string> dialogues = new List<string>();
    
    [Header("觸發設定")]
    public bool enabled = true;
    private bool triggered = false; // 避免重複觸發
    public bool Triggered => triggered; // 只讀屬性
    
    // 私有變數
    private float waveStartTime = 0f;
    private bool isWaveStarted = false;
    private int lastDisplayedSecond = 0; // 用於控制倒數顯示
    
    public WaveProcessTimeDialogueData()
    {
        dialogues = new List<string>();
        enabled = true;
        triggered = false;
        waveStartTime = 0f;
        isWaveStarted = false;
        lastDisplayedSecond = 0;
    }
    
    /// <summary>
    /// 開始波次計時
    /// </summary>
    public void StartWaveTimer()
    {
        if (!enabled) return;
        
        waveStartTime = Time.time;
        isWaveStarted = true;
        triggered = false;
        
        Debug.Log($"開始時間對話計時，將在 {triggerTime} 秒後觸發");
    }
    
    /// <summary>
    /// 檢查並觸發時間對話
    /// </summary>
    public void CheckAndTriggerDialogue()
    {
        Debug.Log("CheckAndTriggerDialogue()");
        
        if (!enabled)
        {
            Debug.Log("CheckAndTriggerDialogue()_NULL: enabled = false");
            return;
        }
        
        if (triggered)
        {
            Debug.Log("CheckAndTriggerDialogue()_NULL: triggered = true");
            return;
        }
        
        if (!isWaveStarted)
        {
            Debug.Log("CheckAndTriggerDialogue()_NULL: isWaveStarted = false");
            return;
        }
        
        if (DialogueManager.Instance == null)
        {
            Debug.Log("CheckAndTriggerDialogue()_NULL: DialogueManager.Instance == null");
            return;
        }
        
        float elapsedTime = Time.time - waveStartTime;
        float remainingTime = triggerTime - elapsedTime;
        
        // Debug 倒數信息 - 只在整數秒數時顯示
        if (remainingTime > 0 && remainingTime <= triggerTime)
        {
            int currentSecond = Mathf.CeilToInt(remainingTime);
            if (currentSecond != lastDisplayedSecond)
            {
                Debug.Log($"時間對話倒數: {remainingTime:F1} 秒後觸發");
                lastDisplayedSecond = currentSecond;
            }
        }
        
        if (elapsedTime >= triggerTime)
        {
            triggered = true;
            Debug.Log($"時間對話觸發！已過時間: {elapsedTime:F1} 秒");
            
            foreach (var dialogue in dialogues)
            {
                DialogueManager.Instance.TriggerDialogue(dialogue);
            }
        }
    }
    
    /// <summary>
    /// 重置觸發狀態
    /// </summary>
    public void ResetTrigger()
    {
        triggered = false;
        isWaveStarted = false;
        waveStartTime = 0f;
        lastDisplayedSecond = 0;
    }
    
    /// <summary>
    /// 獲取剩餘時間
    /// </summary>
    public float GetRemainingTime()
    {
        if (!isWaveStarted) return triggerTime;
        
        float elapsedTime = Time.time - waveStartTime;
        return Mathf.Max(0f, triggerTime - elapsedTime);
    }
    
    /// <summary>
    /// 獲取已過時間
    /// </summary>
    public float GetElapsedTime()
    {
        if (!isWaveStarted) return 0f;
        
        return Time.time - waveStartTime;
    }
} 