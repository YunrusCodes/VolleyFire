using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using UnityEngine.Events;
using UnityEngine.UI;

public class EnemyWave : MonoBehaviour
{
    [Header("敵人清單")]
    public List<EnemyController> enemies = new List<EnemyController>();
    [Header("目標位置")]
    public Transform targetPosition;
    public float moveSpeed = 5f;

    [Header("Boss 血條設置")]
    [SerializeField] private BaseHealth bossHealth;
    [SerializeField] private Slider bossHealthSlider;
    [SerializeField] private Image bossFillImage;
    [SerializeField] private Color bossFullHealthColor = Color.green;
    [SerializeField] private Color bossLowHealthColor = Color.red;
    [SerializeField] private float bossLowHealthPercent = 0.3f;

    [Header("事件")]
    public UnityEvent OnWaveStart = new UnityEvent();
    public UnityEvent OnWaveClear = new UnityEvent();

    [Header("對話系統")]
    public WaveDialogueData waveMoveBeforeDialogues = new WaveDialogueData();    // 波次移動前
    public WaveDialogueData waveMoveDuringDialogues = new WaveDialogueData();    // 波次移動中
    public WaveDialogueData waveAttackBeforeDialogues = new WaveDialogueData();  // 波次攻擊前
    public List<WaveProcessTimeDialogueData> waveProcessTimeDialogues = new List<WaveProcessTimeDialogueData>(); // 波次進行時間觸發
    public List<EnemyHealthDialogueData> enemyHealthDialogues = new List<EnemyHealthDialogueData>(); // 敵人血量觸發

    [Header("提示系統")]
    public Text hintText; // 提示文字 UI 組件
    public GameObject hintObject; // HINT 物件（控制 SetActive）
    public List<EnemyHealthHintData> enemyHealthHints = new List<EnemyHealthHintData>(); // 敵人血量提示

    private bool isMoving = true;
    private bool isWaveActive = false;
    public bool isWaveClear { get; private set; } = false;
    private bool isWaitingForDialogue = false; // 新增：等待對話完成的標記

    private void Start()
    {
        // 確保 HINT 物件初始狀態為關閉
        if (hintObject != null)
        {
            hintObject.SetActive(false);
        }

        // 初始化 Boss 血條
        InitializeBossHealthBar();
        
        // 使用協程串接對話與移動流程
        StartCoroutine(WaveDialogueAndMoveFlow());
    }

