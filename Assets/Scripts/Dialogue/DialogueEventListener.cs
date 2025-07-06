using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 對話事件監聽器 - 監聽遊戲事件並觸發相應對話
/// </summary>
public class DialogueEventListener : MonoBehaviour
{
    [Header("對話觸發設定")]
    public List<DialogueTrigger> eventDialogues = new List<DialogueTrigger>();
    
    private void Start()
    {
        // 訂閱對話事件
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
        Debug.Log($"對話開始: {nodeName}");
        
        // 可以在這裡添加對話開始時的特殊邏輯
        // 例如：暫停敵人 AI、顯示 UI 等
    }
    
    /// <summary>
    /// 對話結束事件
    /// </summary>
    private void OnDialogueEnded()
    {
        Debug.Log("對話結束");
        
        // 可以在這裡添加對話結束時的特殊邏輯
        // 例如：恢復敵人 AI、隱藏 UI 等
    }
    
    /// <summary>
    /// 手動觸發事件對話
    /// </summary>
    public void TriggerEventDialogue(string nodeName)
    {
        if (DialogueManager.Instance == null) return;
        
        var trigger = new DialogueTrigger(nodeName, DialogueTriggerType.Manual);
        DialogueManager.Instance.TriggerDialogue(trigger);
    }
    
    /// <summary>
    /// 玩家受傷時觸發對話
    /// </summary>
    public void OnPlayerDamaged()
    {
        if (DialogueManager.Instance == null) return;
        
        foreach (var dialogue in eventDialogues)
        {
            if (dialogue.triggerType == DialogueTriggerType.PlayerDamage && dialogue.CanTrigger())
            {
                DialogueManager.Instance.TriggerDialogue(dialogue);
                dialogue.MarkAsTriggered();
                break; // 只觸發第一個符合條件的對話
            }
        }
    }
} 