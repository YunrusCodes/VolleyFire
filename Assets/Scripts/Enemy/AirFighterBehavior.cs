using UnityEngine;
using System.Collections;

public class AirFighterBehavior : EnemyBehavior
{
    [Header("戰機參數")]
    public GameObject mainCannonPrefab;
    public float mainCannonSpeed = 15f;
    public float fireInterval = 0.5f;
    
    [Header("移動參數")]
    public float alignSpeed = 8f;
    public float circularSpeed = 5f;
    public float returnSpeed = 10f;
    public float circleRadius = 5f;
    
    [Header("音效")]
    public AudioClip cannonFireSfx;
    private AudioSource audioSource;

    private enum FighterState { AlignAttack, CircularMove, ReturnToStart }
    private FighterState currentState;
    
    private EnemyController controller;
    private Vector3 startPosition;
    private Vector3 alignTargetPos;
    private Vector3 circleCenter;
    private float idealAngle;      // 理想角度
    private float currentAngle;    // 當前角度
    private Vector3 idealPosition; // 理想位置
    private float fireTimer;
    private float stateTimer;
    
    public float alignDuration = 3f;
    public float circleDuration = 5f;
    public float positionLerpSpeed = 5f;  // 位置插值速度
    public float rotationLerpSpeed = 5f;  // 旋轉插值速度
    
    public override void Init(EnemyController controller)
    {
        base.Init(controller);
        this.controller = controller;
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
        
        startPosition = transform.position;
        currentState = FighterState.AlignAttack;
        fireTimer = 0f;
        stateTimer = 0f;
        SetupNextState();
    }

    public override void Tick()
    {
        if (controller.GetHealth().IsDead())
        {
            OnHealthDeath();
            return;
        }

        stateTimer += Time.deltaTime;
        
        switch (currentState)
        {
            case FighterState.AlignAttack:
                HandleAlignAttack();
                break;
            case FighterState.CircularMove:
                HandleCircularMove();
                break;
            case FighterState.ReturnToStart:
                HandleReturnToStart();
                break;
        }
    }
    
    private void HandleAlignAttack()
    {
        // 移動到對齊位置
        Vector3 currentPos = transform.position;
        Vector3 targetPos = new Vector3(alignTargetPos.x, alignTargetPos.y, currentPos.z);
        transform.position = Vector3.MoveTowards(currentPos, targetPos, alignSpeed * Time.deltaTime);
        
        // 發射主砲
        fireTimer += Time.deltaTime;
        if (fireTimer >= fireInterval)
        {
            FireMainCannon();
            fireTimer = 0f;
        }
        
        // 狀態切換
        if (stateTimer >= alignDuration)
        {
            currentState = FighterState.CircularMove;
            SetupNextState();
        }
    }
    
         private void HandleCircularMove()
    {
        // 直接往前移動
        transform.position += transform.forward * circularSpeed * Time.deltaTime;
        
        // 計算飛機的朝向
        Vector3 toCenter = (circleCenter - transform.position);
        Vector3 upDir = toCenter.normalized;
        Vector3 forwardDir = Vector3.Cross(Vector3.up, toCenter).normalized;
        
        // 使用叉積計算右方向
        Vector3 rightDir = Vector3.Cross(forwardDir, upDir).normalized;
        
        // 確保上方向完全垂直於前進方向
        upDir = Vector3.Cross(rightDir, forwardDir).normalized;
        
        // 創建目標旋轉
        Quaternion targetRotation = Quaternion.LookRotation(forwardDir, upDir);
        
        // 平滑插值到目標旋轉
        transform.rotation = Quaternion.Lerp(transform.rotation, targetRotation, Time.deltaTime * rotationLerpSpeed);
        
        // 繪製debug線條
        Debug.DrawLine(transform.position, circleCenter, Color.yellow); // 當前位置到圓心的連線
        Debug.DrawLine(transform.position, idealPosition, Color.magenta); // 當前位置到理想位置的連線
        Debug.DrawLine(transform.position, startPosition, Color.blue); // 當前位置到起始位置的連線
        Debug.DrawRay(circleCenter, Vector3.up * circleRadius, Color.red); // 顯示圓心位置和半徑
        Debug.DrawRay(circleCenter, Vector3.right * circleRadius, Color.red);
        Debug.DrawRay(circleCenter, Vector3.forward * circleRadius, Color.red);
        Debug.DrawRay(transform.position, forwardDir * 2f, Color.green); // 前進方向
        Debug.DrawRay(transform.position, upDir * 2f, Color.white); // 上方向
        Debug.DrawRay(transform.position, rightDir * 2f, Color.cyan); // 右方向
        
        // 狀態切換
        if (stateTimer >= circleDuration)
        {
            currentState = FighterState.ReturnToStart;
            SetupNextState();
        }
    }
    
