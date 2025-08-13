using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Events;
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
    
    [Header("自動閱讀設定")]
    public bool autoRead = false;
    public float autoReadSpeed = 3f; // 自動閱讀間隔（秒）
    
    [Header("玩家控制設定")]
    public bool enablePlayerFireDuringDialogue = false; // 對話期間是否可以射擊
    public bool enablePlayerFireAfterDialogue = true;   // 對話結束後是否可以射擊
    
    [Header("BGM 切換（可選）")]
    public AudioClip BGMToSwitch;

    [Header("事件回調")]
    public UnityEvent OnDialogueBegin;  // 對話開始事件
    public UnityEvent OnDialogueEnd;    // 對話結束事件

    public WaveDialogueData()
    {
        dialogues = new List<string>();
        enabled = true;
        autoRead = false;
        autoReadSpeed = 3f;
        enablePlayerFireDuringDialogue = false;
        enablePlayerFireAfterDialogue = true;
        OnDialogueBegin = new UnityEvent();
        OnDialogueEnd = new UnityEvent();
    }

    /// <summary>
    /// 觸發所有對話
    /// </summary>
    public void TriggerDialogues()
    {
        if (!enabled || DialogueManager.Instance == null) return;
        
        // 觸發開始事件
        OnDialogueBegin?.Invoke();
        
        // 切換 BGM
        if (BGMToSwitch != null && BGMManager.Instance != null)
        {
            BGMManager.Instance.SwitchBGM(BGMToSwitch);
        }
        
        // 控制玩家射擊（對話開始時）
        PlayerController.EnableFire(enablePlayerFireDuringDialogue);
        
        foreach (var dialogue in dialogues)
        {
            DialogueManager.Instance.TriggerDialogue(dialogue);
        }
        
        // 如果啟用自動閱讀，開始自動閱讀協程
        if (autoRead && DialogueManager.Instance != null)
        {
            DialogueManager.Instance.StartAutoReadCoroutine(autoReadSpeed);
        }
        
        // 如果禁用了玩家射擊，需要監聽對話完成事件來恢復
        if (!enablePlayerFireDuringDialogue)
        {
            DialogueManager.OnDialogueEnded += OnDialogueEnded;
        }

        // 訂閱對話結束事件以觸發 OnDialogueEnd
        DialogueManager.OnDialogueEnded += InvokeOnDialogueEnd;
    }

    /// <summary>
    /// 觸發對話並等待完成
    /// </summary>
    public void TriggerDialoguesAndWait()
    {
        if (!enabled || DialogueManager.Instance == null) return;
        
        // 觸發開始事件
        OnDialogueBegin?.Invoke();
        
        // 切換 BGM
        if (BGMToSwitch != null && BGMManager.Instance != null)
        {
            BGMManager.Instance.SwitchBGM(BGMToSwitch);
        }
        
        // 控制玩家射擊（對話開始時）
        PlayerController.EnableFire(enablePlayerFireDuringDialogue);
        
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
                enemyWave.StartCoroutine(WaitForDialogueComplete(enemyWave));
            }
            
            // 如果啟用自動閱讀，在等待協程中處理
            if (autoRead && DialogueManager.Instance != null)
            {
                enemyWave?.StartCoroutine(DelayedAutoRead());
            }

            // 訂閱對話結束事件以觸發 OnDialogueEnd
            DialogueManager.OnDialogueEnded += InvokeOnDialogueEnd;
        }
    }

    /// <summary>
    /// 等待對話完成的協程
    /// </summary>
    private System.Collections.IEnumerator WaitForDialogueComplete(EnemyWave enemyWave)
    {
        while (DialogueManager.Instance.IsDialogueActive())
        {
            yield return null;
        }
        
        // 通知 EnemyWave 對話完成
        if (enemyWave != null)
        {
            enemyWave.SetWaitingForDialogue(false);
            enemyWave.OnDialogueComplete();
        }

        // 觸發結束事件
        OnDialogueEnd?.Invoke();
    }

    /// <summary>
    /// 延遲開始自動閱讀的協程
    /// </summary>
    private System.Collections.IEnumerator DelayedAutoRead()
    {
        yield return null;
        
        if (DialogueManager.Instance != null)
        {
            DialogueManager.Instance.StartAutoReadCoroutine(autoReadSpeed);
        }
    }

    /// <summary>
    /// 對話結束回調
    /// </summary>
    private void OnDialogueEnded()
    {
        // 設置對話結束後的玩家射擊狀態
        PlayerController.EnableFire(enablePlayerFireAfterDialogue);
        // 取消訂閱事件
        DialogueManager.OnDialogueEnded -= OnDialogueEnded;
    }

    /// <summary>
    /// 觸發 OnDialogueEnd 事件的方法
    /// </summary>
    private void InvokeOnDialogueEnd()
    {
        // 設置對話結束後的玩家射擊狀態
        PlayerController.EnableFire(enablePlayerFireAfterDialogue);
        OnDialogueEnd?.Invoke();
        // 取消訂閱，避免重複觸發
        DialogueManager.OnDialogueEnded -= InvokeOnDialogueEnd;
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
        
        // 對話完成後恢復玩家射擊
        PlayerController.EnableFire(true);
        
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
    
    [Header("自動閱讀設定")]
    public bool autoRead = false;
    public float autoReadSpeed = 3f; // 自動閱讀間隔（秒）
    
    [Header("玩家控制設定")]
    public bool enablePlayerFireDuringDialogue = false; // 對話期間是否可以射擊
    public bool enablePlayerFireAfterDialogue = true;   // 對話結束後是否可以射擊
    
    [Header("BGM 切換（可選）")]
    public AudioClip BGMToSwitch;
    
    public EnemyHealthDialogueData()
    {
        dialogues = new List<string>();
        enabled = true;
        triggered = false;
        autoRead = false;
        autoReadSpeed = 3f;
        enablePlayerFireDuringDialogue = false;
        enablePlayerFireAfterDialogue = true;
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
            
            // 切換 BGM
            if (BGMToSwitch != null && BGMManager.Instance != null)
            {
                BGMManager.Instance.SwitchBGM(BGMToSwitch);
            }
            
            // 控制玩家射擊
            PlayerController.EnableFire(enablePlayerFireDuringDialogue);
            
            foreach (var dialogue in dialogues)
            {
                DialogueManager.Instance.TriggerDialogue(dialogue);
            }
            
            // 如果啟用自動閱讀，開始自動閱讀協程
            if (autoRead && DialogueManager.Instance != null)
            {
                DialogueManager.Instance.StartAutoReadCoroutine(autoReadSpeed);
            }
            
            // 如果禁用了玩家射擊，需要監聽對話完成事件來恢復
            if (!enablePlayerFireDuringDialogue)
            {
                DialogueManager.OnDialogueEnded += OnDialogueEnded;
            }
        }
    }

    /// <summary>
    /// 等待對話完成的協程
    /// </summary>
    private System.Collections.IEnumerator WaitForDialogueComplete(EnemyWave enemyWave)
    {
        while (DialogueManager.Instance.IsDialogueActive())
        {
            yield return null;
        }
        PlayerController.EnableFire(true);
        if (enemyWave != null)
        {
            enemyWave.SetWaitingForDialogue(false);
            enemyWave.OnDialogueComplete();
        }
    }

    /// <summary>
    /// 延遲開始自動閱讀的協程
    /// </summary>
    private System.Collections.IEnumerator DelayedAutoRead()
    {
        yield return null;
        if (DialogueManager.Instance != null)
        {
            DialogueManager.Instance.StartAutoReadCoroutine(autoReadSpeed);
        }
    }
    
    /// <summary>
    /// 對話結束回調
    /// </summary>
    private void OnDialogueEnded()
    {
        // 設置對話結束後的玩家射擊狀態
        PlayerController.EnableFire(enablePlayerFireAfterDialogue);
        // 取消訂閱事件
        DialogueManager.OnDialogueEnded -= OnDialogueEnded;
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
    
    [Header("自動閱讀設定")]
    public bool autoRead = false;
    public float autoReadSpeed = 3f; // 自動閱讀間隔（秒）
    
    [Header("玩家控制設定")]
    public bool enablePlayerFireDuringDialogue = false; // 對話期間是否可以射擊
    public bool enablePlayerFireAfterDialogue = true;   // 對話結束後是否可以射擊
    
    [Header("BGM 切換（可選）")]
    public AudioClip BGMToSwitch;
    
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
        autoRead = false;
        autoReadSpeed = 3f;
        enablePlayerFireDuringDialogue = false;
        enablePlayerFireAfterDialogue = true;
    }
    
    /// <summary>
    /// 開始波次計時
    /// </summary>
    public void StartWaveTimer()
    {
        enabled = true; // 確保對話被啟用
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
            // 切換 BGM
            if (BGMToSwitch != null && BGMManager.Instance != null)
            {
                BGMManager.Instance.SwitchBGM(BGMToSwitch);
            }
            triggered = true;
            Debug.Log($"時間對話觸發！已過時間: {elapsedTime:F1} 秒");
            
            Debug.Log($"對話列表數量: {dialogues.Count}");
            if (dialogues.Count == 0)
            {
                Debug.LogWarning("對話列表為空！請在 Inspector 中添加對話節點名稱。");
                return;
            }
            
            // 控制玩家射擊
            PlayerController.EnableFire(enablePlayerFireDuringDialogue);
            
            foreach (var dialogue in dialogues)
            {
                Debug.Log($"嘗試觸發對話: {dialogue}");
                if (string.IsNullOrEmpty(dialogue))
                {
                    Debug.LogWarning("對話節點名稱為空！");
                    continue;
                }
                
                DialogueManager.Instance.TriggerDialogue(dialogue);
            }
            
            // 如果啟用自動閱讀，開始自動閱讀協程
            if (autoRead && DialogueManager.Instance != null)
            {
                DialogueManager.Instance.StartAutoReadCoroutine(autoReadSpeed);
            }
            DialogueManager.OnDialogueEnded += OnDialogueEnded;
        }
    }
    
    /// <summary>
    /// 對話結束回調
    /// </summary>
    private void OnDialogueEnded()
    {
        // 設置對話結束後的玩家射擊狀態
        PlayerController.EnableFire(enablePlayerFireAfterDialogue);
        // 取消訂閱事件
        DialogueManager.OnDialogueEnded -= OnDialogueEnded;
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