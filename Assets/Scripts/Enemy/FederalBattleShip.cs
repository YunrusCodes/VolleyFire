using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class FederalBattleShip : EnemyBehavior
{
    [Header("戰艦參數")]
    public GameObject missilePrefab;
    public GameObject cannonRayPrefab;
    public float fireInterval = 3f;
    public float missileSpeed = 8f;
    [Header("音效")]
    public AudioClip missileFireSfx;
    private AudioSource missileAudioSource;
    
    [Header("發射點設置")]
    public List<Transform> missileSpawnPoints = new List<Transform>();
    public List<Transform> cannonRaySpawnPoints = new List<Transform>();
    private List<GameObject> cannonRayInstances = new List<GameObject>();

    private EnemyController controller;
    private Vector3 alignTargetPos;
    public float alignSpeed = 10f;

    private enum BattleShipState { Patrol, AlignToPlayer }
    private BattleShipState currentState = BattleShipState.Patrol;
    private float patrolMoveSpeed = 3f;
    public float patrolDuration = 5f;
    private float patrolTimer = 0f;
    private Vector3 patrolMoveDir;
    private float fireTimer = 0f;
    public float alignDuration = 3f; // 對齊狀態持續時間
    private float alignTimer = 0f;

    [Header("攻擊間隔")]
    [SerializeField] private float missileFireInterval = 3f;
    [SerializeField] private float cannonFireInterval = 2f;
    private float missileFireTimer = 0f;
    public float cannonFireTimer = 0f;

    private List<Vector3> patrolDirectionsQueue = new List<Vector3>();
    private int patrolDirectionIndex = 0;

    public override void Init(EnemyController controller)
    {
        this.controller = controller;
        // 嘗試從子物件或自身取得 AudioSource
        var audioSources = GetComponentsInChildren<AudioSource>();
        if (audioSources.Length >= 1)
        {
            missileAudioSource = audioSources[0];
        }
        else
        {
            missileAudioSource = gameObject.AddComponent<AudioSource>();
        }
        SetNextPatrolTarget();
        patrolTimer = 0f;
        currentState = BattleShipState.Patrol;
        missileFireTimer = 0f;
        cannonFireTimer = 0f;
        // 初始化 cannonRayInstances
        cannonRayInstances = new List<GameObject>(new GameObject[cannonRaySpawnPoints.Count]);
        controller.GetHealth().ResetHealth();
    }

    public override void Tick()
    {
        if(!controller.GetHealth().IsHurt()){Debug.Log("戰艦未受傷"); return;}

        if (controller.GetHealth().IsDead())
        {
            Debug.Log("戰艦已被擊毀");
            foreach(var cannon in cannonRaySpawnPoints)
            {
                cannon.gameObject.SetActive(false);
            }
            gameObject.SetActive(false);
            return;
        }

        // 取得目前血量
        float currentHp = controller.GetHealth().GetCurrentHealth();

        if (currentState == BattleShipState.Patrol)
        {
            PatrolMove(currentHp);
            missileFireTimer += Time.deltaTime;
            if (missileFireTimer >= missileFireInterval)
            {
                FireMissileAtPlayer();
                missileFireTimer = 0f;
            }
            return;
        }

        if (currentState == BattleShipState.AlignToPlayer)
        {
            // 血量高於等於250時不能進入加農砲狀態，強制切回巡邏
            if (currentHp >= 250f)
            {
                currentState = BattleShipState.Patrol;
                SetNextPatrolTarget();
                return;
            }
            alignTimer += Time.deltaTime;
            if (alignTimer < alignDuration )
            {
                alignTargetPos = GetPlayerAlignPos(); // 前1/4持續修正
            }
            // 後半段不再修正 alignTargetPos
            MoveToAlignWithPlayer();
            cannonFireTimer += Time.deltaTime;
            if (cannonFireTimer >= cannonFireInterval)
            {
                FireCannonRayAtPlayer();
                cannonFireTimer = 0f;
            }
            if (alignTimer >= alignDuration)
            {
                currentState = BattleShipState.Patrol;
                SetNextPatrolTarget();
            }
            return;
        }
    }

    private void FireMissileAtPlayer()
    {
        if (missilePrefab == null || missileSpawnPoints.Count == 0) return;

        if (missileAudioSource != null && missileFireSfx != null)
        {
            missileAudioSource.PlayOneShot(missileFireSfx);
        }
        // 從每個發射點發射一顆飛彈
        foreach (Transform spawnPoint in missileSpawnPoints)
        {
            // 使用發射點的位置和方向
            GameObject missile = Instantiate(missilePrefab, spawnPoint.position, spawnPoint.rotation);
            var bullet = missile.GetComponent<BulletBehavior>();
            if (bullet != null)
            {
                // 使用發射點的前方向量作為飛彈方向
                bullet.SetDirection(spawnPoint.forward);
                bullet.SetSpeed(missileSpeed);
            }
        }
    }

    private void FireCannonRayAtPlayer()
    {
        if (cannonRayPrefab == null || cannonRaySpawnPoints.Count == 0) return;

        // 確保 cannonRayInstances 長度正確
        while (cannonRayInstances.Count < cannonRaySpawnPoints.Count)
            cannonRayInstances.Add(null);
        for (int i = 0; i < cannonRaySpawnPoints.Count; i++)
        {
            Transform spawnPoint = cannonRaySpawnPoints[i];
            GameObject ray = cannonRayInstances[i];
            if (ray == null || !ray.activeSelf)
            {
                ray = Instantiate(cannonRayPrefab, spawnPoint.position, spawnPoint.rotation);
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
                }
            }
            // 若物件還在 active 狀態則不重複生成
        }
    }

    private void MoveToAlignWithPlayer()
    {
        Debug.Log(alignTargetPos.x+" "+alignTargetPos.y);
        Vector3 current = transform.position;
        Vector3 target = new Vector3(alignTargetPos.x, alignTargetPos.y, current.z); // Z 不變
        Vector2 current2D = new Vector2(current.x, current.y);
        Vector2 target2D = new Vector2(target.x, target.y);
        Vector2 dir = (target2D - current2D).normalized;
        float distance = Vector2.Distance(current2D, target2D);
        float moveStep = alignSpeed * Time.deltaTime;
        if (distance <= moveStep)
        {
            transform.position = target;
            currentState = BattleShipState.Patrol;
            SetNextPatrolTarget();
        }
        else
        {
            Vector2 newPos2D = current2D + dir * moveStep;
            transform.position = new Vector3(newPos2D.x, newPos2D.y, current.z);
        }
    }

    private void SetNextPatrolTarget()
    {
        float currentHp = controller != null && controller.GetHealth() != null ? controller.GetHealth().GetCurrentHealth() : 9999f;
        // 血量低於250時，巡邏方向改為朝向玩家
        if (currentHp < 250f)
        {
            var player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                Vector3 toPlayer = player.transform.position - transform.position;
                patrolMoveDir = new Vector3(toPlayer.x, toPlayer.y, 0f).normalized;
            }
            else
            {
                patrolMoveDir = Vector3.right;
            }
            return;
        }
        // 否則維持原本八次隨機分布
        if (patrolDirectionsQueue.Count == 0 || patrolDirectionIndex >= patrolDirectionsQueue.Count)
        {
            patrolDirectionsQueue = new List<Vector3>
            {
                Vector3.up, Vector3.up,
                Vector3.down, Vector3.down,
                Vector3.left, Vector3.left,
                Vector3.right, Vector3.right
            };
            // 洗牌
            patrolDirectionsQueue = patrolDirectionsQueue.OrderBy(x => Random.value).ToList();
            patrolDirectionIndex = 0;
        }
        patrolMoveDir = patrolDirectionsQueue[patrolDirectionIndex];
        patrolDirectionIndex++;
    }

    private void PatrolMove(float currentHp)
    {
        transform.position += patrolMoveDir * patrolMoveSpeed * Time.deltaTime;
        patrolTimer += Time.deltaTime;
        if (patrolTimer >= patrolDuration)
        {
            patrolTimer = 0f;
            SetNextPatrolTarget(); // 每隔 patrolDuration 秒隨機換方向

            // 只有血量低於250時才切換到加農砲狀態，否則繼續巡邏
            if (currentHp < 250f)
            {
                currentState = BattleShipState.AlignToPlayer;
                alignTargetPos = GetPlayerAlignPos();
                alignTimer = 0f;
                FireCannonRayAtPlayer(); // 先發射雷射砲
                return;
            }
        }
    }

    private Vector3 GetPlayerAlignPos()
    {
        var player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            Vector3 playerPos = player.transform.position;
            Vector3 dirToPlayer = (playerPos - transform.position).normalized;
            return new Vector3(playerPos.x + dirToPlayer.x * 0.2f, playerPos.y + 2f + dirToPlayer.y * 0.2f, transform.position.z);
        }
        return transform.position;
    }

    public override void OnWaveMove()
    {
        // 波次移動階段，可加特效或待機動畫
    }

    public override void OnWaveStart()
    {
        // 波次開始行動階段，可加初始化或特效
    }

    private void OnValidate()
    {
        // 在編輯器中檢查發射點設置
        if (missileSpawnPoints.Count == 0)
        {
            Debug.LogWarning("FederalBattleShip: 請設置飛彈發射點！");
        }
        if (cannonRaySpawnPoints.Count == 0)
        {
            Debug.LogWarning("FederalBattleShip: 請設置雷射發射點！");
        }
    }
} 