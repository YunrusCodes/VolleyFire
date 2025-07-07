using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(EnemyHealth))]
public class EnemyController : MonoBehaviour
{
    [SerializeField] private EnemyBehavior behavior;
    [SerializeField] private BaseHealth health;
    
    [Header("對話系統")]
    public List<string> enemyDeathDialogues = new List<string>();

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
    }
    

    
    /// <summary>
    /// 敵人死亡時觸發對話
    /// </summary>
    public void TriggerDeathDialogue()
    {
        if (DialogueManager.Instance == null) return;
        
        // 創建一個臨時的 WaveDialogueData 來處理死亡對話
        var deathDialogue = new WaveDialogueData();
        deathDialogue.dialogues = enemyDeathDialogues;
        deathDialogue.TriggerDialogues();
    }

    public BaseHealth GetHealth() => health;
    public EnemyBehavior GetBehavior() => behavior;
}