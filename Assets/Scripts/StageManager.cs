using UnityEngine;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

public class StageManager : MonoBehaviour
{
    [Header("敵人波次列表")]
    public List<EnemyWave> waves = new List<EnemyWave>();
    
    [Header("下一關卡場景名稱（留空則重載本關）")]
    public string NextStageSceneName = "";
    
    private int currentWaveIndex = 0;
    private EnemyWave currentWave;
    private bool isStageCompleted = false;

    private void Start()
    {
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
        if (!string.IsNullOrEmpty(NextStageSceneName))
        {
            SceneManager.LoadScene(NextStageSceneName);
        }
        else
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }
        Debug.Log("所有波次完成！");
    }
} 