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
    
    // 自動閱讀相關
    private bool isAutoReading = false;
    private float autoReadSpeed = 3f;
    private System.Collections.IEnumerator autoReadCoroutine;
    
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
        {
            dialogueRunner = FindObjectOfType<DialogueRunner>();
            Debug.Log($"找到 DialogueRunner: {(dialogueRunner != null ? "成功" : "失敗")}");
        }
            
        if (dialogueUI == null)
        {
            dialogueUI = FindObjectOfType<MonoBehaviour>(); // 簡化查找邏輯
            Debug.Log($"找到 DialogueUI: {(dialogueUI != null ? "成功" : "失敗")}");
        }
        
        if (dialogueRunner != null)
        {
            dialogueRunner.onDialogueComplete.AddListener(OnDialogueComplete);
            Debug.Log("DialogueRunner 事件監聽器已設置");
        }
        else
        {
            Debug.LogError("無法找到 DialogueRunner！請確保場景中有 DialogueRunner 組件。");
        }
    }
    
    /// <summary>
    /// 觸發對話
    /// </summary>
    /// <param name="nodeName">對話節點名稱</param>
    public void TriggerDialogue(string nodeName)
    {
        Debug.Log($"DialogueManager.TriggerDialogue 被調用: {nodeName}");
        Debug.Log($"enableDialogueSystem: {enableDialogueSystem}");
        Debug.Log($"isDialogueActive: {isDialogueActive}");
        
        if (!enableDialogueSystem) 
        {
            Debug.LogWarning("對話系統被禁用！");
            return;
        }
        
        // 如果當前有對話在進行，加入等待佇列
        if (isDialogueActive)
        {
            Debug.Log($"當前有對話在進行，將 {nodeName} 加入等待佇列");
            pendingDialogues.Enqueue(nodeName);
            return;
        }
        
        StartDialogue(nodeName);
    }
    
    /// <summary>
    /// 強制觸發對話（會結束當前對話）
    /// </summary>
    /// <param name="nodeName">對話節點名稱</param>
    public void ForceTriggerDialogue(string nodeName)
    {
        Debug.Log($"DialogueManager.ForceTriggerDialogue 被調用: {nodeName}");
        
        if (!enableDialogueSystem) 
        {
            Debug.LogWarning("對話系統被禁用！");
            return;
        }
        
        // 如果當前有對話在進行，強制結束
        if (isDialogueActive)
        {
            Debug.Log($"強制結束當前對話，開始新對話: {nodeName}");
            ForceEndDialogue();
        }
        
        StartDialogue(nodeName);
    }
    
    /// <summary>
    /// 開始播放對話
    /// </summary>
    private void StartDialogue(string nodeName)
    {
        Debug.Log($"嘗試開始對話: {nodeName}");
        Debug.Log($"DialogueRunner: {(dialogueRunner != null ? "存在" : "不存在")}");
        Debug.Log($"節點名稱: {(string.IsNullOrEmpty(nodeName) ? "為空" : nodeName)}");
        
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
    
    /// <summary>
    /// 開始自動閱讀協程
    /// </summary>
    /// <param name="speed">自動閱讀間隔（秒）</param>
    public void StartAutoReadCoroutine(float speed)
    {
        if (isAutoReading)
        {
            // 如果已經在自動閱讀，停止當前的協程
            StopAutoReadCoroutine();
        }
        
        autoReadSpeed = speed;
        isAutoReading = true;
        autoReadCoroutine = AutoReadCoroutine();
        StartCoroutine(autoReadCoroutine);
        
        Debug.Log($"開始自動閱讀，間隔: {speed} 秒");
    }
    
    /// <summary>
    /// 停止自動閱讀協程
    /// </summary>
    public void StopAutoReadCoroutine()
    {
        if (autoReadCoroutine != null)
        {
            StopCoroutine(autoReadCoroutine);
            autoReadCoroutine = null;
        }
        isAutoReading = false;
        Debug.Log("停止自動閱讀");
    }
    
    /// <summary>
    /// 自動閱讀協程
    /// </summary>
    private System.Collections.IEnumerator AutoReadCoroutine()
    {
        Debug.Log("自動閱讀協程開始");
        
        while (isAutoReading && isDialogueActive)
        {
            yield return new WaitForSeconds(autoReadSpeed);
            
            Debug.Log($"自動閱讀檢查 - isAutoReading: {isAutoReading}, isDialogueActive: {isDialogueActive}");
            
            // 檢查對話是否仍在進行
            if (isDialogueActive)
            {
                // 找到 DialogueRunner 實例
                var dialogueRunner = FindObjectOfType<DialogueRunner>();
                if (dialogueRunner != null && dialogueRunner.IsDialogueRunning)
                {
                    // 請求下一行對話
                    dialogueRunner.RequestNextLine();
                    Debug.Log("自動閱讀：請求下一行對話");
                }
                else
                {
                    // 對話已結束，停止自動閱讀
                    Debug.Log("對話已結束，停止自動閱讀");
                    isAutoReading = false;
                    isDialogueActive = false; // 確保狀態正確
                    break;
                }
            }
            else
            {
                // 對話已結束，停止自動閱讀
                Debug.Log("isDialogueActive 為 false，停止自動閱讀");
                isAutoReading = false;
                break;
            }
        }
        
        isAutoReading = false;
        Debug.Log("自動閱讀協程結束");
    }
    
    /// <summary>
    /// 檢查是否正在自動閱讀
    /// </summary>
    public bool IsAutoReading() => isAutoReading;
} 