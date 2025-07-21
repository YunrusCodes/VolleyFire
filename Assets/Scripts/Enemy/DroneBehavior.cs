using UnityEngine;
using System.Collections;

public class DroneBehavior : EnemyBehavior
{
    [Header("飛彈參數")]
    public GameObject missilePrefab;
    public float fireInterval = 2f;
    public float missileSpeed = 10f;
    [Header("音效")]
    public AudioClip missileFireSfx;
    private AudioSource audioSource;

    private EnemyController controller;
    private Vector3 alignTargetPos;
    private float alignLerpSpeed = 10f;

    private enum DroneState { Swing, Fire }
    private DroneState currentState = DroneState.Swing;
    private Vector3 swingTargetPos;
    private float swingMoveSpeed = 5f;
    private float swingDuration = 1f;
    private float swingTimer = 0f;
    private Vector3 swingMoveDir;
    private System.Action onSwingComplete;
    private bool isMovingToAlign = false;
    private bool useRandomSwing = true; // 交替用：true=隨機方向，false=朝玩家

    public override void Init(EnemyController controller)
    {
        base.Init(controller);
        this.controller = controller;
        audioSource = GetComponent<AudioSource>();
        useRandomSwing = true; // 初始為隨機方向
        SetNextSwingTarget();
        swingTimer = 0f;
        onSwingComplete = () => { isMovingToAlign = true; currentState = DroneState.Fire; };
        currentState = DroneState.Swing;
    }

    public override void Tick()
    {
        if (controller.GetHealth().IsDead())
        {
            OnHealthDeath();
            return;
        }

        if (currentState == DroneState.Swing)
        {
            SwingMoveToPlayer();
            return;
        }

        if (currentState == DroneState.Fire)
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
        var player = GameObject.FindGameObjectWithTag("Player");
        if (player == null || missilePrefab == null) return;

        Vector3 firePos = transform.position;
        Vector3 targetPos = player.transform.position;
        Vector3 dir = (targetPos - firePos).normalized;

        GameObject missile = Instantiate(missilePrefab, firePos, Quaternion.LookRotation(dir));
        var bullet = missile.GetComponent<BulletBehavior>();
        if (bullet != null)
        {
            bullet.SetDirection(dir);
            bullet.SetSpeed(missileSpeed);
        }
    }

    private void MoveToAlignWithPlayer()
    {
        transform.position = Vector3.Lerp(transform.position, alignTargetPos, alignLerpSpeed * Time.deltaTime);
        if (Vector3.Distance(transform.position, alignTargetPos) < 0.05f)
        {
            transform.position = alignTargetPos;
            isMovingToAlign = false;
            StartCoroutine(FireMissileBurst());
        }
    }

    private void SetNextSwingTarget()
    {
        if (useRandomSwing)
        {
            // 隨機單位向量
            float angle = Random.Range(0f, Mathf.PI * 2f);
            swingMoveDir = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f).normalized;
        }
        else
        {
            // 朝玩家
            var player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                swingTargetPos = new Vector3(player.transform.position.x, player.transform.position.y, transform.position.z);
                swingMoveDir = (swingTargetPos - transform.position).normalized;
            }
            else
            {
                swingMoveDir = Vector3.right; // 預設右
            }
        }
    }

    private void SwingMoveToPlayer()
    {
        transform.position += swingMoveDir * swingMoveSpeed * Time.deltaTime;
        swingTimer += Time.deltaTime;
        if (swingTimer >= swingDuration)
        {
            currentState = DroneState.Fire;
            alignTargetPos = transform.position;
            onSwingComplete?.Invoke();
            onSwingComplete = null;
            useRandomSwing = !useRandomSwing; // 交替
        }
    }

    private IEnumerator FireMissileBurst()
    {
        if (audioSource != null && missileFireSfx != null)
        {
            audioSource.PlayOneShot(missileFireSfx);
        }
        int burstCount = Random.Range(1, 6);
        for (int i = 0; i < burstCount; i++)
        {
            FireMissileAtPlayer();
            yield return new WaitForSeconds(0.1f);
        }

        // Fire 結束後再 Swing
        SetNextSwingTarget();
        swingTimer = 0f;
        onSwingComplete = () => { isMovingToAlign = true; currentState = DroneState.Fire; };
        currentState = DroneState.Swing;
    }

    public override void OnWaveMove()
    {
        // 波次移動階段，可加特效或待機動畫
    }

    public override void OnWaveStart()
    {
        // 波次開始行動階段，可加初始化或特效
    }

}
