using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;

public class RobotBehavior : EnemyBehavior
{
    #region Enums & Constants
    public enum RobotMode { Idle = 0, GunMode = 1, SwordMode = 2 }
    private const int MAX_BULLETS = 100;
    #endregion

    #region Serialized Fields
    [Header("Mode Settings")]
    public RobotMode mode = RobotMode.GunMode;

    [Header("移動參數")]
    public float moveSpeed = 5f;
    public float moveDistance = 3f;
    public int targetPointCount = 3;
    public float turnSpeed = 5f;

    [Header("標記設置")]
    public Color markerColor = Color.red;
    public float markerSize = 0.3f;

    [Header("邊界設置")]
    [Tooltip("x: 上限, y: 下限")]
    public Vector2 boundaryX = new Vector2(8f, -8f);
    public Vector2 boundaryY = new Vector2(4f, -4f);

    [Header("射擊設置")]
    [SerializeField] private GameObject gunBulletPrefab;
    [SerializeField] private float fireRate = 1f;

    [Header("劍模式設置")]
    public Vector3 slashDistance = new Vector3(0f, 0f, 3f);
    public float swordMoveSpeed = 8f;
    public float returnSpeed = 10f;
    [SerializeField] private bool isReturning = false;
    [SerializeField] private Transform swordTransform;
    [SerializeField] private Transform gunTransform;
    [SerializeField] private Transform pistolTransform;
    [SerializeField] private Animator animator;
    #endregion

    #region Private Fields
    private EnemyController controller;
    private float nextFireTime;
    private int bulletsFired = 0;
    private bool slashing = false;
    private bool drawshooting = false;
    private Vector3 initialPosition;
    private Quaternion initialRotation;
    private Transform playerTransform;
    private Renderer playerRenderer;
    private bool hasCalculatedSwordPosition = false;
    private Vector3 desiredSwordPosition;
    private bool hasSlashed = false;
    private RobotMode lastAttackMode = RobotMode.SwordMode;

    // 狀態標記
    public bool slashbool = false;
    public bool sheathbool = false;
    public bool drawshootbool = false;

    // 巡邏相關
    private List<Vector3> targetPoints = new List<Vector3>();
    private List<GameObject> targetMarkers = new List<GameObject>();
    private int currentTargetIndex = 0;
    private Vector3 moveDirection;
    private Vector3 targetDirection;
    private bool hasUpdatedMidway = false;
    private Vector3 swordStartPosition;
    private float swordStartToTargetDistance;
    #endregion

    #region Initialization
    public override void Init(EnemyController controller)
    {
        this.controller = controller;
        initialPosition = transform.position;
        initialRotation = transform.rotation;
        SetupPlayerReferences();
        ResetState();
        GenerateTargetPoints();
    }

    private void SetupPlayerReferences()
    {
        playerTransform = GameObject.FindGameObjectWithTag("Player")?.transform;
        if (playerTransform != null)
        {
            playerRenderer = playerTransform.GetComponent<Renderer>() ?? playerTransform.GetComponentInChildren<Renderer>();
            if (playerRenderer == null)
            {
                Debug.LogWarning("找不到玩家的 Renderer 組件！");
            }
        }
        else
        {
            Debug.LogWarning("找不到標記為 'Player' 的物件！");
        }
    }

    private void ResetState()
    {
        slashing = false;
        drawshooting = false;
        slashbool = false;
        sheathbool = false;
        drawshootbool = false;
        currentTargetIndex = 0;
        bulletsFired = 0;
        hasSlashed = false;
        ClearMarkers();
    }

    private void OnDestroy()
    {
        ClearMarkers();
    }

    private void ClearMarkers()
    {
        foreach (var marker in targetMarkers)
        {
            if (marker != null)
            {
                Destroy(marker);
            }
        }
        targetMarkers.Clear();
    }
    #endregion

