using UnityEngine;
using Yarn.Unity;
using System;
using System.Collections.Generic;
using System.Collections;

[DisallowMultipleComponent]
public class DialogueManager : MonoBehaviour
{
    [Header("對話系統設定")]
    [SerializeField] private DialogueRunner dialogueRunner;
    [SerializeField] private MonoBehaviour dialogueUI;

    [Header("對話觸發設定")]
    [SerializeField] private bool enableDialogueSystem = true;

    public static DialogueManager Instance { get; private set; }

    private bool isDialogueActive = false;
    private Queue<string> pendingDialogues = new Queue<string>();

    private bool isAutoReading = false;
    private float autoReadSpeed = 3f;
    private Coroutine autoReadCoroutine;

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
        if (dialogueRunner != null)
            dialogueRunner.onDialogueComplete.AddListener(OnDialogueComplete);
        else
            Debug.LogError("找不到 DialogueRunner，請確認場景中有該組件。");
    }

    /// <summary> 觸發對話（如果正在進行其他對話則排隊） </summary>
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

    /// <summary> 強制打斷當前對話並立即啟動新對話 </summary>
    public void ForceTriggerDialogue(string nodeName, Action onComplete = null)
    {
        // 1. 先打斷
        AbortDialogue(() =>
        {
            // 2. 臨時訂閱
            void TempHandler()
            {
                OnDialogueEnded = null; // 自動解除
                onComplete?.Invoke();           // 執行外部回呼
            }

            if (onComplete != null)
                OnDialogueEnded += TempHandler;

            // 3. 再觸發新對話
            TriggerDialogue(nodeName);
        });
    }


    /// <summary> 結束當前對話，清除排隊（可選擇執行後續動作） </summary>
    public void AbortDialogue(Action callback = null)
    {
        if (!isDialogueActive)
        {
            callback?.Invoke();
            return;
        }

        dialogueRunner?.Stop();
        isDialogueActive = false;
        pendingDialogues.Clear();

        OnDialogueEnded?.Invoke();
        Time.timeScale = 1f;
        StopCoroutine(autoReadCoroutine);
        callback?.Invoke();
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
        StartCoroutine(StartDialogueDelayFrame(nodeName));
    }
    IEnumerator StartDialogueDelayFrame(string nodename)
    {
        yield return null;        
        dialogueRunner.StartDialogue(nodename);
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

    public void ClearPendingDialogues() => pendingDialogues.Clear();

    // --- 自動閱讀功能 ---
    public void StartAutoReadCoroutine(float speed)
    {
        StopAutoReadCoroutine();
        autoReadSpeed = speed;
        isAutoReading = true;
        autoReadCoroutine = StartCoroutine(AutoReadRoutine());
    }

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