    private void HandleReturnToStart()
    {
        // 一直往自身前方移動
        transform.position += transform.forward * returnSpeed * Time.deltaTime;

        // 計算並平滑過渡到起始角度
        Quaternion targetRotation = Quaternion.Euler(0, -180, 0); // 起始角度
        transform.rotation = Quaternion.Lerp(transform.rotation, targetRotation, Time.deltaTime * rotationLerpSpeed);
        
        // 檢查是否需要修正z座標
        float currentZ = transform.position.z;
        if (currentZ < startPosition.z)
        {
            // 如果超過了起始平面，修正回來
            Vector3 correctedPos = transform.position;
            correctedPos.z = Mathf.Lerp(currentZ, startPosition.z, Time.deltaTime * positionLerpSpeed);
            transform.position = correctedPos;
        }
        
        // 繪製debug線條
        Debug.DrawRay(transform.position, transform.forward * 2f, Color.green); // 前進方向
        Debug.DrawRay(transform.position, transform.up * 2f, Color.white); // 上方向
        Debug.DrawRay(transform.position, transform.right * 2f, Color.cyan); // 右方向
        Debug.DrawLine(transform.position, new Vector3(transform.position.x, transform.position.y, startPosition.z), Color.yellow); // 到目標平面的距離
        
        // 檢查是否返回完成
        if (Mathf.Abs(transform.position.z - startPosition.z) < 0.1f && 
            Quaternion.Angle(transform.rotation, targetRotation) < 1f)
        {
            // 返回完成後切換到對齊玩家狀態
            currentState = FighterState.AlignAttack;
            SetupNextState();
        }
    }
    
    private void SetupNextState()
    {
        stateTimer = 0f;
        
        switch (currentState)
        {
            case FighterState.AlignAttack:
                alignTargetPos = GetPlayerAlignPosition();
                break;
                
                         case FighterState.CircularMove:
                 // 在自身-z方向（後方）選擇圓心點
                 Vector3 currentPos = transform.position;
                 
                 // 使用transform.forward獲取物體的前方向量，取反得到後方
                 // 固定使用circleRadius作為圓心距離
                 circleCenter = currentPos - transform.forward * circleRadius;
                 
                 // 設置初始角度和位置
                 idealAngle = 0f;
                 currentAngle = 0f;
                 idealPosition = currentPos;
                 break;
                
            case FighterState.ReturnToStart:
                // 不需要特別設置
                break;
        }
    }
    
    private Vector3 GetPlayerAlignPosition()
    {
        var player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            Vector3 playerPos = player.transform.position;
            // 在玩家上方稍微偏移的位置
            return new Vector3(playerPos.x, playerPos.y + 2f, transform.position.z);
        }
        return transform.position;
    }
    
    private void FireMainCannon()
    {
        if (mainCannonPrefab == null) return;
        
        if (audioSource != null && cannonFireSfx != null)
        {
            audioSource.PlayOneShot(cannonFireSfx);
        }
        
        var player = GameObject.FindGameObjectWithTag("Player");
        if (player == null) return;

        Vector3 firePos = transform.position;
        Vector3 targetPos = player.transform.position;
        Vector3 direction = (targetPos - firePos).normalized;
        
        GameObject cannon = Instantiate(mainCannonPrefab, firePos, Quaternion.LookRotation(direction));
        var bullet = cannon.GetComponent<BulletBehavior>();
        if (bullet != null)
        {
            bullet.SetDirection(direction);
            bullet.SetSpeed(mainCannonSpeed);
        }
    }
    
    public override void OnWaveMove()
    {
        // 波次移動階段實作
    }
    
    public override void OnWaveStart()
    {
        // 記錄起始位置
        startPosition = transform.position;
    }
}
