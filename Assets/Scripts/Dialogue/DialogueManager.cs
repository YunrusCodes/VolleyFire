using UnityEngine;
using Yarn.Unity;
using System.Collections.Generic;
using System;

/// <summary>
/// 對話管理器 - 負責處理遊戲中的對話系統
/// </summary>
public class DialogueManager : MonoBehaviour
{
    [Header("對話系統設定")]
    [SerializeField] private DialogueRunner dialogueRunner;
    [SerializeField] private MonoBehaviour dialogueUI; // 使用 MonoBehaviour 避免編譯錯誤
    
    [Header("對話觸發設定")]
    [SerializeField] private bool enableDialogueSystem = true;
    
    // 對話狀態
    private bool isDialogueActive = false;
    private Queue<string> pendingDialogues = new Queue<string>();
    
    // 事件系統
    public static event Action<string> OnDialogueStarted;
    public static event Action OnDialogueEnded;
    
    // 單例模式
    public static DialogueManager Instance { get; private set; }
    
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }
        
        InitializeDialogueSystem();
    }
    
    private void InitializeDialogueSystem()
    {
        if (dialogueRunner == null)
            dialogueRunner = FindObjectOfType<DialogueRunner>();
            
        if (dialogueUI == null)
            dialogueUI = FindObjectOfType<MonoBehaviour>(); // 簡化查找邏輯
        
        if (dialogueRunner != null)
        {
            dialogueRunner.onDialogueComplete.AddListener(OnDialogueComplete);
        }
    }
    
    /// <summary>
    /// 觸發對話
    /// </summary>
    /// <param name="nodeName">對話節點名稱</param>
    public void TriggerDialogue(string nodeName)
    {
        if (!enableDialogueSystem) return;
        
        // 如果當前有對話在進行，加入等待佇列
        if (isDialogueActive)
        {
            pendingDialogues.Enqueue(nodeName);
            return;
        }
        
        StartDialogue(nodeName);
    }
    
    /// <summary>
    /// 開始播放對話
    /// </summary>
    private void StartDialogue(string nodeName)
    {
        if (dialogueRunner == null || string.IsNullOrEmpty(nodeName))
        {
            Debug.LogWarning("對話系統未正確設置或節點名稱為空");
            return;
        }
        
        isDialogueActive = true;
        OnDialogueStarted?.Invoke(nodeName);
        
        dialogueRunner.StartDialogue(nodeName);
        
        Debug.Log($"開始播放對話: {nodeName}");
    }
    
    /// <summary>
    /// 對話完成回調
    /// </summary>
    private void OnDialogueComplete()
    {
        isDialogueActive = false;
        OnDialogueEnded?.Invoke();
        
        // 恢復遊戲時間
        Time.timeScale = 1f;
        
        // 處理等待中的對話
        if (pendingDialogues.Count > 0)
        {
            var nextDialogue = pendingDialogues.Dequeue();
            StartDialogue(nextDialogue);
        }
        
        Debug.Log("對話播放完成");
    }
    
    /// <summary>
    /// 檢查是否有對話正在進行
    /// </summary>
    public bool IsDialogueActive() => isDialogueActive || pendingDialogues.Count > 0;
    
    /// <summary>
    /// 強制結束當前對話
    /// </summary>
    public void ForceEndDialogue()
    {
        if (dialogueRunner != null)
        {
            dialogueRunner.Stop();
        }
        
        // 清空等待佇列
        pendingDialogues.Clear();
        OnDialogueComplete();
    }
    
    /// <summary>
    /// 清空等待佇列
    /// </summary>
    public void ClearPendingDialogues()
    {
        pendingDialogues.Clear();
    }
} 