using UnityEngine;
using Yarn.Unity;
using System;
using System.Collections.Generic;

[DisallowMultipleComponent]
public class DialogueManager : MonoBehaviour
{
    [Header("對話系統設定")]
    [SerializeField] private DialogueRunner dialogueRunner;
    [SerializeField] private MonoBehaviour dialogueUI; // 避免類型限制錯誤

    [Header("對話觸發設定")]
    [SerializeField] private bool enableDialogueSystem = true;

    // 單例
    public static DialogueManager Instance { get; private set; }

    // 狀態
    private bool isDialogueActive = false;
    private Queue<string> pendingDialogues = new Queue<string>();

    // 自動閱讀
    private bool isAutoReading = false;
    private float autoReadSpeed = 3f;
    private Coroutine autoReadCoroutine;

    // 事件
    public static event Action<string> OnDialogueStarted;
    public static event Action OnDialogueEnded;

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        InitializeDialogueSystem();
    }

    private void InitializeDialogueSystem()
    {
        if (dialogueRunner == null)
            dialogueRunner = FindObjectOfType<DialogueRunner>();

        if (dialogueUI == null)
            dialogueUI = FindObjectOfType<MonoBehaviour>();

        if (dialogueRunner != null)
            dialogueRunner.onDialogueComplete.AddListener(OnDialogueComplete);
        else
            Debug.LogError("找不到 DialogueRunner，請確認場景中有該組件。");
    }

    /// <summary> 觸發對話 </summary>
    public void TriggerDialogue(string nodeName)
    {
        if (!enableDialogueSystem)
        {
            Debug.LogWarning("對話系統已被禁用");
            return;
        }

        if (isDialogueActive)
        {
            pendingDialogues.Enqueue(nodeName);
            return;
        }

        StartDialogue(nodeName);
    }

    /// <summary> 強制觸發（結束目前對話） </summary>
    public void ForceTriggerDialogue(string nodeName)
    {
        if (!enableDialogueSystem)
        {
            Debug.LogWarning("對話系統已被禁用");
            return;
        }

        if (isDialogueActive)
            ForceEndDialogue();

        StartDialogue(nodeName);
    }

    private void StartDialogue(string nodeName)
    {
        if (dialogueRunner == null || string.IsNullOrEmpty(nodeName))
        {
            Debug.LogWarning("對話啟動失敗：runner 未設置或節點為空");
            return;
        }

        isDialogueActive = true;
        OnDialogueStarted?.Invoke(nodeName);
        dialogueRunner.StartDialogue(nodeName);
    }

    private void OnDialogueComplete()
    {
        isDialogueActive = false;
        OnDialogueEnded?.Invoke();
        Time.timeScale = 1f;

        if (pendingDialogues.Count > 0)
            StartDialogue(pendingDialogues.Dequeue());
    }

    public bool IsDialogueActive() => isDialogueActive || pendingDialogues.Count > 0;

    public void ForceEndDialogue()
    {
        dialogueRunner?.Stop();
        pendingDialogues.Clear();
        OnDialogueComplete();
    }

    public void ClearPendingDialogues() => pendingDialogues.Clear();

    /// <summary> 啟動自動閱讀 </summary>
    public void StartAutoReadCoroutine(float speed)
    {
        StopAutoReadCoroutine();
        autoReadSpeed = speed;
        isAutoReading = true;
        autoReadCoroutine = StartCoroutine(AutoReadRoutine());
    }

    /// <summary> 停止自動閱讀 </summary>
    public void StopAutoReadCoroutine()
    {
        if (autoReadCoroutine != null)
        {
            StopCoroutine(autoReadCoroutine);
            autoReadCoroutine = null;
        }

        isAutoReading = false;
    }

    private System.Collections.IEnumerator AutoReadRoutine()
    {
        while (isAutoReading && isDialogueActive)
        {
            yield return new WaitForSeconds(autoReadSpeed);

            if (dialogueRunner != null && dialogueRunner.IsDialogueRunning)
            {
                dialogueRunner.RequestNextLine();
            }
            else
            {
                break;
            }
        }

        isAutoReading = false;
    }

    public bool IsAutoReading() => isAutoReading;
}