    #region Movement System
    private void GenerateTargetPoints()
    {
        targetPoints.Clear();
        ClearMarkers();

        Vector3 currentPos = transform.position;
        int attempts = 0;
        const int MAX_ATTEMPTS = 20;

        while (targetPoints.Count < targetPointCount && attempts < MAX_ATTEMPTS)
        {
            float randomAngle = Random.Range(0f, 360f);
            float randomRadian = randomAngle * Mathf.Deg2Rad;
            
            Vector3 direction = new Vector3(
                Mathf.Cos(randomRadian),
                Mathf.Sin(randomRadian),
                0f
            ).normalized;

            // 在世界座標系中隨機生成點
            float randomX = Random.Range(boundaryX.y, boundaryX.x);
            float randomY = Random.Range(boundaryY.y, boundaryY.x);
            Vector3 newTarget = new Vector3(randomX, randomY, currentPos.z);

            if (IsWithinBoundary(newTarget))
            {
                targetPoints.Add(newTarget);
                CreateTargetMarker(newTarget, targetPoints.Count);
                currentPos = newTarget;
            }

            attempts++;
        }

        while (targetPoints.Count < targetPointCount && targetPoints.Count > 0)
        {
            Vector3 lastPoint = targetPoints[targetPoints.Count - 1];
            targetPoints.Add(lastPoint);
            CreateTargetMarker(lastPoint, targetPoints.Count);
        }

        if (targetPoints.Count > 0)
        {
            targetDirection = (targetPoints[0] - transform.position).normalized;
            moveDirection = targetDirection;
        }
    }

    private void CreateTargetMarker(Vector3 position, int index)
    {
        GameObject marker = GameObject.CreatePrimitive(PrimitiveType.Cube);
        marker.transform.position = position;
        marker.transform.localScale = Vector3.one * markerSize;
        
        var renderer = marker.GetComponent<Renderer>();
        if (renderer != null)
        {
            renderer.material = new Material(Shader.Find("Standard"));
            renderer.material.color = markerColor;
        }
        
        marker.name = $"TargetMarker_{index}";
        targetMarkers.Add(marker);
    }

    private bool IsWithinBoundary(Vector3 position)
    {
        // 直接使用世界座標檢查邊界
        return position.x >= boundaryX.y && position.x <= boundaryX.x &&
               position.y >= boundaryY.y && position.y <= boundaryY.x;
    }

    private void UpdateMarkersColor()
    {
        for (int i = 0; i < targetMarkers.Count; i++)
        {
            if (targetMarkers[i] != null)
            {
                var renderer = targetMarkers[i].GetComponent<Renderer>();
                if (renderer != null)
                {
                    renderer.material.color = (i == currentTargetIndex) ? Color.red : markerColor;
                }
            }
        }
    }
    #endregion

    #region Combat System
    private void FireBullet()
    {
        if (gunBulletPrefab != null && gunTransform != null)
        {
            Instantiate(gunBulletPrefab, gunTransform.position, gunTransform.rotation);
        }
    }

    private void TargetLock()
    {
        if (playerTransform == null || pistolTransform == null) return;
        
        Vector3 targetPosition = playerTransform.position;
        Vector3 pistolToPlayer = targetPosition - pistolTransform.position;
        if (pistolToPlayer == Vector3.zero) return;

        Vector3 localPistolPos = transform.InverseTransformPoint(pistolTransform.position);
        Vector3 localTargetPos = transform.InverseTransformPoint(targetPosition);
        Vector3 localDir = (localTargetPos - localPistolPos).normalized;

        Quaternion localTargetRot = Quaternion.LookRotation(localDir, Vector3.up);
        Quaternion worldTargetRot = transform.rotation * localTargetRot;
        transform.rotation = Quaternion.Lerp(transform.rotation, worldTargetRot, 0.25f);
    }
    #endregion

