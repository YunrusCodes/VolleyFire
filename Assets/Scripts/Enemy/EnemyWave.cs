using UnityEngine;
using System.Collections.Generic;

public class EnemyWave : MonoBehaviour
{
    [Header("敵人清單")]
    public List<EnemyController> enemies = new List<EnemyController>();
    [Header("目標位置")]
    public Transform targetPosition;
    public float moveSpeed = 5f;

    [Header("對話系統")]
    public WaveDialogueData waveMoveBeforeDialogues = new WaveDialogueData();    // 波次移動前
    public WaveDialogueData waveMoveDuringDialogues = new WaveDialogueData();    // 波次移動中
    public WaveDialogueData waveAttackBeforeDialogues = new WaveDialogueData();  // 波次攻擊前
    public List<WaveProcessTimeDialogueData> waveProcessTimeDialogues = new List<WaveProcessTimeDialogueData>(); // 波次進行時間觸發
    public List<EnemyHealthDialogueData> enemyHealthDialogues = new List<EnemyHealthDialogueData>(); // 敵人血量觸發

    private bool isMoving = true;
    private bool isWaveActive = false;
    public bool isWaveClear { get; private set; } = false;
    private bool isWaitingForDialogue = false; // 新增：等待對話完成的標記

    private void Start()
    {
        // 觸發波次移動前對話，並等待對話完成後才開始移動
        waveMoveBeforeDialogues.TriggerDialoguesAndWait();
        
        // 初始化波次進行時間對話（如果列表為空）
        if (waveProcessTimeDialogues.Count == 0)
        {
            // 可以添加一些預設的時間對話
            // AddWaveProcessTimeDialogue(5f, new List<string>{"EarlyWarning_Dialogue"});
            // AddWaveProcessTimeDialogue(15f, new List<string>{"MidWarning_Dialogue"});
        }
    }

    private void Update()
    {
        // 如果正在等待對話完成，則不執行移動邏輯
        if (isWaitingForDialogue)
        {
            return;
        }
        
        if (isMoving && targetPosition != null)
        {
            // 父物件移動
            transform.position = Vector3.MoveTowards(transform.position, targetPosition.position, moveSpeed * Time.deltaTime);
            
            // 觸發波次移動中對話
            waveMoveDuringDialogues.TriggerDialogues();
            
            foreach (var enemy in enemies)
                enemy.OnWaveMove();
                
            if (Vector3.Distance(transform.position, targetPosition.position) < 0.1f)
            {
                isMoving = false;
                isWaveActive = true;
                
                Debug.Log("波次進入戰鬥階段，開始計時對話");
                
                // 觸發波次攻擊前對話
                waveAttackBeforeDialogues.TriggerDialogues();
                
                // 開始波次進行時間對話計時
                StartWaveProcessTimeDialogues();
                
                foreach (var enemy in enemies)
                    enemy.OnWaveStart();
            }
        }
        else if (isWaveActive)
        {
            // 檢查波次進行時間觸發對話
            CheckWaveProcessTimeDialogues();
            
            // 檢查敵人血量觸發對話
            CheckEnemyHealthDialogues();
            
            bool waveClear = true;
            foreach (var enemy in enemies)
            {
                enemy.WaveProcessing();
                waveClear = waveClear && enemy.GetHealth().IsDead();
            }
                
            if (waveClear)
            {
                Debug.Log("Wave cleared!");
                isWaveClear = true;
            }
        }
        else
        {
            // Debug: 確認波次狀態
            if (isWaveClear)
            {
                Debug.Log("波次已結束，無法觸發時間對話");
            }
        }
    }
    
    /// <summary>
    /// 開始波次進行時間對話計時
    /// </summary>
    private void StartWaveProcessTimeDialogues()
    {
        Debug.Log($"開始波次進行時間對話計時，共有 {waveProcessTimeDialogues.Count} 個時間對話");
        foreach (var timeDialogue in waveProcessTimeDialogues)
        {
            timeDialogue.StartWaveTimer();
        }
    }
    
