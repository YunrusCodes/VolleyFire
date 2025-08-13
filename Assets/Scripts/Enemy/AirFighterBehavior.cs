using UnityEngine;
using System.Collections;

public class AirFighterBehavior : EnemyBehavior
{
    [Header("戰機參數")]
    public Transform[] GatlingFirePoints;
    public GameObject GatlingPrefab;
    public float gatlingFireInterval = 0.1f;
    public float gatlingBulletSpeed = 20f;
    
    [Header("移動參數")]
    public float alignSpeed = 8f;
    public float circularSpeed = 5f;
    public float returnSpeed = 10f;
    public float circleRadius = 5f;
    public float directionUpdateInterval = 3f; // 方向更新間隔
    public float maxZRotationAngle = 22.5f; // 最大z軸偏轉角度
    public float maxXRotationAngle = 11.25f; // 最大x軸偏轉角度
    
    [Header("音效")]
    public AudioClip GatlingAudio;
    private AudioSource audioSource;

    public enum FighterState { AlignAttack, CircularMove, ReturnToStart }
    public FighterState currentState;
    
    private EnemyController controller;
    private Vector3 startPosition;
    private Vector3 alignDirection; // 對齊方向
    private Vector3 circleCenter;
    private float idealAngle;      // 理想角度
    private float currentAngle;    // 當前角度
    private Vector3 idealPosition; // 理想位置
    private float fireTimer;
    private float stateTimer;
    private float directionUpdateTimer; // 方向更新計時器
    private bool isGatlingAudioPlaying = false; // 機槍音效播放狀態
    private bool isClockwiseRotation = false; // 盤旋方向：true=順時針，false=逆時針
    
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
        directionUpdateTimer = 0f;
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
        // 更新方向計時器
        directionUpdateTimer += Time.deltaTime;
        
        // 每3秒更新一次方向
        if (directionUpdateTimer >= directionUpdateInterval)
        {
            UpdateAlignDirection();
            directionUpdateTimer = 0f;
        }
        
        // 沿著對齊方向移動
        transform.position += alignDirection * alignSpeed * Time.deltaTime;
        
        // 根據位移方向計算x軸和z軸轉角，並限制最大偏轉角度
        // x轉角根據y位移程度，z轉角根據x位移程度
        float xRotation = -alignDirection.y * maxXRotationAngle;
        float zRotation = alignDirection.x * maxZRotationAngle;
        
        xRotation = Mathf.Clamp(xRotation, -maxXRotationAngle, maxXRotationAngle);
        zRotation = Mathf.Clamp(zRotation, -maxZRotationAngle, maxZRotationAngle);
        
        // 創建目標旋轉，保持當前y旋轉，改變x軸和z軸
        Quaternion currentRotation = transform.rotation;
        Quaternion targetRotation = Quaternion.Euler(xRotation, currentRotation.eulerAngles.y, zRotation);
        
        // 平滑插值到目標旋轉
        transform.rotation = Quaternion.Lerp(currentRotation, targetRotation, Time.deltaTime);
        
        // 發射機槍
        fireTimer += Time.deltaTime;
        if (fireTimer >= gatlingFireInterval)
        {
            FireGatling();
            fireTimer = 0f;
        }
        