    #region Mode Behaviors
    private void GunModeBehavior()
    {
        if (playerTransform != null && gunTransform != null)
        {
            Vector3 targetPosition = playerTransform.position;
            Vector3 localTargetPosition = transform.InverseTransformPoint(targetPosition);
            Vector3 localGunPosition = transform.InverseTransformPoint(gunTransform.position);
            Vector3 localDirectionToPlayer = localTargetPosition - localGunPosition;
            
            float yaw = Mathf.Atan2(localDirectionToPlayer.x, localDirectionToPlayer.z) * Mathf.Rad2Deg;
            float pitch = -Mathf.Atan2(localDirectionToPlayer.y, new Vector2(localDirectionToPlayer.x, localDirectionToPlayer.z).magnitude) * Mathf.Rad2Deg;

            Quaternion targetRotation = transform.rotation * Quaternion.Euler(pitch, yaw, 0);
            transform.rotation = Quaternion.Lerp(transform.rotation, targetRotation, turnSpeed * Time.deltaTime);

            if (Time.time >= nextFireTime && bulletsFired < MAX_BULLETS && animator.GetCurrentAnimatorStateInfo(0).normalizedTime >= 1f)
            {
                FireBullet();
                nextFireTime = Time.time + 1f / fireRate;
                bulletsFired++;

                if (bulletsFired >= MAX_BULLETS)
                {
                    sheathbool = true;
                    bulletsFired = 0;
                }
            }
        }

        Vector3 toTarget = targetPoints[currentTargetIndex] - transform.position;
        float distanceToTarget = toTarget.magnitude;

        if (distanceToTarget < 0.1f)
        {
            currentTargetIndex = (currentTargetIndex + 1) % targetPoints.Count;

            if (currentTargetIndex == 0)
            {
                GenerateTargetPoints();
                return;
            }

            toTarget = targetPoints[currentTargetIndex] - transform.position;
            UpdateMarkersColor();
        }

        targetDirection = toTarget.normalized;
        moveDirection = Vector3.Lerp(moveDirection, targetDirection, turnSpeed * Time.deltaTime);
        moveDirection.Normalize();
        transform.position += moveDirection * moveSpeed * Time.deltaTime;
    }

    private void HandleIdleMode()
    {
        AnimatorStateInfo IdleLayer = animator.GetCurrentAnimatorStateInfo(0);
        
        if (IdleLayer.IsName("Idle") && IdleLayer.normalizedTime >= 0.25f)
        {
            mode = lastAttackMode == RobotMode.SwordMode ? RobotMode.GunMode : RobotMode.SwordMode;
            lastAttackMode = mode;
            return;
        }

        if (playerTransform != null)
        {
            Vector3 directionToPlayer = playerTransform.position - transform.position;
            directionToPlayer.y = 0;

            if (directionToPlayer != Vector3.zero)
            {
                Quaternion targetRotation = Quaternion.LookRotation(directionToPlayer, Vector3.up);
                transform.rotation = Quaternion.Lerp(transform.rotation, targetRotation, turnSpeed * Time.deltaTime);
            }
        }

        ResetState();
    }

    private void HandleGunMode()
    {
        if (sheathbool)
        {
            animator.SetBool("DrawingGun", false);
            sheathbool = false;
            mode = RobotMode.Idle;
        }
        else if (!animator.GetBool("DrawingGun"))
        {
            animator.SetBool("DrawingGun", true);
        }
    }

    private void HandleSwordMode()
    {
        AnimatorStateInfo SlashLayer = animator.GetCurrentAnimatorStateInfo(1);
        
        if (drawshootbool && !slashing)
        {
            animator.SetTrigger("DrawAndShoot");
            drawshootbool = false;
            drawshooting = true;
        }
        else if (drawshooting && SlashLayer.normalizedTime >= 1f)
        {
            drawshooting = false;
        }

        if (isReturning)
        {
            HandleReturning();
            return;
        }

        if (animator.GetCurrentAnimatorStateInfo(0).normalizedTime >= 1f)
        {
            HandleSwordAttack();
        }

        HandleSwordAnimationStates(SlashLayer);
    }

    private void HandleReturning()
    {
        Vector3 directionToStart = initialPosition - transform.position;
        directionToStart.y = 0;
        float distanceToStart = directionToStart.magnitude;

        TargetLock();
        
        if (distanceToStart > 0.1f)
        {
            transform.position += directionToStart.normalized * returnSpeed * Time.deltaTime;
        }
        else if (!animator.GetCurrentAnimatorStateInfo(0).IsName("TakeSwordShoot"))
        {
            transform.position = new Vector3(initialPosition.x, transform.position.y, initialPosition.z);
            isReturning = false;
            if (Random.value < 0.5f)
            {
                animator.SetBool("DrawingSword", false);
                mode = RobotMode.Idle;
            }
            ResetState();
        }
    }

