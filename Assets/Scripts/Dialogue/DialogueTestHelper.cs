using UnityEngine;

/// <summary>
/// 對話測試輔助腳本 - 用於測試對話等待功能
/// </summary>
public class DialogueTestHelper : MonoBehaviour
{
    [Header("測試設定")]
    [SerializeField] private EnemyWave testWave;
    [SerializeField] private bool enableTestLogs = true;
    
    private void Start()
    {
        // 訂閱對話事件來監控狀態
        DialogueManager.OnDialogueStarted += OnDialogueStarted;
        DialogueManager.OnDialogueEnded += OnDialogueEnded;
    }
    
    private void OnDestroy()
    {
        // 取消訂閱對話事件
        DialogueManager.OnDialogueStarted -= OnDialogueStarted;
        DialogueManager.OnDialogueEnded -= OnDialogueEnded;
    }
    
    /// <summary>
    /// 對話開始事件
    /// </summary>
    private void OnDialogueStarted(string nodeName)
    {
        if (enableTestLogs)
        {
            Debug.Log($"[對話測試] 對話開始: {nodeName}");
        }
    }
    
    /// <summary>
    /// 對話結束事件
    /// </summary>
    private void OnDialogueEnded()
    {
        if (enableTestLogs)
        {
            Debug.Log("[對話測試] 對話結束");
        }
    }
    
    /// <summary>
    /// 測試波次移動對話等待功能
    /// </summary>
    [ContextMenu("測試波次移動對話等待")]
    public void TestWaveMoveDialogueWait()
    {
        if (testWave == null)
        {
            testWave = FindObjectOfType<EnemyWave>();
        }
        
        if (testWave != null)
        {
            Debug.Log("[對話測試] 開始測試波次移動對話等待功能");
            
            // 重置對話觸發狀態
            testWave.ResetDialogueTriggers();
            
            // 添加測試對話
            var testDialogue = new DialogueTrigger("WaveMove_Dialogue", DialogueTriggerType.WaveMove)
            {
                pauseGame = false,
                triggerOnce = true,
                waveIndex = testWave.waveIndex
            };
            testWave.waveDialogues.Add(testDialogue);
            
            // 重新啟動波次來測試
            testWave.gameObject.SetActive(false);
            testWave.gameObject.SetActive(true);
        }
        else
        {
            Debug.LogWarning("[對話測試] 找不到 EnemyWave 組件");
        }
    }
    
    /// <summary>
    /// 測試手動觸發對話
    /// </summary>
    [ContextMenu("測試手動觸發對話")]
    public void TestManualDialogue()
    {
        if (DialogueManager.Instance != null)
        {
            var testDialogue = new DialogueTrigger("Emergency_Dialogue", DialogueTriggerType.Manual)
            {
                pauseGame = false,
                triggerOnce = true
            };
            DialogueManager.Instance.TriggerDialogue(testDialogue);
            Debug.Log("[對話測試] 手動觸發對話");
        }
    }
    
    /// <summary>
    /// 檢查對話系統狀態
    /// </summary>
    [ContextMenu("檢查對話系統狀態")]
    public void CheckDialogueSystemStatus()
    {
        if (DialogueManager.Instance != null)
        {
            bool isActive = DialogueManager.Instance.IsDialogueActive();
            Debug.Log($"[對話測試] 對話系統狀態: {(isActive ? "進行中" : "閒置")}");
        }
        else
        {
            Debug.LogWarning("[對話測試] DialogueManager 未找到");
        }
    }
    
    /// <summary>
    /// 強制結束當前對話
    /// </summary>
    [ContextMenu("強制結束對話")]
    public void ForceEndDialogue()
    {
        if (DialogueManager.Instance != null)
        {
            DialogueManager.Instance.ForceEndDialogue();
            Debug.Log("[對話測試] 強制結束對話");
        }
    }
}