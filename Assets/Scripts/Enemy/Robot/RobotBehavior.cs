using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEditor;
using VolleyFire.Funnel;

namespace VolleyFire.Enemy
{
    public class RobotBehavior : EnemyBehavior
    {
        #region Enums & Constants
        public enum RobotMode { Idle = 0, GunMode = 1, SwordMode = 2 }
        public int MAX_BULLETS = 10;
        public float SummonFunnelHealth = 500f;
        #endregion

        #region Serialized Fields
        [Header("Mode Settings")]
        public RobotMode mode = RobotMode.GunMode;

        [Header("移動參數")]
        public float moveSpeed = 5f;
        public float moveDistance = 3f;
        public int targetPointCount = 3;
        public float turnSpeed = 5f;
        public float slashTriggerDistance = 0.5f;

        [Header("邊界設置")]
        [Tooltip("x: 上限, y: 下限")]
        public Vector2 boundaryX = new Vector2(8f, -8f);
        public Vector2 boundaryY = new Vector2(4f, -4f);

        [Header("射擊設置")]
        [SerializeField] public GameObject gunBulletPrefab;
        [SerializeField] public float fireRate = 1f;

        [Header("劍模式設置")]
        public Vector3 slashDistance = new Vector3(0f, 0f, 3f);
        public float swordMoveSpeed = 8f;
        public float returnSpeed = 10f;
        [SerializeField] public Transform swordTransform;
        [SerializeField] public Transform gunTransform;
        [SerializeField] public Transform pistolTransform;

        [Header("巡邏點距離限制")]
        public float minTargetPointDistance = 2f;
        #endregion

        #region Private Fields
        private EnemyController controller;
        private RobotState currentState;
        private RobotMode lastAttackMode = RobotMode.SwordMode;
        public Vector3 initialPosition { get; private set; }

        #endregion

        public FunnelSystem funnelSystem;

        #region Public Methods
        public RobotMode GetLastAttackMode() => lastAttackMode;

        public void TransitionToState(RobotMode newMode)
        {
            mode = newMode;
            currentState?.Exit();
            
            // 更新 lastAttackMode
            if (newMode == RobotMode.GunMode || newMode == RobotMode.SwordMode)
            {
                lastAttackMode = newMode;
            }
            
            switch (newMode)
            {
                case RobotMode.Idle:
                    currentState = new RobotIdleState(this);
                    break;
                case RobotMode.GunMode:
                    currentState = new RobotGunState(this);
                    break;
                case RobotMode.SwordMode:
                    currentState = new RobotSwordState(this);
                    break;
            }
            
            currentState?.Enter();
        }

        public Animator GetAnimator()
        {
            return animator;
        }
        #endregion

        #region Unity Lifecycle
        public override void Init(EnemyController controller)
        {
            this.controller = controller;
            initialPosition = transform.position;
            TransitionToState(mode);
        }

        public override void Tick()
        {
            if (animator == null) return;
            
            if (controller.GetHealth().GetCurrentHealth() <= SummonFunnelHealth)
            {
                funnelSystem.SetEnableAction(true);
            }
            
            if (controller.GetHealth().IsDead())
            {
                OnHealthDeath();
                funnelSystem.SetEnableAction(false);
                return;
            }

            currentState?.Execute();
        }

        private void OnDestroy()
        {
            currentState?.Exit();
        }
        #endregion

        #region Debug
#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            // 畫出巡邏邊界
            Vector3 min = new Vector3(boundaryX.y, boundaryY.y, transform.position.z);
            Vector3 max = new Vector3(boundaryX.x, boundaryY.x, transform.position.z);
            Vector3 p1 = new Vector3(min.x, min.y, min.z);
            Vector3 p2 = new Vector3(max.x, min.y, min.z);
            Vector3 p3 = new Vector3(max.x, max.y, min.z);
            Vector3 p4 = new Vector3(min.x, max.y, min.z);
            Color color = Color.yellow;
            Debug.DrawLine(p1, p2, color);
            Debug.DrawLine(p2, p3, color);
            Debug.DrawLine(p3, p4, color);
            Debug.DrawLine(p4, p1, color);
        }
#endif
        #endregion
    }
}

