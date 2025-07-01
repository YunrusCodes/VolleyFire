using UnityEngine;
using UnityEngine.Events;

public class ScoreManager : MonoBehaviour
{
    [Header("分數設定")]
    [SerializeField] private int currentScore = 0;
    [SerializeField] private int highScore = 0;
    
    [Header("事件")]
    public UnityEvent<int> OnScoreChanged;
    public UnityEvent<int> OnHighScoreChanged;
    
    private void Start()
    {
        // 載入最高分數
        LoadHighScore();
        
        // 觸發初始事件
        OnScoreChanged?.Invoke(currentScore);
        OnHighScoreChanged?.Invoke(highScore);
    }
    
    public void AddScore(int points)
    {
        currentScore += points;
        OnScoreChanged?.Invoke(currentScore);
        
        // 檢查是否破紀錄
        if (currentScore > highScore)
        {
            highScore = currentScore;
            OnHighScoreChanged?.Invoke(highScore);
            SaveHighScore();
        }
    }
    
    public void SetScore(int score)
    {
        currentScore = score;
        OnScoreChanged?.Invoke(currentScore);
    }
    
    public void ResetScore()
    {
        currentScore = 0;
        OnScoreChanged?.Invoke(currentScore);
    }
    
    public int GetCurrentScore()
    {
        return currentScore;
    }
    
    public int GetHighScore()
    {
        return highScore;
    }
    
    private void SaveHighScore()
    {
        PlayerPrefs.SetInt("HighScore", highScore);
        PlayerPrefs.Save();
    }
    
    private void LoadHighScore()
    {
        highScore = PlayerPrefs.GetInt("HighScore", 0);
    }
} 