    private void HandleSwordAttack()
    {
        if (playerTransform == null || isReturning || swordTransform == null) return;

        Vector3 targetPosition = playerTransform.position;

        if (!hasCalculatedSwordPosition)
        {
            desiredSwordPosition = targetPosition + slashDistance;
            hasCalculatedSwordPosition = true;
            hasUpdatedMidway = false;
            swordStartPosition = swordTransform.position;
            swordStartToTargetDistance = (desiredSwordPosition - swordStartPosition).magnitude;
        }

        Vector3 swordToTarget = desiredSwordPosition - swordTransform.position;
        float swordDistanceToTarget = swordToTarget.magnitude;
        Vector3 swordDirection = swordToTarget.normalized;

        if (!hasUpdatedMidway && swordStartToTargetDistance > 0f && swordDistanceToTarget <= swordStartToTargetDistance / 2f)
        {
            desiredSwordPosition = playerTransform.position + slashDistance;
            hasUpdatedMidway = true;
        }

        Vector3 directionToPlayer = targetPosition - transform.position;
        directionToPlayer.y = 0;
        
        if (directionToPlayer != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(directionToPlayer, Vector3.up);
            transform.rotation = Quaternion.Lerp(transform.rotation, targetRotation, turnSpeed * Time.deltaTime);
        }

        if (!slashing && !slashbool && !isReturning)
        {
            transform.position += swordDirection * swordMoveSpeed * Time.deltaTime;
        }

        if (swordDistanceToTarget <= 0.1f && !slashing)
        {
            if (!isReturning && !slashing) slashbool = true;
        }

        if (animator.GetCurrentAnimatorStateInfo(0).IsName("TakeSwordShoot"))
        {
            UpdatePistolRotation(targetPosition);
        }
    }

    private void UpdatePistolRotation(Vector3 targetPosition)
    {
        Vector3 localTargetPosition = transform.InverseTransformPoint(targetPosition);
        Vector3 localPistolPosition = transform.InverseTransformPoint(pistolTransform.position);
        Vector3 localDirectionToPlayer = localTargetPosition - localPistolPosition;

        float yaw = Mathf.Atan2(localDirectionToPlayer.x, localDirectionToPlayer.z) * Mathf.Rad2Deg;
        float pitch = -Mathf.Atan2(localDirectionToPlayer.y, new Vector2(localDirectionToPlayer.x, localDirectionToPlayer.z).magnitude) * Mathf.Rad2Deg;

        Quaternion targetRotation = transform.rotation * Quaternion.Euler(pitch, yaw, 0);
        pistolTransform.rotation = Quaternion.Lerp(pistolTransform.rotation, targetRotation, turnSpeed * Time.deltaTime);
    }

    private void HandleSwordAnimationStates(AnimatorStateInfo SlashLayer)
    {
        if (SlashLayer.IsName("Slash"))
        {
            slashing = true;
        }
        else if (slashing)
        {
            slashing = false;
            isReturning = true;
            hasCalculatedSwordPosition = false;
            drawshootbool = true;
            hasUpdatedMidway = false;
        }
        else if (slashbool && !drawshooting && !hasSlashed)
        {
            animator.SetTrigger("Slash");
            slashbool = false;
            hasSlashed = true;
        }
        else if (sheathbool)
        {
            animator.SetBool("DrawingSword", false);
            sheathbool = false;
            mode = RobotMode.Idle;
        }
        else if (!animator.GetBool("DrawingSword"))
        {
            animator.SetBool("DrawingSword", true);
        }
    }
    #endregion

    #region Main Update
    public override void Tick()
    {
        if (animator == null) return;

        switch (mode)
        {
            case RobotMode.Idle:
                HandleIdleMode();
                break;
            case RobotMode.GunMode:
                HandleGunMode();
                GunModeBehavior();
                break;
            case RobotMode.SwordMode:
                HandleSwordMode();
                break;
        }
    }
    #endregion
}

