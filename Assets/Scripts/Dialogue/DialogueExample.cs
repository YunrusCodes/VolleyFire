using UnityEngine;

/// <summary>
/// 對話系統使用示例 - 展示如何在 Inspector 中設置對話觸發器
/// </summary>
public class DialogueExample : MonoBehaviour
{
    [Header("對話設置示例")]
    [SerializeField] private EnemyWave exampleWave;
    
    private void Start()
    {
        // 示例：如何在程式碼中動態添加對話觸發器
        SetupExampleDialogues();
    }
    
    /// <summary>
    /// 設置示例對話觸發器
    /// </summary>
    private void SetupExampleDialogues()
    {
        if (exampleWave == null) return;
        
        // 添加波次移動前對話
        exampleWave.waveMoveBeforeDialogues.dialogues.Add("WaveMoveBefore_Dialogue");
        
        // 添加波次攻擊前對話
        exampleWave.waveAttackBeforeDialogues.dialogues.Add("WaveAttackBefore_Dialogue");
        
        // 添加波次進行時間對話（30秒後觸發）
        exampleWave.AddWaveProcessTimeDialogue(30f, new System.Collections.Generic.List<string>{"WaveProcessTime_Dialogue"});
        
        // 添加敵人血量對話示例
        if (exampleWave.enemies.Count > 0)
        {
            var enemyHealth = exampleWave.enemies[0].GetHealth();
            if (enemyHealth != null)
            {
                exampleWave.AddEnemyHealthDialogue(enemyHealth, 50f, new System.Collections.Generic.List<string>{"EnemyLowHealth_Dialogue"});
            }
        }
    }
    
    /// <summary>
    /// 手動觸發對話示例
    /// </summary>
    [ContextMenu("觸發緊急對話")]
    public void TriggerEmergencyDialogue()
    {
        if (DialogueManager.Instance != null)
        {
            // 創建一個臨時的 WaveDialogueData 來處理緊急對話
            var emergencyDialogue = new WaveDialogueData();
            emergencyDialogue.dialogues.Add("Emergency_Dialogue");
            emergencyDialogue.TriggerDialogues();
        }
    }
    
    /// <summary>
    /// 測試玩家受傷對話
    /// </summary>
    [ContextMenu("測試玩家受傷對話")]
    public void TestPlayerDamageDialogue()
    {
        var eventListener = FindObjectOfType<DialogueEventListener>();
        if (eventListener != null)
        {
            eventListener.OnPlayerDamaged();
        }
    }
} 