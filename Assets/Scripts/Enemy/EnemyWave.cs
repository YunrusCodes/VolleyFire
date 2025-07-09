using UnityEngine;
using System.Collections.Generic;
using System.Collections;

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
        // 使用協程串接對話與移動流程
        StartCoroutine(WaveDialogueAndMoveFlow());
        
        // 初始化波次進行時間對話（如果列表為空）
        if (waveProcessTimeDialogues.Count == 0)
        {
            // 可以添加一些預設的時間對話
            // AddWaveProcessTimeDialogue(5f, new List<string>{"EarlyWarning_Dialogue"});
            // AddWaveProcessTimeDialogue(15f, new List<string>{"MidWarning_Dialogue"});
        }
    }

    private IEnumerator WaveDialogueAndMoveFlow()
    {
        // 1. 等待移動前對話結束
        waveMoveBeforeDialogues.TriggerDialoguesAndWait();
        while (isWaitingForDialogue) yield return null;

        // 2. 進入移動階段
        isMoving = true;

        bool hasMoveDuring = waveMoveDuringDialogues != null && waveMoveDuringDialogues.dialogues.Count > 0 && waveMoveDuringDialogues.enabled;
        bool moveDuringDialogueDone = !hasMoveDuring;

        if (hasMoveDuring)
        {
            // 有移動中對話，移動同時觸發對話（等待）
            waveMoveDuringDialogues.TriggerDialoguesAndWait();
            // 啟動一個協程監聽對話結束
            StartCoroutine(WaitForMoveDuringDialogue(() => moveDuringDialogueDone = true));
        }

        // 3. 等待移動到目標位置
        while (isMoving && targetPosition != null)
        {
            transform.position = Vector3.MoveTowards(transform.position, targetPosition.position, moveSpeed * Time.deltaTime);
            foreach (var enemy in enemies)
                enemy.OnWaveMove();
            if (Vector3.Distance(transform.position, targetPosition.position) < 0.1f)
            {
                isMoving = false;
            }
            yield return null;
        }

        // 4. 等待「移動中對話結束」與「移動結束」都成立
        while (!moveDuringDialogueDone) yield return null;

        // 5. 進入攻擊前對話，並等待結束
        waveAttackBeforeDialogues.TriggerDialoguesAndWait();
        while (isWaitingForDialogue) yield return null;

        // 6. 進入戰鬥階段
        isWaveActive = true;
        Debug.Log("波次進入戰鬥階段，開始計時對話");
        StartWaveProcessTimeDialogues();
        foreach (var enemy in enemies)
            enemy.OnWaveStart();
    }

    // 新增：監聽 waveMoveDuringDialogues 對話結束
    private IEnumerator WaitForMoveDuringDialogue(System.Action onDone)
    {
        while (isWaitingForDialogue) yield return null;
        onDone?.Invoke();
    }

    private void Update()
    {
        // 如果正在等待對話完成，則不執行戰鬥邏輯
        if (isWaitingForDialogue)
            return;

        // 只負責戰鬥階段
        if (isWaveActive)
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
            if (isWaveClear)
                Debug.Log("波次已結束，無法觸發時間對話");
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