        // 狀態切換
        if (stateTimer >= alignDuration)
        {
                    // 根據最後移動方向決定盤旋方向
        // 使用x、y方向來決定盤旋方向，讓繞行不在x、z平面
        isClockwiseRotation = (alignDirection.x < 0); // -x為順時針，+x為逆時針
            
            // 停止機槍音效
            StopGatlingAudio();
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
        
        // 根據x、y分量的大小來決定盤旋平面
        Vector3 rotationNormal;
        float xWeight = Mathf.Abs(alignDirection.x);
        float yWeight = Mathf.Abs(alignDirection.y);
        
        // 根據x、y分量的權重來混合旋轉軸
        if (xWeight > 0.01f || yWeight > 0.01f) // 避免除以零
        {
            // 計算權重比例
            float totalWeight = xWeight + yWeight;
            float xRatio = xWeight / totalWeight;
            float yRatio = yWeight / totalWeight;
            
            // 混合x軸和y軸作為旋轉軸
            rotationNormal = (Vector3.right * xRatio + Vector3.up * yRatio).normalized;
        }
        else
        {
            // 如果x、y分量都很小，使用預設的y軸
            rotationNormal = Vector3.up;
        }
        
        // 根據盤旋方向決定前進方向
        Vector3 forwardDir;
        if (isClockwiseRotation)
        {
            // 順時針：使用 -Vector3.Cross(rotationNormal, toCenter)
            forwardDir = -Vector3.Cross(rotationNormal, toCenter).normalized;
        }
        else
        {
            // 逆時針：使用原本的 Vector3.Cross(rotationNormal, toCenter)
            forwardDir = Vector3.Cross(rotationNormal, toCenter).normalized;
        }
        
        // 計算上方向，確保與盤旋平面垂直
        Vector3 upDir = Vector3.Cross(forwardDir, rotationNormal).normalized;
        
        // 使用叉積計算右方向
        Vector3 rightDir = Vector3.Cross(forwardDir, upDir).normalized;
        
        // 確保上方向完全垂直於前進方向
        upDir = Vector3.Cross(rightDir, forwardDir).normalized;
        
        // 分開處理x、z旋轉方向和y方向
        float xMovement = transform.forward.x;
        float zMovement = transform.forward.z;
        
        // 處理x、z方向的旋轉（前進方向）
        Vector3 targetForwardDir = forwardDir;
        
        // 處理y方向的旋轉（上方向）
        Vector3 targetUpDir;
        // if (zMovement > 0)
        // {
            // z位移方向 > 0，本體y軸照目前這樣lerp
            targetUpDir = upDir;
        //}
        // else
        // {
        //     // z位移方向 < 0，本體y軸與世界座標的y軸lerp
        //     targetUpDir = Vector3.up;
        // }
        
        // 分開處理x、z方向的旋轉和y方向的旋轉
        // 先lerp x、z方向（前進方向），使用原本的速度
        Vector3 currentForward = transform.forward;
        Vector3 lerpedForward = Vector3.Lerp(currentForward, targetForwardDir, Time.deltaTime * rotationLerpSpeed);
        
        // 再lerp y方向（上方向），使用Time.deltaTime * 1
        Vector3 currentUp = transform.up;
        Vector3 lerpedUp = Vector3.Lerp(currentUp, targetUpDir, Time.deltaTime * 1f);
        
        // 使用叉積計算右方向，確保三個方向向量正交
        Vector3 lerpedRight = Vector3.Cross(lerpedForward, lerpedUp).normalized;
        lerpedUp = Vector3.Cross(lerpedRight, lerpedForward).normalized; // 重新計算上方向確保正交
        
        // 創建最終的旋轉
        Quaternion finalRotation = Quaternion.LookRotation(lerpedForward, lerpedUp);
        transform.rotation = finalRotation;
        
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
        
        // 狀態切換：必須位移方向非常接近世界座標的(0,0,-1)才能離開狀態
        Vector3 worldMinusZ = new Vector3(0, 0, -1);
        float directionThreshold = 0.1f; // 方向相似度閾值
        bool isCloseToMinusZ = Vector3.Distance(transform.forward, worldMinusZ) < directionThreshold;
        
        if (stateTimer >= circleDuration && isCloseToMinusZ)
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
        transform.rotation = Quaternion.Lerp(transform.rotation, targetRotation, Time.deltaTime);
        
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
        if (Mathf.Abs(transform.position.z - startPosition.z) < 10f)
        {
            // 返回完成後切換到對齊玩家狀態
            currentState = FighterState.AlignAttack;
            SetupNextState();
        }
        else
        {
            Debug.Log(Mathf.Abs(transform.position.z - startPosition.z));
            Debug.Log(Quaternion.Angle(transform.rotation, targetRotation));
        }
    }
    
    private void SetupNextState()
    {
        stateTimer = 0f;
        
        switch (currentState)
        {
            case FighterState.AlignAttack:
                UpdateAlignDirection();
                // 開始播放機槍音效
                StartGatlingAudio();
                break;
                
                                     case FighterState.CircularMove:
                // 在transform.forward的y方向±45度和x方向±45度範圍內隨機選擇圓心點
                Vector3 currentPos = transform.position;
                
                // 獲取戰機的前方向量
                Vector3 forwardDir = transform.forward;
                
                // 在y方向±45度範圍內隨機偏移
                float yAngleOffset = Random.Range(-45f, 45f);
                Vector3 yRotatedDir = Quaternion.AngleAxis(yAngleOffset, transform.right) * forwardDir;
                
                // 在x方向±45度範圍內隨機偏移
                float xAngleOffset = Random.Range(-45f, 45f);
                Vector3 finalDir = Quaternion.AngleAxis(xAngleOffset, transform.up) * yRotatedDir;
                
                // 使用最終方向計算圓心位置
                circleCenter = currentPos - yRotatedDir * circleRadius;
                
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
    
    private void UpdateAlignDirection()
    {
        var player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            Vector3 playerPos = player.transform.position;
            // 計算到玩家上方偏移位置的方向
            Vector3 targetPos = new Vector3(playerPos.x, playerPos.y, transform.position.z);
            alignDirection = (targetPos - transform.position).normalized;
        }
        else
        {
            // 如果找不到玩家，使用預設方向
            alignDirection = Vector3.forward;
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
    
    private void StartGatlingAudio()
    {
        if (audioSource != null && GatlingAudio != null && !isGatlingAudioPlaying)
        {
            audioSource.clip = GatlingAudio;
            audioSource.loop = true;
            audioSource.Play();
            isGatlingAudioPlaying = true;
        }
    }
    
    private void StopGatlingAudio()
    {
        if (audioSource != null && isGatlingAudioPlaying)
        {
            audioSource.Stop();
            audioSource.loop = false;
            isGatlingAudioPlaying = false;
        }
    }
    
    private void FireGatling()
    {
        if (GatlingPrefab == null || GatlingFirePoints == null || GatlingFirePoints.Length == 0) return;
        
        var player = GameObject.FindGameObjectWithTag("Player");
        if (player == null) return;

        // 從所有射擊點發射子彈
        foreach (Transform firePoint in GatlingFirePoints)
        {
            if (firePoint == null) continue;
            
            Vector3 firePos = firePoint.position;
            Vector3 direction = firePoint.forward; // 朝著FirePoint的+z方向
            
            GameObject bullet = Instantiate(GatlingPrefab, firePos, Quaternion.LookRotation(direction));
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
