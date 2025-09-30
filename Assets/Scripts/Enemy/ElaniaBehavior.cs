using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class ElaniaBehavior : EnemyBehavior
{
    [Header("移動參數")]
    public float moveSpeed = 5f;
    public float turnSpeed = 3f;
    [Tooltip("距離目標點多近開始減速")]
    public float slowdownDistance = 5f;
    [Tooltip("最低速度比例 (0-1)")]
    [Range(0, 1)]
    public float minSpeedRatio = 0.2f;
    [Tooltip("加農砲發射時的水平移動速度")]
    public float cannonSideMoveSpeed = 3f;
    private int horizontalMoveDirection = 1;  // 1 = 右, -1 = 左
    [Tooltip("x: 上限, y: 下限")]
    public Vector2 boundaryX = new Vector2(8f, -8f);
    public Vector2 boundaryY = new Vector2(4f, -4f);
    
    [Header("加農砲設置")]
    public GameObject cannonRayPrefab;  // 一般加農砲
    public GameObject wormholeCannonPrefab;  // 蟲洞加農砲
    public List<Transform> cannonRaySpawnPoints = new List<Transform>();
    public List<GameObject> cannonRayInstances = new List<GameObject>();
    public float cannonFireInterval = 2f;
    private float cannonFireTimer = 0f;
    private bool useHoleCanon = false;  // 是否使用蟲洞加農砲
    public bool UseHoleCanon
    {
        get { return useHoleCanon; }
        set 
        { 
            useHoleCanon = value;
            if (mainWormhole != null)
            {
                mainWormhole.SetActive(value);
            }
        }
    }
    
    [Header("蟲洞加農砲預警線")]
    public float warningLineLength = 100f;  // 預警線長度
    public Color warningLineColor = Color.red;  // 預警線顏色
    private List<LineRenderer> warningLines = new List<LineRenderer>();  // 預警線渲染器列表
    private List<GameObject> warningTexts = new List<GameObject>();  // 預警文字列表
    public List<GameObject> wormCanonWarningObjects = new List<GameObject>();  // 蟲洞加農砲預警物件列表
    public Canvas targetCanvas;  // 目標UI Canvas
    public GameObject warningTextPrefab;  // 預警文字預製體

    [Header("導彈設置")]
    public GameObject missilePrefab;
    public GameObject missileWarningEffectPrefab;  // 導彈預警發光效果
    public List<Transform> missileSpawnPoints = new List<Transform>();
    public float missileFireInterval = 0.025f;  // 導彈發射間隔
    public float missileWarningDuration = 0.1f;   // 預警時間

    [System.Serializable]
    public class WormholePlane
    {
        public float z = 10f;  // 平面的z座標
        public Vector2 xRange = new Vector2(-8f, 8f);  // x範圍
        public Vector2 yRange = new Vector2(-4f, 4f);  // y範圍
        public Color color = new Color(1, 1, 1, 0.2f);  // 平面顯示顏色
    }

    [Header("蟲洞設置")]
    public GameObject wormholePrefab;  // 蟲洞預製體
    public GameObject wormholeMissilePrefab;  // 蟲洞飛彈預製體
    public GameObject wormholeMissileWarningEffectPrefab;  // 蟲洞飛彈預警發光效果
    public GameObject mainWormhole;    // 主蟲洞物件
    public GameObject TeleportEfftct_in;  // 發光效果預製體
    public GameObject TeleportEfftct_out;  // 發光效果預製體
    public GameObject ShinyEffect;  // 發光效果預製體
    public List<Transform> wormholeMissileWarningEffectPoints = new List<Transform>();    // 主蟲洞生成點
    public WormholePlane[] wormholePlanes = new WormholePlane[] {
        new WormholePlane { z = 5f, color = new Color(1, 0, 0, 0.2f) },  // 紅色平面
        new WormholePlane { z = 10f, color = new Color(0, 1, 0, 0.2f) }, // 綠色平面
        new WormholePlane { z = 15f, color = new Color(0, 0, 1, 0.2f) }  // 藍色平面
    };
    public float wormholeHealthThreshold = 500f;  // 觸發蟲洞生成的血量閾值
    private List<GameObject> activeWormholes = new List<GameObject>();  // 當前活躍的蟲洞
    private ElaniaHealth elaniaHealth;  // 參考到ElaniaHealth組件
    public int maxWormholeMissileCount = 8;  // 最大發射的蟲洞飛彈數量
    [Header("攻擊設置")]
    public int shotCountBeforeMove = 3;  // 發射幾次後移動
    private int currentShotCount = 0;

    private EnemyController controller;
    private Vector3 targetPosition;
    private bool isMoving = true;
    private bool movingRight = true;  // 控制水平移動方向
    private bool hasReachedTarget = false;
    
    // 加農砲狀態追蹤
    private float cannonStateTimer = 0f;   // 狀態計時器
    public bool CHARGE = false;           // 充能狀態
    public bool ACTIVE = false;           // 作用狀態
    private bool lastCHARGE = false;      // 上一次的充能狀態
    private bool lastACTIVE = false;      // 上一次的作用狀態

    public override void Init(EnemyController controller)
    {
        this.controller = controller;
        elaniaHealth = GetComponent<ElaniaHealth>();
        // 初始化加農砲實例列表
        cannonRayInstances = new List<GameObject>(new GameObject[cannonRaySpawnPoints.Count]);
        // 初始化加農砲狀態
        cannonStateTimer = 0f;
        CHARGE = false;
        ACTIVE = false;
        lastCHARGE = false;
        lastACTIVE = false;
        
        // 初始化主蟲洞狀態
        if (mainWormhole != null)
        {
            mainWormhole.SetActive(false);
        }
        
        // 初始化旋轉目標
        if (GameObject.FindGameObjectWithTag("Player") != null)
        {
            rotationTarget = GameObject.FindGameObjectWithTag("Player").transform.position;
        }
        else
        {
            rotationTarget = transform.position + transform.forward;
        }
        currentShotCount = 0;
        SetNewRandomTarget();

    }

    private Vector3 GetWormholePosition(WormholePlane plane)
    {
        // 在指定平面的範圍內隨機生成位置
        float randomX = Random.Range(plane.xRange.x, plane.xRange.y);
        float randomY = Random.Range(plane.yRange.x, plane.yRange.y);
        return new Vector3(randomX, randomY, plane.z);
    }

    private IEnumerator SpawnWormholesSequence()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null) yield break;
        if (!UseHoleCanon) UseHoleCanon = true;
        // 生成4個蟲洞，每0.25秒一個
        for (int i = 0; i < 4; i++)
        {
            // 隨機選擇一個平面
            int planeIndex = Random.Range(0, wormholePlanes.Length);
            var plane = wormholePlanes[planeIndex];

            // 在選定的平面上生成蟲洞
            Vector3 position = GetWormholePosition(plane);
            
            // 計算朝向玩家的方向
            Vector3 direction = (player.transform.position - position).normalized;
            Quaternion rotation = Quaternion.LookRotation(direction, Vector3.up);
            
            GameObject wormhole = Instantiate(wormholePrefab, position, rotation);
            activeWormholes.Add(wormhole);
            if (elaniaHealth != null)
            {
                elaniaHealth.UpdateWormholes(activeWormholes);
            }

            // 等待0.25秒
            yield return new WaitForSeconds(0.25f);
        }
    }

    private IEnumerator FireWormholeMissiles()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null || mainWormhole == null) yield break;

        // 隨機選擇不重複的蟲洞
        List<GameObject> selectedWormholes = new List<GameObject>(activeWormholes);
        System.Random rng = new System.Random();
        int n = selectedWormholes.Count;
        while (n > 1)
        {
            n--;
            int k = rng.Next(n + 1);
            GameObject temp = selectedWormholes[k];
            selectedWormholes[k] = selectedWormholes[n];
            selectedWormholes[n] = temp;
        }

        // 決定要發射的數量（8個或當前可用的最大數量）
        int fireCount = Mathf.Min(maxWormholeMissileCount, selectedWormholes.Count);
        selectedWormholes = selectedWormholes.Take(fireCount).ToList();

        // 發射點創建預警效果，並朝向主蟲洞
        Queue<GameObject> initialWarningEffects = new Queue<GameObject>();
        List<GameObject> teleportEfftcts = new List<GameObject>();
        for(int i = 0; i < fireCount; i++)
        {
            Transform spawnPoint = wormholeMissileWarningEffectPoints[i%wormholeMissileWarningEffectPoints.Count];
            // 創建預警效果並朝向主蟲洞
            GameObject warningEffect = Instantiate(wormholeMissileWarningEffectPrefab, spawnPoint.position, spawnPoint.rotation, transform);
            initialWarningEffects.Enqueue(warningEffect);
            warningEffect.transform.LookAt(mainWormhole.transform);
            float moveTime = 0f;
            float moveDuration = 0.25f; // 移動時間
            Vector3 startPos = warningEffect.transform.position;
            Vector3 endPos = mainWormhole.transform.position;
            
            while (moveTime < moveDuration)
            {
                moveTime += Time.deltaTime;
                float t = moveTime / moveDuration;
                warningEffect.transform.position = Vector3.Lerp(startPos, endPos, t);
                // 讓預警效果朝向移動方向
                if ((endPos - startPos).sqrMagnitude > 0.001f)
                {
                    warningEffect.transform.rotation = Quaternion.LookRotation((endPos - startPos).normalized);
                }
                yield return null;
            }
            GameObject teleportEfftct = Instantiate(TeleportEfftct_in, warningEffect.transform.position, warningEffect.transform.rotation);
            teleportEfftcts.Add(teleportEfftct);
            Destroy(warningEffect);
        }
        // 從每個選中的蟲洞發射飛彈
        foreach (GameObject wormhole in selectedWormholes)
        {
            if (wormhole != null)
            {
                // 計算朝向玩家的方向
                Vector3 direction = (player.transform.position - wormhole.transform.position).normalized;
                Quaternion rotation = Quaternion.LookRotation(direction);

                // 生成飛彈
                GameObject missile = Instantiate(wormholeMissilePrefab, wormhole.transform.position, rotation);
                Instantiate(TeleportEfftct_out, wormhole.transform.position, wormhole.transform.rotation);
                
                // 如果導彈有 BulletBehavior 組件，設置其方向
                var bullet = missile.GetComponent<BulletBehavior>();
                if (bullet != null)
                {
                    bullet.SetDirection(direction);
                }
            }
            yield return new WaitForSeconds(0.25f);
        }
        
        foreach (GameObject teleportEfftct in teleportEfftcts)
        {
            Destroy(teleportEfftct);
        }
    }

    public override void Tick()
    {

        if(lastACTIVE && !ACTIVE && controller.GetHealth().GetCurrentHealth() <= wormholeHealthThreshold)
        {
            UseHoleCanon = true;
            gameObject.tag = "Wormhole";
        }
        
        if (controller.GetHealth().IsDead())
        {
            foreach (var cannon in cannonRaySpawnPoints)
            {
                cannon.gameObject.SetActive(false);
            }
            OnHealthDeath();
            return;
        }
        // 保存當前狀態
        lastCHARGE = CHARGE;
        lastACTIVE = ACTIVE;

        HandleMovement();    // 處理移動
        HandleRotation();    // 處理旋轉
        HandleAttack();      // 處理攻擊
    }

    private void HandleMovement()
    {
        // 檢查狀態變化
        if (lastCHARGE && !CHARGE && !lastACTIVE && ACTIVE)
        {
            // 當 CHARGE 從 true 變成 false，且 ACTIVE 從 false 變成 true 時
            // 隨機決定移動方向
            horizontalMoveDirection = Random.value < 0.5f ? -1 : 1;
        }

        if (CHARGE)
        {
            // 充能時停止移動
            return;
        }
        else if (ACTIVE)
        {
            // 作用時水平移動
            Vector3 sideMovement = new Vector3(horizontalMoveDirection * cannonSideMoveSpeed * Time.deltaTime, 0, 0);
            transform.position += sideMovement;
        }
        else
        {
            // 非充能非作用時的移動
            if (!isMoving)
            {
                SetNewRandomTarget();
            }
            MoveToTarget();
        }
    }

    [Header("旋轉設置")]
    public float activeRotateSpeed = 1f;  // 作用時的旋轉速度
    private Vector3 rotationTarget;        // 儲存旋轉目標位置

    private void RotateTowards(Vector3 targetPosition, float rotateSpeed)
    {
        // 計算朝向目標的方向
        Vector3 direction = (targetPosition - transform.position).normalized;
        
        // 讓物件的 forward 朝向目標，up 使用世界的 Vector3.up
        Quaternion targetRotation = Quaternion.LookRotation(direction, Vector3.up);
        
        // 平滑旋轉
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotateSpeed * Time.deltaTime);
    }

    [Header("作用時的上升設置")]
    public float upwardMoveSpeed = 20f;  // 向上移動的速度

    private void HandleRotation()
    {
        if (CHARGE && !ACTIVE && !UseHoleCanon)
        {
            // 充能狀態時，更新目標為玩家位置
            rotationTarget = GameObject.FindGameObjectWithTag("Player").transform.position;
        }
        else if (!CHARGE && ACTIVE && !UseHoleCanon)
        {
            // 作用狀態時，目標點持續往上移動
            rotationTarget += Vector3.up *0.085f;
        }

        // 使用儲存的目標位置進行旋轉
        RotateTowards(rotationTarget, activeRotateSpeed);
    }
    bool isFistAttack =true;
    private void HandleAttack()
    {
        if(isFistAttack){
            StartCoroutine(FireNormalMissiles());
            isFistAttack = false;
        }
        // 檢查是否有實例存在
        bool hasInstance = false;
        foreach (var ray in cannonRayInstances)
        {
            if (ray != null && ray.activeSelf)
            {
                hasInstance = true;
                break;
            }
        }

        // 沒有實例時，重置所有狀態
        if (!hasInstance)
        {
            CHARGE = false;
            ACTIVE = false;
            cannonStateTimer = 0f;

            // 當 ACTIVE 從 true 變成 false 時，啟動飛彈檢查
            if (lastACTIVE && !ACTIVE)
            {
                StartCoroutine(DelayedMissileCheck());
            }
            // 當兩個狀態都是 false，且冷卻時間到時，創建新實例
            cannonFireTimer += Time.deltaTime;
            if (cannonFireTimer >= cannonFireInterval)
            {
                FireCannonRayAtPlayer();
                cannonFireTimer = 0f;
            }

            return;
        }

        // 有實例時的狀態處理
        if (!CHARGE && !ACTIVE)
        {
            // 新實例出現，進入充能狀態
            CHARGE = true;
            cannonStateTimer = 0f;
        }
        else if (CHARGE)
        {
            // 充能中，檢查時間
            cannonStateTimer += Time.deltaTime;
            var cannonRay = cannonRayPrefab.GetComponent<CannonRay>();
            if (cannonStateTimer >= cannonRay.GetChargingTime())
            {
                // 充能完成，進入作用狀態
                CHARGE = false;
                ACTIVE = true;
            }
        }
    }

    private void MoveToTarget()
    {
        // 計算到目標的距離
        float distance = Vector3.Distance(transform.position, targetPosition);
        
        if (distance > 0.1f)
        {
            // 計算速度係數（在減速距離內逐漸減速）
            float speedRatio = 1f;
            if (distance < slowdownDistance)
            {
                // 使用平滑的線性插值計算速度比例
                speedRatio = Mathf.Lerp(minSpeedRatio, 1f, distance / slowdownDistance);
                
                // 如果速度已經降到最低，直接找新的目標點
                if (Mathf.Approximately(speedRatio, minSpeedRatio))
                {
                    SetNewRandomTarget();
                    return;
                }
            }

            // 移動向目標（使用計算後的速度）
            Vector3 moveDirection = (targetPosition - transform.position).normalized;
            float currentSpeed = moveSpeed * speedRatio;
            transform.position += moveDirection * currentSpeed * Time.deltaTime;
        }
        else
        {
            // 到達目標點後，直接設置新的目標點
            SetNewRandomTarget();
        }
    }


    [Header("目標點設置")]
    public float minDistanceToNewTarget = 10f;  // 新目標點最小距離
    public int maxAttempts = 30;  // 最大嘗試次數

    private void SetNewRandomTarget()
    {
        Vector3 currentPos = transform.position;
        Vector3 newTarget = currentPos;
        float distance = 0f;
        int attempts = 0;

        // 嘗試找到符合距離要求的新目標點
        while (distance < minDistanceToNewTarget && attempts < maxAttempts)
        {
            float randomX = Random.Range(boundaryX.y, boundaryX.x);
            float randomY = Random.Range(boundaryY.y, boundaryY.x);
            newTarget = new Vector3(randomX, randomY, transform.position.z);
            
            distance = Vector3.Distance(currentPos, newTarget);
            attempts++;
        }

        // 如果找不到合適的點，就用最後一次生成的點
        targetPosition = newTarget;
        isMoving = true;
    }

    private void FireCannonRayAtPlayer()
    {
        if (cannonRayPrefab == null || cannonRaySpawnPoints.Count == 0) return;

        // 檢查是否可以發射新的加農砲
        bool canFire = true;
        foreach (var ray in cannonRayInstances)
        {
            if (ray != null && ray.activeSelf)
            {
                canFire = false;
                break;
            }
        }

        if (!canFire) return;  // 如果還有活躍的加農砲，不要發射新的

        // 確保 cannonRayInstances 長度正確（考慮額外的蟲洞加農砲）
        int maxCannonCount = cannonRaySpawnPoints.Count;
        if (controller.GetHealth().GetCurrentHealth() <= wormholeHealthThreshold)
        {
            maxCannonCount += 2;  // 蟲洞模式下多兩個
        }
        while (cannonRayInstances.Count < maxCannonCount)
        {
            cannonRayInstances.Add(null);
        }

        // 移除列表中已經被銷毀的蟲洞
        activeWormholes.RemoveAll(wormhole => wormhole == null);

        // 根據 UseHoleCanon 決定使用哪種加農砲
        if (UseHoleCanon)
        {
            FireWormholeCannonRay();
        }
        else
        {
            FireNormalCannonRay();
        }

        cannonStateTimer = 0f;

        // 增加射擊計數
        currentShotCount++;
        
        // 如果達到指定的射擊次數，重新選擇目標點
        if (currentShotCount >= shotCountBeforeMove)
        {
            currentShotCount = 0;
            SetNewRandomTarget();
        }
    }

    private void FireNormalCannonRay()
    {
        // 使用原始發射點
        for (int i = 0; i < cannonRaySpawnPoints.Count; i++)
        {
            Transform spawnPoint = cannonRaySpawnPoints[i];
            
            // 生成一般加農砲
            GameObject ray = Instantiate(cannonRayPrefab, spawnPoint.position, spawnPoint.rotation);
            cannonRayInstances[i] = ray;
            
            var bullet = ray.GetComponent<BulletBehavior>();
            if (bullet != null)
            {
                bullet.SetDirection(spawnPoint.forward);
            }
            
            var cannonRay = ray.GetComponent<CannonRay>();
            if (cannonRay != null)
            {
                cannonRay.SetSpawnPoint(spawnPoint);
                cannonRay.OnDestroyed += OnCannonRayDestroyed;
            }
        }
    }

    private void FireWormholeCannonRay()
    {
        
        StartCoroutine(FireNormalMissiles(4f));
        StartCoroutine(ExpandWormCanonWarningObjects(0.5f,3f));
        // 隨機選擇蟲洞
        var shuffledWormholes = new List<GameObject>(activeWormholes);
        for (int i = shuffledWormholes.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            var temp = shuffledWormholes[i];
            shuffledWormholes[i] = shuffledWormholes[j];
            shuffledWormholes[j] = temp;
        }

        // 取前面幾個蟲洞
        int wormholeCount = cannonRaySpawnPoints.Count + 2;
        for (int i = 0; i < wormholeCount && i < shuffledWormholes.Count; i++)
        {
            Transform spawnPoint = shuffledWormholes[i].transform;
            
            // 創建預警線物件
            GameObject warningLineObj = new GameObject($"WarningLine_{i}");
            warningLineObj.transform.position = spawnPoint.position;
            
            // 添加 LineRenderer 組件
            LineRenderer lineRenderer = warningLineObj.AddComponent<LineRenderer>();
            lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
            lineRenderer.startColor = warningLineColor;
            lineRenderer.endColor = warningLineColor;
            lineRenderer.startWidth = 0.1f;
            lineRenderer.endWidth = 0.1f;
            lineRenderer.positionCount = 2;
            
            // 設置線的起點和終點
            Vector3 startPoint = spawnPoint.position;
            Vector3 endPoint = startPoint + spawnPoint.forward * warningLineLength;
            lineRenderer.SetPosition(0, startPoint);
            lineRenderer.SetPosition(1, endPoint);
            
            warningLines.Add(lineRenderer);

            // 計算線與z=0平面的交點
            Vector3 direction = (endPoint - startPoint).normalized;
            float t = -startPoint.z / direction.z;
            Vector3 intersectionPoint = startPoint + direction * t;

            // 如果有Canvas和預製體，創建UI文字
            if (targetCanvas != null && warningTextPrefab != null)
            {
                // 將世界座標轉換為螢幕座標
                Vector2 screenPoint = Camera.main.WorldToScreenPoint(intersectionPoint);
                
                // 創建UI文字
                GameObject warningTextObj = Instantiate(warningTextPrefab, targetCanvas.transform);
                RectTransform rectTransform = warningTextObj.GetComponent<RectTransform>();
                
                // 將螢幕座標轉換為Canvas座標
                Vector2 canvasPosition;
                RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    targetCanvas.GetComponent<RectTransform>(),
                    screenPoint,
                    targetCanvas.worldCamera,
                    out canvasPosition
                );
                
                rectTransform.anchoredPosition = canvasPosition;
                
                // 設置追蹤器組件
                var tracker = warningTextObj.AddComponent<WarningTextTracker>();
                tracker.targetLine = lineRenderer;
                tracker.targetCanvas = targetCanvas;
                
                // 保持在 Canvas 下並啟用
                warningTextObj.SetActive(true);
                warningTexts.Add(warningTextObj);
            }

            // 生成蟲洞加農砲
            GameObject ray = Instantiate(wormholeCannonPrefab, spawnPoint.position, spawnPoint.rotation);
            cannonRayInstances[i] = ray;

            // 設置 CannonRay 組件
            var rayComponent = ray.GetComponent<CannonRay>();
            if (rayComponent != null)
            {
                rayComponent.SetSpawnPoint(spawnPoint);  // 設置參考點
                StartCoroutine(WatchCannonRayActive(rayComponent, warningLines[warningLines.Count - 1]));
                rayComponent.OnDestroyed += OnCannonRayDestroyed;
            }
            
            var bullet = ray.GetComponent<BulletBehavior>();
            if (bullet != null)
            {
                bullet.SetDirection(spawnPoint.forward);
            }
        }
        
    }

    private IEnumerator ExpandWormCanonWarningObjects(float expandTime, float delayTime)
    {
        float initialWarningObjectsZ = 0;
        float maxWarningObjectsZ = 13f;
        float warningObjectsExpandTime = expandTime;
        float warningObjectsExpandSpeed = (maxWarningObjectsZ - initialWarningObjectsZ) / warningObjectsExpandTime;
        float warningObjectsExpandTimer = 0f;
        foreach (GameObject warningObject in wormCanonWarningObjects)
        {
            warningObject.SetActive(true);
        }
        while (warningObjectsExpandTimer < warningObjectsExpandTime)
        {
            warningObjectsExpandTimer += Time.deltaTime;
            float z = initialWarningObjectsZ + warningObjectsExpandSpeed * warningObjectsExpandTimer;
            foreach (GameObject warningObject in wormCanonWarningObjects)
            {
                warningObject.transform.localScale = new Vector3(warningObject.transform.localScale.x, warningObject.transform.localScale.y, z);
            }
            yield return null;
        }
        ShinyEffect.SetActive(true);
        yield return new WaitForSeconds(delayTime);
        foreach (GameObject warningObject in wormCanonWarningObjects)
        {
            warningObject.SetActive(false);
        }
        ShinyEffect.SetActive(false);
    }
    void OnCannonRayDestroyed(GameObject ray)
    {
        // 從列表中移除被銷毀的加農砲
        int index = cannonRayInstances.IndexOf(ray);
        if (index != -1)
        {
            cannonRayInstances[index] = null;
        }
    }

    void OnDestroy()
    {
        // 清理所有事件訂閱
        foreach (var ray in cannonRayInstances)
        {
            if (ray != null)
            {
                var cannonRay = ray.GetComponent<CannonRay>();
                if (cannonRay != null)
                {
                    cannonRay.OnDestroyed -= OnCannonRayDestroyed;
                }
            }
        }

        // 清理所有預警線
        foreach (var line in warningLines)
        {
            if (line != null)
            {
                Destroy(line.gameObject);
            }
        }
        warningLines.Clear();

        // 清理所有預警文字
        foreach (var text in warningTexts)
        {
            if (text != null)
            {
                Destroy(text);
            }
        }
        warningTexts.Clear();
    }

    IEnumerator WatchCannonRayActive(CannonRay cannonRay, LineRenderer warningLine)
    {
        // 等待直到加農砲進入 active 狀態
        while (!cannonRay.IsActive())
        {
            yield return null;
        }

        // 當加農砲變為 active 時，銷毀預警線和對應的文字
        if (warningLine != null)
        {
            int index = warningLines.IndexOf(warningLine);
            if (index >= 0 && index < warningTexts.Count)
            {
                // 銷毀對應的文字
                if (warningTexts[index] != null)
                {
                    Destroy(warningTexts[index]);
                }
                warningTexts.RemoveAt(index);
            }
            
            // 銷毀預警線
            warningLines.Remove(warningLine);
            Destroy(warningLine.gameObject);
        }
    }

    private IEnumerator DelayedMissileCheck()
    {
        yield return null;

        // 根據條件發射對應的飛彈
        if (UseHoleCanon)
        {
            yield return StartCoroutine(SpawnWormholesSequence());
            // 使用蟲洞飛彈
            yield return StartCoroutine(FireWormholeMissiles());
        }
        else
        {
            // 使用一般飛彈
            yield return StartCoroutine(FireNormalMissiles());
        }
    }

    private IEnumerator FireNormalMissiles(float delayFire = 0f)
    {
        yield return new WaitForSeconds(delayFire);
        if (missilePrefab == null || missileWarningEffectPrefab == null || missileSpawnPoints.Count == 0) yield break;

        // 依序創建預警效果
        List<GameObject> warningEffects = new List<GameObject>();
        foreach (Transform spawnPoint in missileSpawnPoints)
        {
            GameObject warningEffect = Instantiate(missileWarningEffectPrefab, spawnPoint.position, spawnPoint.rotation, spawnPoint);
            warningEffects.Add(warningEffect);
            yield return new WaitForSeconds(missileFireInterval);
        }

        // 等待預警時間
        yield return new WaitForSeconds(missileWarningDuration);

        // 依序發射每個導彈
        for (int i = 0; i < missileSpawnPoints.Count; i++)
        {
            Transform spawnPoint = missileSpawnPoints[i];
            
            // 銷毀對應的預警效果
            if (warningEffects[i] != null)
            {
                Destroy(warningEffects[i]);
            }

            // 創建導彈
            GameObject missile = Instantiate(missilePrefab, spawnPoint.position, spawnPoint.rotation);
            
            // 如果導彈有 BulletBehavior 組件，設置其方向
            var bullet = missile.GetComponent<BulletBehavior>();
            if (bullet != null)
            {
                bullet.SetDirection(spawnPoint.forward);
            }

            // 等待指定時間
            yield return new WaitForSeconds(missileFireInterval);
        }

        // 確保所有預警效果都被清理
        foreach (var effect in warningEffects)
        {
            if (effect != null)
            {
                Destroy(effect);
            }
        }
    }

    void OnDrawGizmos()
    {
        // 畫出活動範圍邊界
        Vector3 min = new Vector3(boundaryX.y, boundaryY.y, transform.position.z);
        Vector3 max = new Vector3(boundaryX.x, boundaryY.x, transform.position.z);
        
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(new Vector3(min.x, min.y, min.z), new Vector3(max.x, min.y, min.z));
        Gizmos.DrawLine(new Vector3(max.x, min.y, min.z), new Vector3(max.x, max.y, min.z));
        Gizmos.DrawLine(new Vector3(max.x, max.y, min.z), new Vector3(min.x, max.y, min.z));
        Gizmos.DrawLine(new Vector3(min.x, max.y, min.z), new Vector3(min.x, min.y, min.z));
        
        // 畫出目標點
        if (!hasReachedTarget)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(targetPosition, 0.3f);
        }

        // 畫出蟲洞可生成的平面
        if (wormholePlanes != null)
        {
            foreach (var plane in wormholePlanes)
            {
                // 設置 Gizmos 顏色
                Gizmos.color = plane.color;
                
                // 計算平面的四個角點
                Vector3 p1 = new Vector3(plane.xRange.x, plane.yRange.x, plane.z);
                Vector3 p2 = new Vector3(plane.xRange.y, plane.yRange.x, plane.z);
                Vector3 p3 = new Vector3(plane.xRange.y, plane.yRange.y, plane.z);
                Vector3 p4 = new Vector3(plane.xRange.x, plane.yRange.y, plane.z);
                
                // 畫出平面邊框
                Gizmos.DrawLine(p1, p2);
                Gizmos.DrawLine(p2, p3);
                Gizmos.DrawLine(p3, p4);
                Gizmos.DrawLine(p4, p1);
                
                // 畫出一些交叉線來表示平面
                int gridSize = 5;
                for (int x = 0; x < gridSize; x++)
                {
                    float xPos = Mathf.Lerp(plane.xRange.x, plane.xRange.y, (float)x / (gridSize - 1));
                    Gizmos.DrawLine(
                        new Vector3(xPos, plane.yRange.x, plane.z),
                        new Vector3(xPos, plane.yRange.y, plane.z)
                    );
                }
                for (int y = 0; y < gridSize; y++)
                {
                    float yPos = Mathf.Lerp(plane.yRange.x, plane.yRange.y, (float)y / (gridSize - 1));
                    Gizmos.DrawLine(
                        new Vector3(plane.xRange.x, yPos, plane.z),
                        new Vector3(plane.xRange.y, yPos, plane.z)
                    );
                }
            }
        }
    }
}
