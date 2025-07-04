using UnityEngine;
using System.Collections.Generic;

public class EnemyWave : MonoBehaviour
{
    [Header("敵人清單")]
    public List<EnemyController> enemies = new List<EnemyController>();
    [Header("目標位置")]
    public Transform targetPosition;
    public float moveSpeed = 5f;

    private bool isMoving = true;
    private bool isWaveActive = false;
    public bool isWaveClear { get; private set; } = false;

    private void Start()
    {
        foreach (var enemy in enemies)
            enemy.OnWaveMove();
    }

    private void Update()
    {
        if (isMoving && targetPosition != null)
        {
            // 父物件移動
            transform.position = Vector3.MoveTowards(transform.position, targetPosition.position, moveSpeed * Time.deltaTime);
            foreach (var enemy in enemies)
                enemy.OnWaveMove();
            if (Vector3.Distance(transform.position, targetPosition.position) < 0.1f)
            {
                isMoving = false;
                isWaveActive = true;
                foreach (var enemy in enemies)
                    enemy.OnWaveStart();
            }
        }
        else if (isWaveActive)
        {
            bool waveClear = true;
            foreach (var enemy in enemies)
            {
                enemy.WaveProcessing();
                waveClear = waveClear && enemy.GetHealth().IsDead();
            }
                
            if (waveClear)
            {
                Debug.Log("Wave cleared!");
                isWaveClear = true;
            }
        }
    }
} 