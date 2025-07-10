using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class StageManager : MonoBehaviour
{
    [Header("敵人波次列表")]
    public List<EnemyWave> waves = new List<EnemyWave>();
    
    [Header("下一關卡場景名稱（留空則重載本關）")]
    public string NextStageSceneName = "";

    [Header("畫面泛白用 Volume（需有 Bloom 覆蓋）")]
    public Volume volume; // 拖入場景中的 Volume
    private Bloom bloom; // 用來操作泛白
    
    private int currentWaveIndex = 0;
    private EnemyWave currentWave;
    private bool isStageCompleted = false;

    private void Start()
    {
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
        if (waves.Count > 0)
        {
            StartNextWave();
        }
        else
        {
            Debug.LogWarning("沒有設置任何敵人波次！");
        }
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
} 