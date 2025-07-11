using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Yarn.Unity;

public class StageManager : MonoBehaviour
{
    [Header("敵人波次列表")]
    public List<EnemyWave> waves = new List<EnemyWave>();
    
    [Header("下一關卡場景名稱（留空則重載本關）")]
    public string NextStageSceneName = "";

    [Header("畫面泛白用 Volume（需有 Bloom 覆蓋）")]
    public Volume volume; // 拖入場景中的 Volume
    private Bloom bloom; // 用來操作泛白
    
    [Header("玩家設定")]
    public PlayerController playerController;

    [Header("對話系統")]
    public DialogueManager dialogueManager;

    private int currentWaveIndex = 0;
    private EnemyWave currentWave;
    private bool isStageCompleted = false;

    // PlayerPrefs 的 key
    private const string RETRY_KEY = "IsRetry";
    private const string WAVE_INDEX_KEY = "LastWaveIndex";

    private void Start()
    {
        // 檢查是否為重試
        bool isRetry = PlayerPrefs.GetInt(RETRY_KEY, 0) == 1;
        if (isRetry)
        {
            currentWaveIndex = PlayerPrefs.GetInt(WAVE_INDEX_KEY, 0);
        }

        // 取得 Volume 中的 Bloom
        if (volume != null && volume.profile.TryGet<Bloom>(out bloom))
        {
            bloom.intensity.value = 50f; // 先設為最大值
            StartCoroutine(BloomFadeIn()); // 2 秒內降到 0
        }
        else
        {
            Debug.LogWarning("Volume 未設置或未加 Bloom 覆蓋！");
        }

        // 設置玩家死亡事件監聽
        if (playerController != null && playerController.PlayerHealth != null)
        {
            playerController.PlayerHealth.onPlayerDeath.AddListener(HandlePlayerDeath);
        }

        if (waves.Count > 0)
        {
            if (isRetry)
            {
                Debug.Log($"重試：從第 {currentWaveIndex + 1} 波開始");
                // 重置重試標記
                PlayerPrefs.SetInt(RETRY_KEY, 0);
                PlayerPrefs.Save();
            }
            StartNextWave();
        }
        else
        {
            Debug.LogWarning("沒有設置任何敵人波次！");
        }
    }

    private void OnDestroy()
    {
        // 移除事件監聽
        if (playerController != null && playerController.PlayerHealth != null)
        {
            playerController.PlayerHealth.onPlayerDeath.RemoveListener(HandlePlayerDeath);
        }
    }

    private void HandlePlayerDeath()
    {
        Debug.Log("玩家死亡");
        if (dialogueManager != null)
        {
            dialogueManager.ForceTriggerDialogue("MissionFailed",OnDeathDialogueComplete);
        }
        else
        {
            StartCoroutine(BloomAndReload());
        }
    }


    private void OnDeathDialogueComplete()
    {
        // 移除事件監聽
        DialogueManager.OnDialogueEnded -= OnDeathDialogueComplete;

        // 確保 StageManager 還存在
        if (this == null || !gameObject.activeInHierarchy)
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
            return;
        }

        // 保存重試資訊
        PlayerPrefs.SetInt(RETRY_KEY, 1);
        PlayerPrefs.SetInt(WAVE_INDEX_KEY, currentWaveIndex);
        PlayerPrefs.Save();

        StartCoroutine(BloomAndReload());
    }

    private void Update()
    {
        if (isStageCompleted || currentWave == null) return;

        // 檢查當前波次是否已經結束
        if (currentWave.isWaveClear)
        {
            OnWaveCompleted();
        }
    }

    private void StartNextWave()
    {
        if (currentWaveIndex >= waves.Count)
        {
            OnStageCompleted();
            return;
        }

        currentWave = waves[currentWaveIndex];
        currentWave.gameObject.SetActive(true);
        Debug.Log($"開始第 {currentWaveIndex + 1} 波");
    }

    private void OnWaveCompleted()
    {
        Debug.Log($"第 {currentWaveIndex + 1} 波結束");
        currentWave.gameObject.SetActive(false);
        currentWaveIndex++;
        StartNextWave();
    }

    private void OnStageCompleted()
    {
        isStageCompleted = true;
        StartCoroutine(BloomAndTransition());
        Debug.Log("所有波次完成！");
    }

    private IEnumerator BloomAndTransition()
    {
        float duration = 2f;
        float timer = 0f;
        float from = 0f;
        float to = 50f;
        if (bloom != null)
        {
            while (timer < duration)
            {
                timer += Time.deltaTime;
                float value = Mathf.Lerp(from, to, timer / duration);
                bloom.intensity.value = value;
                yield return null;
            }
            bloom.intensity.value = to;
        }
        else
        {
            Debug.Log("未取得 Bloom 組件");
        }
        // 泛白完成後再轉場
        if (!string.IsNullOrEmpty(NextStageSceneName))
        {
            SceneManager.LoadScene(NextStageSceneName);
        }
        else
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }
    }

    private IEnumerator BloomFadeIn()
    {
        float duration = 2f;
        float timer = 0f;
        float from = 50f;
        float to = 0f;
        if (bloom != null)
        {
            while (timer < duration)
            {
                timer += Time.deltaTime;
                float value = Mathf.Lerp(from, to, timer / duration);
                bloom.intensity.value = value;
                yield return null;
            }
            bloom.intensity.value = to;
        }
    }

    private IEnumerator BloomAndReload()
    {
        // 確保 StageManager 還存在
        if (this == null || !gameObject.activeInHierarchy)
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
            yield break;
        }

        float duration = 2f;
        float timer = 0f;
        float from = 0f;
        float to = 50f;
        if (bloom != null)
        {
            while (timer < duration && gameObject != null && gameObject.activeInHierarchy)
            {
                timer += Time.deltaTime;
                float value = Mathf.Lerp(from, to, timer / duration);
                bloom.intensity.value = value;
                yield return null;
            }
            if (bloom != null) // 再次檢查 bloom 是否存在
            {
                bloom.intensity.value = to;
            }
        }
        else
        {
            Debug.Log("未取得 Bloom 組件");
        }
        // 重新載入當前場景
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
} 