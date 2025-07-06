using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(EnemyHealth))]
public class EnemyController : MonoBehaviour
{
    [SerializeField] private EnemyBehavior behavior;
    [SerializeField] private BaseHealth health;
    
    [Header("對話系統")]
    public List<DialogueTrigger> enemyDialogues = new List<DialogueTrigger>();

    private void Awake()
    {
        // 自動設置 tag
        gameObject.tag = "Enemy";

        if (behavior == null)
            behavior = GetComponent<EnemyBehavior>();
        if (health == null)
            health = GetComponent<BaseHealth>();
    }

    // 波次移動階段呼叫
    public void OnWaveMove()
    {
        behavior?.OnWaveMove();
    }

    // 波次開始行動時呼叫
    public void OnWaveStart()
    {
        behavior?.Init(this);
        behavior?.OnWaveStart();
    }

    // 波次進行中每幀呼叫
    public void WaveProcessing()
    {
        behavior?.Tick();
        
        // 檢查敵人個別的對話觸發
        CheckEnemyDialogues();
    }
    
    /// <summary>
    /// 檢查敵人個別的對話觸發
    /// </summary>
    private void CheckEnemyDialogues()
    {
        if (DialogueManager.Instance == null || health.IsDead()) return;
        
        float healthPercentage = health.GetHealthPercentage();
        
        foreach (var dialogue in enemyDialogues)
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
    
    /// <summary>
    /// 敵人死亡時觸發對話
    /// </summary>
    public void TriggerDeathDialogue()
    {
        if (DialogueManager.Instance == null) return;
        
        foreach (var dialogue in enemyDialogues)
        {
            if (dialogue.triggerType == DialogueTriggerType.EnemyDeath && dialogue.CanTrigger())
            {
                DialogueManager.Instance.TriggerDialogue(dialogue);
                dialogue.MarkAsTriggered();
            }
        }
    }

    public BaseHealth GetHealth() => health;
    public EnemyBehavior GetBehavior() => behavior;
}