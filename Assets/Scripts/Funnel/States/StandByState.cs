using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace VolleyFire.Funnel.States
{
    public class StandByState : IFunnelState
    {
        private List<Coroutine> standByCoroutines = new List<Coroutine>();
        private bool isReadyToAttack = false;
        public bool IsReadyToAttack => isReadyToAttack;
        // activeCoroutineCount 已移除
        private int standByCount;
        private int destroyedBeforeStandByCount;
        private int totalStandByCount;
        private int runningCoroutineCount;
        private int enteredCoroutineCount;
        private int exitedCoroutineCount;
        public int TotalStandByCount => totalStandByCount;
        public int DestroyedBeforeStandByCount => destroyedBeforeStandByCount;
        public int GetStandByCount() { return standByCount; }

        public void EnterState(FunnelSystem context)
        {
            standByCount = 0;
            destroyedBeforeStandByCount = 0;
            runningCoroutineCount = 0;
            enteredCoroutineCount = 0;
            exitedCoroutineCount = 0;
            var funnels = context.GetFunnels();
            totalStandByCount = funnels.Count;
            isReadyToAttack = false;
            standByCoroutines.Clear();

            for (int i = 0; i < funnels.Count; i++)
            {
                var funnel = funnels[i];
                if (funnel != null)
                {
                    var coroutine = context.StartCoroutine(StandByCoroutine(context, funnel, i * 0.1f));
                    standByCoroutines.Add(coroutine);
                }
            }
        }

        public void UpdateState(FunnelSystem context)
        {
            Debug.Log($"entered:{enteredCoroutineCount} exited:{exitedCoroutineCount} total:{totalStandByCount}");
            isReadyToAttack = (enteredCoroutineCount == exitedCoroutineCount && enteredCoroutineCount == totalStandByCount);
        }

        public void ExitState(FunnelSystem context)
        {
            foreach (var coroutine in standByCoroutines)
            {
                if (coroutine != null)
                {
                    context.StopCoroutine(coroutine);
                }
            }
            standByCoroutines.Clear();
        }

        private IEnumerator StandByCoroutine(FunnelSystem context, Funnel funnel, float startDelay)
        {
            enteredCoroutineCount++;
            runningCoroutineCount++;
            try {
                yield return new WaitForSeconds(startDelay);

                if (funnel.Health == null || funnel.Health.IsDead()) { destroyedBeforeStandByCount++; yield break; }
                Vector3 targetPos = context.GetRandomPositionOnPlane(context.WorldZOffset, funnel);

                if (funnel.Health == null || funnel.Health.IsDead()) { destroyedBeforeStandByCount++; yield break; }
                if (!funnel.Transform.gameObject.activeSelf)
                {
                    funnel.Transform.gameObject.SetActive(true);
                }

                if (funnel.Health == null || funnel.Health.IsDead()) { destroyedBeforeStandByCount++; yield break; }
                if (funnel.Transform.parent != null)
                {
                    funnel.Transform.SetParent(null);
                }

                if (funnel.Health == null || funnel.Health.IsDead()) { destroyedBeforeStandByCount++; yield break; }
                yield return context.StartCoroutine(funnel.MoveToPosition(targetPos));

                if (funnel.Health == null || funnel.Health.IsDead()) { destroyedBeforeStandByCount++; yield break; }
                Vector3 apex = context.CalculatePyramidApex();
                Quaternion lookApex = Quaternion.LookRotation((apex - funnel.Transform.position).normalized);

                while (funnel.Health != null && !funnel.Health.IsDead() && Quaternion.Angle(funnel.Transform.rotation, lookApex) > 0.1f)
                {
                    funnel.Transform.rotation = Quaternion.RotateTowards(
                        funnel.Transform.rotation,
                        lookApex,
                        context.RotationSpeed * Time.deltaTime
                    );
                    yield return null;
                }

                if (funnel.Health == null || funnel.Health.IsDead()) { destroyedBeforeStandByCount++; yield break; }
                standByCount++;
                context.StartCoroutine(StandByRaycastCoroutine(context, funnel));
            } finally {
                runningCoroutineCount--;
                exitedCoroutineCount++;
            }
        }

        private IEnumerator StandByRaycastCoroutine(FunnelSystem context, Funnel funnel)
        {
            while (context.Mode == FunnelSystem.FunnelMode.StandBy)
            {
                if (funnel.Health == null || funnel.Health.IsDead()) yield break;
                if (Physics.Raycast(funnel.Transform.position, funnel.Transform.forward, out RaycastHit hit, context.StandByRaycastDistance))
                {
                    if (hit.collider.CompareTag("Player") && funnel.CanShoot(context.StandByShootCooldown))
                    {
                        Debug.Log($"Funnel {funnel.Transform.name} 對 {hit.collider.name} 射擊");
                        funnel.Shoot();
                    }
                }

                yield return new WaitForSeconds(0.1f);
            }
        }
    }
} 