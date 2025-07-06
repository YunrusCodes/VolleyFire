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
        
        // 添加波次開始對話
        var waveStartDialogue = new DialogueTrigger("WaveStart_Dialogue", DialogueTriggerType.WaveStart)
        {
            pauseGame = false,
            triggerOnce = true,
            waveIndex = 0 // 只在第一波觸發
        };
        exampleWave.waveDialogues.Add(waveStartDialogue);
        
        // 添加波次移動對話
        var waveMoveDialogue = new DialogueTrigger("WaveMove_Dialogue", DialogueTriggerType.WaveMove)
        {
            pauseGame = false,
            triggerOnce = false // 每波都觸發
        };
        exampleWave.waveDialogues.Add(waveMoveDialogue);
        
        // 添加血量閾值對話
        var healthDialogue = new DialogueTrigger("HealthThreshold_Dialogue", DialogueTriggerType.HealthThreshold)
        {
            healthThreshold = 0.3f, // 血量低於 30% 時觸發
            triggerOnce = true
        };
        exampleWave.waveDialogues.Add(healthDialogue);
    }
    
    /// <summary>
    /// 手動觸發對話示例
    /// </summary>
    [ContextMenu("觸發緊急對話")]
    public void TriggerEmergencyDialogue()
    {
        if (DialogueManager.Instance != null)
        {
            var emergencyTrigger = new DialogueTrigger("Emergency_Dialogue", DialogueTriggerType.Manual)
            {
                pauseGame = true // 暫停遊戲
            };
            DialogueManager.Instance.TriggerDialogue(emergencyTrigger);
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