    private IEnumerator WaveDialogueAndMoveFlow()
    {    
        OnWaveStart?.Invoke();
        // 1. 等待移動前對話結束
        waveMoveBeforeDialogues?.TriggerDialoguesAndWait();
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

        // 更新 Boss 血條
        UpdateBossHealthBar();

        // 只負責戰鬥階段
        if (isWaveActive)
        {
            // 檢查波次進行時間觸發對話
            CheckWaveProcessTimeDialogues();
            // 檢查敵人血量觸發對話
            CheckEnemyHealthDialogues();
            // 檢查敵人血量提示
            CheckEnemyHealthHints();
            bool waveClear = true;
            foreach (var enemy in enemies)
            {
                enemy.WaveProcessing();
                waveClear = waveClear && enemy.IsLeaving();
            }
            if (waveClear)
            {
                Debug.Log("Wave cleared!");
                isWaveClear = true;
                OnWaveClear?.Invoke();
                // 清空提示文字
                ClearHintText();
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
    /// 檢查敵人血量提示
    /// </summary>
    private void CheckEnemyHealthHints()
    {
        foreach (var healthHint in enemyHealthHints)
        {
            string hintText = healthHint.CheckAndTriggerHint();
            if (!string.IsNullOrEmpty(hintText))
            {
                ShowHintText(hintText);
            }
        }
    }
    
    /// <summary>
    /// 顯示提示文字
    /// </summary>
    private void ShowHintText(string text)
    {
        if (hintText != null)
        {
            hintText.text = text;
            Debug.Log($"顯示提示: {text}");
        }
        
        // 開啟 HINT 物件
        if (hintObject != null)
        {
            hintObject.SetActive(true);
            Debug.Log("開啟 HINT 物件");
        }
    }
    
    /// <summary>
    /// 清空提示文字
    /// </summary>
    private void ClearHintText()
    {
        if (hintText != null)
        {
            hintText.text = "";
            Debug.Log("清空提示文字");
        }
        
        // 關閉 HINT 物件
        if (hintObject != null)
        {
            hintObject.SetActive(false);
            Debug.Log("關閉 HINT 物件");
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
    /// 重置所有血量提示觸發狀態
    /// </summary>
    public void ResetHealthHints()
    {
        foreach (var healthHint in enemyHealthHints)
        {
            healthHint.ResetTrigger();
        }
    }

    /// <summary>
    /// 初始化 Boss 血條
    /// </summary>
    private void InitializeBossHealthBar()
    {
        if (bossHealth != null)
        {
            bossHealthSlider.gameObject.SetActive(true);
            bossHealthSlider.maxValue = bossHealth.GetMaxHealth();
            bossHealthSlider.value = bossHealth.GetCurrentHealth();
            UpdateBossHealthBar();
        }
    }

    /// <summary>
    /// 更新 Boss 血條
    /// </summary>
    private void UpdateBossHealthBar()
    {
        if (bossHealth == null || bossHealthSlider == null || bossFillImage == null) return;

        bossHealthSlider.value = bossHealth.GetCurrentHealth();
        
        // 計算血量百分比
        float healthPercent = bossHealth.GetCurrentHealth() / bossHealth.GetMaxHealth();
        
        // 根據血量百分比更新顏色
        bossFillImage.color = Color.Lerp(bossLowHealthColor, bossFullHealthColor, healthPercent / bossLowHealthPercent);
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
    /// 添加敵人血量提示
    /// </summary>
    /// <param name="targetHealth">目標血量組件</param>
    /// <param name="healthThreshold">血量閾值（實際血量值）</param>
    /// <param name="isAboveThreshold">true: 血量高於閾值, false: 血量低於閾值</param>
    /// <param name="durationThreshold">持續時間閾值（秒）</param>
    /// <param name="hintText">提示文字</param>
    public void AddEnemyHealthHint(BaseHealth targetHealth, float healthThreshold, bool isAboveThreshold, float durationThreshold, string hintText)
    {
        var healthHint = new EnemyHealthHintData();
        healthHint.targetHealth = targetHealth;
        healthHint.healthThreshold = healthThreshold;
        healthHint.isAboveThreshold = isAboveThreshold;
        healthHint.durationThreshold = durationThreshold;
        healthHint.hintText = hintText;
        
        enemyHealthHints.Add(healthHint);
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

/// <summary>
/// 敵人血量提示數據
/// </summary>
[System.Serializable]
public class EnemyHealthHintData
{
    [Header("觸發條件")]
    public BaseHealth targetHealth;
    public float healthThreshold = 50f; // 實際血量閾值
    public bool isAboveThreshold = false; // true: 血量高於閾值, false: 血量低於閾值
    public float durationThreshold = 3f; // 持續時間閾值（秒）
    
    [Header("提示設定")]
    public string hintText = "提示：敵人血量異常！";
    public bool enabled = true;
    
    [Header("狀態")]
    private bool triggered = false; // 避免重複觸發
    private float conditionStartTime = 0f; // 條件開始時間
    private bool isConditionMet = false; // 條件是否滿足
    
    public EnemyHealthHintData()
    {
        targetHealth = null;
        healthThreshold = 50f;
        isAboveThreshold = false;
        durationThreshold = 3f;
        hintText = "提示：敵人血量異常！";
        enabled = true;
        triggered = false;
        conditionStartTime = 0f;
        isConditionMet = false;
    }
    
    /// <summary>
    /// 檢查並觸發血量提示
    /// </summary>
    public string CheckAndTriggerHint()
    {
        if (!enabled || triggered || targetHealth == null) return null;
        
        float currentHealth = targetHealth.GetCurrentHealth();
        
        bool conditionMet = isAboveThreshold ? 
            currentHealth >= healthThreshold : 
            currentHealth <= healthThreshold;
        
        if (conditionMet)
        {
            if (!isConditionMet)
            {
                // 條件剛開始滿足，記錄開始時間
                isConditionMet = true;
                conditionStartTime = Time.time;
            }
            else
            {
                // 條件持續滿足，檢查是否超過持續時間閾值
                float duration = Time.time - conditionStartTime;
                if (duration >= durationThreshold)
                {
                    triggered = true;
                    return hintText;
                }
            }
        }
        else
        {
            // 條件不滿足，重置狀態
            isConditionMet = false;
            conditionStartTime = 0f;
        }
        
        return null;
    }
    
    /// <summary>
    /// 重置觸發狀態
    /// </summary>
    public void ResetTrigger()
    {
        triggered = false;
        isConditionMet = false;
        conditionStartTime = 0f;
    }
} 