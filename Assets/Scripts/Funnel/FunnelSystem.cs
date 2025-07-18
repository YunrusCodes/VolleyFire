using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using VolleyFire.Funnel;
using VolleyFire.Funnel.States;

namespace VolleyFire.Funnel
{
    public class FunnelSystem : MonoBehaviour
    {
        public enum FunnelMode { Default, AttackPattern, StandBy }

        [Header("功能設定")]
        public bool enableAction = false;

        [Header("世界座標範圍設定")]
        public Vector2 WorldXRange = new Vector2(-5, 5);
        public Vector2 WorldYRange = new Vector2(-5, 5);
        [SerializeField] private float worldZOffset = 1.0f;
        public Vector3 WorldCenterPoint;

        [Header("縮放設定")]
        [Range(0.1f, 1.0f)] public float MinScale = 0.2f;
        [Range(0.1f, 1.0f)] public float MaxScale = 1.0f;

        [Header("間距設定")]
        public float MinFunnelDistance = 2.0f;

        [Header("攻擊設定")]
        public GameObject BulletPrefab;
        [SerializeField] private float movementSpeed = 5f;
        [SerializeField] private float rotationSpeed = 180f;

        [Header("音效設定")]
        public AudioClip funnelMove;
        [SerializeField] private float standByRaycastDistance = 400f;
        [SerializeField] private float standByShootCooldown = 1.5f;

        [Header("Funnel 設定")]
        [SerializeField] private List<Transform> funnelTransforms = new List<Transform>();

        private FunnelMode mode = FunnelMode.Default;
        private List<Funnel> funnels = new List<Funnel>();
        private FunnelFactory funnelFactory;
        private IFunnelState currentState;
        private Dictionary<Transform, Vector3> funnelLastPositions = new();

        private Dictionary<FunnelMode, IFunnelState> states;

        public FunnelMode Mode => mode;
        public float WorldZOffset => worldZOffset;
        public float RotationSpeed => rotationSpeed;
        public float StandByRaycastDistance => standByRaycastDistance;
        public float StandByShootCooldown => standByShootCooldown;

        void Start()
        {
            if (WorldCenterPoint == Vector3.zero)
                WorldCenterPoint = transform.position;

            InitializeStates();
            InitializeFunnelFactory();
            InitializeFunnels();
            
            ApplyMode(FunnelMode.Default);
        }

        void Update()
        {
            if (currentState != null)
            {
                currentState.UpdateState(this);
            }

            RemoveDestroyedFunnels();
        }

        void OnDestroy()
        {
            if (currentState != null)
            {
                currentState.ExitState(this);
            }
            
            funnelLastPositions.Clear();
        }

        private void InitializeStates()
        {
            states = new Dictionary<FunnelMode, IFunnelState>
            {
                { FunnelMode.Default, new DefaultState() },
                { FunnelMode.AttackPattern, new AttackPatternState() },
                { FunnelMode.StandBy, new StandByState() }
            };
        }

        private void InitializeFunnelFactory()
        {
            funnelFactory = new FunnelFactory(BulletPrefab, movementSpeed, rotationSpeed, funnelMove);
        }

        private void InitializeFunnels()
        {
            funnels.Clear();
            foreach (var transform in funnelTransforms)
            {
                if (transform != null)
                {
                    var funnel = funnelFactory.CreateFunnel(transform);
                    funnels.Add(funnel);
                    transform.gameObject.SetActive(false);
                }
            }
        }

        private void RemoveDestroyedFunnels()
        {
            funnels.RemoveAll(f => f.Health == null || f.Health.GetCurrentHealth() <= 0);
        }

        public void SetEnableAction(bool value)
        {
            if (value == enableAction) return;
            enableAction = value;
            ApplyMode(FunnelMode.StandBy);
        }

        public void Attack()
        {
            Debug.Log("Attack");
            Debug.Log(Mode);
            if (!enableAction || Mode == FunnelMode.AttackPattern) return;
            ApplyMode(FunnelMode.AttackPattern);
        }

        public void StandBy()
        {
            Debug.Log("StandBy");
            if (!enableAction) return;
            ApplyMode(FunnelMode.StandBy);
        }

        private void ApplyMode(FunnelMode newMode)
        {
            if (currentState != null)
            {
                currentState.ExitState(this);
            }

            mode = newMode;
            currentState = states[newMode];
            currentState.EnterState(this);
        }

        public List<Funnel> GetFunnels()
        {
            return funnels;
        }

        public Vector3 GetRandomPositionOnPlane(float zOffset, Funnel funnel)
        {
            const int MAX_ATTEMPTS = 10;
            const float CHECK_RADIUS = 5f;

            float scale = Mathf.Lerp(MinScale, MaxScale, (zOffset + worldZOffset) / (worldZOffset * 2));

            for (int i = 0; i < MAX_ATTEMPTS; i++)
            {
                Vector2 scaledX = WorldXRange * scale;
                Vector2 scaledY = WorldYRange * scale;

                float x = Random.Range(scaledX.x, scaledX.y) + WorldCenterPoint.x;
                float y = Random.Range(scaledY.x, scaledY.y) + WorldCenterPoint.y;
                float z = WorldCenterPoint.z + zOffset;
                Vector3 pos = new Vector3(x, y, z);

                if (Physics.OverlapSphere(pos, CHECK_RADIUS).Length > 0)
                {
                    Debug.Log($"Collision at {pos}, retry {i + 1}");
                    continue;
                }

                bool tooClose = false;
                foreach (var kv in funnelLastPositions)
                {
                    if (funnel == null || funnel.Transform == null || !funnel.Transform) continue;
                    if (kv.Key == funnel.Transform) continue;
                    if (Vector3.Distance(pos, kv.Value) < MinFunnelDistance)
                    {
                        tooClose = true;
                        Debug.Log($"Too close to {kv.Key.name}, retry {i + 1}");
                        break;
                    }
                }

                if (!tooClose)
                {
                    if (funnel != null && funnel.Transform != null && funnel.Transform)
                        funnelLastPositions[funnel.Transform] = pos;
                    return pos;
                }
            }

            Debug.LogWarning("Position fallback after max attempts");
            float fallbackX = Random.Range(WorldXRange.x, WorldXRange.y) + WorldCenterPoint.x;
            float fallbackY = Random.Range(WorldYRange.x, WorldYRange.y) + WorldCenterPoint.y;
            float fallbackZ = WorldCenterPoint.z + zOffset;
            Vector3 fallbackPos = new Vector3(fallbackX, fallbackY, fallbackZ);

            if (funnel != null && funnel.Transform != null && funnel.Transform)
                funnelLastPositions[funnel.Transform] = fallbackPos;
            return fallbackPos;
        }

        public Vector3 CalculatePyramidApex()
        {
            float plusScale = Mathf.Lerp(MinScale, MaxScale, 1f);
            float zeroScale = Mathf.Lerp(MinScale, MaxScale, 0.5f);
            float scaleRate = (plusScale - zeroScale) / worldZOffset;
            float distanceToApex = zeroScale / scaleRate;
            float apexZ = WorldCenterPoint.z - distanceToApex;

            return new Vector3(WorldCenterPoint.x, WorldCenterPoint.y, apexZ);
        }
    }
}