    /// <summary>
    /// 檢查波次進行時間觸發對話
    /// </summary>
    private void CheckWaveProcessTimeDialogues()
    {
        if (waveProcessTimeDialogues.Count > 0)
        {
            Debug.Log($"檢查時間對話，共 {waveProcessTimeDialogues.Count} 個");
        }
        
        foreach (var timeDialogue in waveProcessTimeDialogues)
        {
            timeDialogue.CheckAndTriggerDialogue();
        }
    }
    
    /// <summary>
    /// 檢查敵人血量觸發對話
    /// </summary>
    private void CheckEnemyHealthDialogues()
    {
        foreach (var healthDialogue in enemyHealthDialogues)
        {
            healthDialogue.CheckAndTriggerDialogue();
        }
    }
    
    /// <summary>
    /// 設置等待對話狀態
    /// </summary>
    public void SetWaitingForDialogue(bool waiting)
    {
        isWaitingForDialogue = waiting;
    }
    
    /// <summary>
    /// 對話完成回調
    /// </summary>
    public void OnDialogueComplete()
    {
        // 這個方法現在主要用於其他回調，等待狀態由 SetWaitingForDialogue 控制
    }
    
    /// <summary>
    /// 重置所有血量對話觸發狀態
    /// </summary>
    public void ResetHealthDialogues()
    {
        foreach (var healthDialogue in enemyHealthDialogues)
        {
            healthDialogue.ResetTrigger();
        }
    }
    
    /// <summary>
    /// 重置所有時間對話觸發狀態
    /// </summary>
    public void ResetTimeDialogues()
    {
        foreach (var timeDialogue in waveProcessTimeDialogues)
        {
            timeDialogue.ResetTrigger();
        }
    }
    

    
    /// <summary>
    /// 手動觸發對話
    /// </summary>
    public void TriggerManualDialogue(string nodeName)
    {
        if (DialogueManager.Instance == null) return;
        
        // 創建一個臨時的 WaveDialogueData 來處理手動觸發
        var manualDialogue = new WaveDialogueData();
        manualDialogue.dialogues.Add(nodeName);
        manualDialogue.TriggerDialogues();
    }
    
    /// <summary>
    /// 添加敵人血量對話
    /// </summary>
    public void AddEnemyHealthDialogue(BaseHealth targetHealth, float healthThreshold, List<string> dialogues)
    {
        var healthDialogue = new EnemyHealthDialogueData();
        healthDialogue.targetHealth = targetHealth;
        healthDialogue.healthThreshold = healthThreshold;
        healthDialogue.dialogues = dialogues;
        
        enemyHealthDialogues.Add(healthDialogue);
    }
    
    /// <summary>
    /// 添加波次進行時間對話
    /// </summary>
    public void AddWaveProcessTimeDialogue(float triggerTime, List<string> dialogues)
    {
        var timeDialogue = new WaveProcessTimeDialogueData();
        timeDialogue.triggerTime = triggerTime;
        timeDialogue.dialogues = dialogues;
        
        waveProcessTimeDialogues.Add(timeDialogue);
    }
    
    /// <summary>
    /// 測試時間對話（用於調試）
    /// </summary>
    [ContextMenu("測試時間對話")]
    public void TestTimeDialogues()
    {
        Debug.Log($"當前波次狀態: isMoving={isMoving}, isWaveActive={isWaveActive}");
        Debug.Log($"時間對話數量: {waveProcessTimeDialogues.Count}");
        
        if (waveProcessTimeDialogues.Count > 0)
        {
            foreach (var timeDialogue in waveProcessTimeDialogues)
            {
                Debug.Log($"時間對話: triggerTime={timeDialogue.triggerTime}, enabled={timeDialogue.enabled}, triggered={timeDialogue.Triggered}");
            }
        }
        
        // 強制開始計時
        StartWaveProcessTimeDialogues();
    }
} 