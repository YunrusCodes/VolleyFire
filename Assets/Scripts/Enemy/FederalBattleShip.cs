using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class FederalBattleShip : EnemyBehavior
{
    [Header("戰艦參數")]
    public GameObject missilePrefab;
    public GameObject cannonRayPrefab;
    public float fireInterval = 3f;
    public float missileSpeed = 8f;
    
    [Header("發射點設置")]
    public List<Transform> missileSpawnPoints = new List<Transform>();
    public List<Transform> cannonRaySpawnPoints = new List<Transform>();

    private EnemyController controller;
    private Vector3 alignTargetPos;
    private float alignLerpSpeed = 8f;

    private enum BattleShipState { Patrol, Attack }
    private BattleShipState currentState = BattleShipState.Patrol;
    private Vector3 patrolTargetPos;
    private float patrolMoveSpeed = 3f;
    private float patrolDuration = 2f;
    private float patrolTimer = 0f;
    private Vector3 patrolMoveDir;
    private System.Action onPatrolComplete;
    private bool isMovingToAlign = false;
    private bool useRandomPatrol = true; // 交替用：true=隨機方向，false=朝玩家
    private bool useCannonRay = false;   // 交替使用飛彈和雷射

    public override void Init(EnemyController controller)
    {
        this.controller = controller;
        useRandomPatrol = true; // 初始為隨機方向
        useCannonRay = false;   // 初始為飛彈
        SetNextPatrolTarget();
        patrolTimer = 0f;
        onPatrolComplete = () => { isMovingToAlign = true; currentState = BattleShipState.Attack; };
        currentState = BattleShipState.Patrol;
    }

    public override void Tick()
    {
        if(!controller.GetHealth().IsHurt()){Debug.Log("戰艦未受傷"); return;}

        if (controller.GetHealth().IsDead())
        {
            Debug.Log("戰艦已被擊毀");
            gameObject.SetActive(false);
            return;
        }

        if (currentState == BattleShipState.Patrol)
        {
            PatrolMove();
            return;
        }

        if (currentState == BattleShipState.Attack)
        {
            if (isMovingToAlign)
            {
                MoveToAlignWithPlayer();
            }
            return;
        }
    }

    private void FireMissileAtPlayer()
    {
        if (missilePrefab == null || missileSpawnPoints.Count == 0) return;

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

        // 從每個發射點發射一道雷射
        foreach (Transform spawnPoint in cannonRaySpawnPoints)
        {
            // 使用發射點的位置和方向
            GameObject ray = Instantiate(cannonRayPrefab, spawnPoint.position, spawnPoint.rotation);
            var bullet = ray.GetComponent<BulletBehavior>();
            if (bullet != null)
            {
                bullet.SetDirection(spawnPoint.forward);
            }
            // 設置發射點
            var cannonRay = ray.GetComponent<CannonRay>();
            if (cannonRay != null)
            {
                cannonRay.SetSpawnPoint(spawnPoint);
            }
        }
    }

    private void MoveToAlignWithPlayer()
    {
        transform.position = Vector3.Lerp(transform.position, alignTargetPos, alignLerpSpeed * Time.deltaTime);
        if (Vector3.Distance(transform.position, alignTargetPos) < 0.05f)
        {
            transform.position = alignTargetPos;
            isMovingToAlign = false;
            StartCoroutine(FireWeaponBurst());
        }
    }

    private void SetNextPatrolTarget()
    {
        if (useRandomPatrol)
        {
            // 隨機單位向量，但限制在上半部分移動
            float angle = Random.Range(-45f, 45f) * Mathf.Deg2Rad; // 限制在 -45 到 45 度之間
            patrolMoveDir = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f).normalized;
        }
        else
        {
            // 朝玩家
            var player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                patrolTargetPos = new Vector3(player.transform.position.x, player.transform.position.y + 2f, transform.position.z);
                patrolMoveDir = (patrolTargetPos - transform.position).normalized;
            }
            else
            {
                patrolMoveDir = Vector3.right; // 預設右
            }
        }
    }

    private void PatrolMove()
    {
        transform.position += patrolMoveDir * patrolMoveSpeed * Time.deltaTime;
        patrolTimer += Time.deltaTime;
        if (patrolTimer >= patrolDuration)
        {
            currentState = BattleShipState.Attack;
            alignTargetPos = transform.position;
            onPatrolComplete?.Invoke();
            onPatrolComplete = null;
            useRandomPatrol = !useRandomPatrol; // 交替巡邏方式
        }
    }

    private IEnumerator FireWeaponBurst()
    {
        // 根據當前武器類型發射
        if (useCannonRay)
        {
            if (cannonRaySpawnPoints.Count == 0)
            {
                Debug.LogWarning("FederalBattleShip: 沒有設置雷射發射點！");
                yield break;
            }
            FireCannonRayAtPlayer();
        }
        else
        {
            if (missileSpawnPoints.Count == 0)
            {
                Debug.LogWarning("FederalBattleShip: 沒有設置飛彈發射點！");
                yield break;
            }
            FireMissileAtPlayer();
        }

        yield return new WaitForSeconds(0.2f);

        // 攻擊結束後回到巡邏
        SetNextPatrolTarget();
        patrolTimer = 0f;
        onPatrolComplete = () => { isMovingToAlign = true; currentState = BattleShipState.Attack; };
        currentState = BattleShipState.Patrol;
        useCannonRay = !useCannonRay; // 交替武器類型
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