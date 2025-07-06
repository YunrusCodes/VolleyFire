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
    public List<DialogueTrigger> waveDialogues = new List<DialogueTrigger>();
    public int waveIndex = 0; // 波次索引，用於對話觸發條件

    private bool isMoving = true;
    private bool isWaveActive = false;
    public bool isWaveClear { get; private set; } = false;
    private bool isWaitingForDialogue = false; // 新增：等待對話完成的標記

    private void Start()
    {
        // 觸發波次移動對話，並等待對話完成後才開始移動
        TriggerDialoguesAndWait(DialogueTriggerType.WaveMove);
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
            foreach (var enemy in enemies)
                enemy.OnWaveMove();
            if (Vector3.Distance(transform.position, targetPosition.position) < 0.1f)
            {
                isMoving = false;
                isWaveActive = true;
                
                // 觸發波次開始對話
                TriggerDialogues(DialogueTriggerType.WaveStart);
                
                foreach (var enemy in enemies)
                    enemy.OnWaveStart();
            }
        }
        else if (isWaveActive)
        {
            bool waveClear = true;
            foreach (var enemy in enemies)
            {
                enemy.WaveProcessing();
                waveClear = waveClear && enemy.GetHealth().IsDead();
            }
            
            // 檢查血量閾值對話觸發
            CheckHealthThresholdDialogues();
                
            if (waveClear)
            {
                Debug.Log("Wave cleared!");
                isWaveClear = true;
            }
        }
    }
    
    /// <summary>
    /// 觸發指定類型的對話
    /// </summary>
    private void TriggerDialogues(DialogueTriggerType triggerType)
    {
        if (DialogueManager.Instance == null) return;
        
        foreach (var dialogue in waveDialogues)
        {
            if (dialogue.triggerType == triggerType && dialogue.CanTrigger())
            {
                // 檢查波次索引條件
                if (dialogue.waveIndex == -1 || dialogue.waveIndex == waveIndex)
                {
                    DialogueManager.Instance.TriggerDialogue(dialogue);
                    dialogue.MarkAsTriggered();
                }
            }
        }
    }
    
    /// <summary>
    /// 觸發對話並等待完成後開始移動
    /// </summary>
    private void TriggerDialoguesAndWait(DialogueTriggerType triggerType)
    {
        if (DialogueManager.Instance == null) return;
        
        bool hasTriggeredDialogue = false;
        
        foreach (var dialogue in waveDialogues)
        {
            if (dialogue.triggerType == triggerType && dialogue.CanTrigger())
            {
                // 檢查波次索引條件
                if (dialogue.waveIndex == -1 || dialogue.waveIndex == waveIndex)
                {
                    DialogueManager.Instance.TriggerDialogue(dialogue);
                    dialogue.MarkAsTriggered();
                    hasTriggeredDialogue = true;
                }
            }
        }
        
        // 如果有觸發對話，則等待對話完成
        if (hasTriggeredDialogue)
        {
            isWaitingForDialogue = true;
            StartCoroutine(WaitForDialogueComplete());
        }
        else
        {
            // 沒有對話，直接開始移動
            StartEnemyMovement();
        }
    }
    
    /// <summary>
    /// 等待對話完成的協程
    /// </summary>
    private System.Collections.IEnumerator WaitForDialogueComplete()
    {
        // 等待對話完成
        while (DialogueManager.Instance.IsDialogueActive())
        {
            yield return null;
        }
        
        // 對話完成後，開始敵人移動
        isWaitingForDialogue = false;
        StartEnemyMovement();
    }
    
    /// <summary>
    /// 開始敵人移動
    /// </summary>
    private void StartEnemyMovement()
    {
        foreach (var enemy in enemies)
            enemy.OnWaveMove();
    }
    
    /// <summary>
    /// 檢查血量閾值對話觸發
    /// </summary>
    private void CheckHealthThresholdDialogues()
    {
        if (DialogueManager.Instance == null) return;
        
        foreach (var enemy in enemies)
        {
            if (enemy.GetHealth().IsDead()) continue;
            
            float healthPercentage = enemy.GetHealth().GetHealthPercentage();
            
            foreach (var dialogue in waveDialogues)
            {
                if (dialogue.triggerType == DialogueTriggerType.HealthThreshold && 
                    dialogue.CanTrigger() && 
                    healthPercentage <= dialogue.healthThreshold)
                {
                    DialogueManager.Instance.TriggerDialogue(dialogue);
                    dialogue.MarkAsTriggered();
                }
            }
        }
    }
    
    /// <summary>
    /// 手動觸發對話
    /// </summary>
    public void TriggerManualDialogue(string nodeName)
    {
        if (DialogueManager.Instance == null) return;
        
        var manualTrigger = new DialogueTrigger(nodeName, DialogueTriggerType.Manual);
        DialogueManager.Instance.TriggerDialogue(manualTrigger);
    }
    
    /// <summary>
    /// 重置所有對話觸發狀態
    /// </summary>
    public void ResetDialogueTriggers()
    {
        foreach (var dialogue in waveDialogues)
        {
            dialogue.ResetTrigger();
        }
    }
} 