using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace VolleyFire.Funnel.States
{
    public class StandByState : IFunnelState
    {
        private List<Coroutine> standByCoroutines = new List<Coroutine>();
        private int activeCoroutineCount;

        public void EnterState(FunnelSystem context)
        {
            activeCoroutineCount = 0;
            standByCoroutines.Clear();

            var funnels = context.GetFunnels();
            for (int i = 0; i < funnels.Count; i++)
            {
                var funnel = funnels[i];
                if (funnel != null)
                {
                    activeCoroutineCount++;
                    var coroutine = context.StartCoroutine(StandByCoroutine(context, funnel, i * 0.1f));
                    standByCoroutines.Add(coroutine);
                }
            }
        }

        public void UpdateState(FunnelSystem context)
        {
            // StandBy state is handled in coroutines
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
            yield return new WaitForSeconds(startDelay);

            Vector3 targetPos = context.GetRandomPositionOnPlane(context.WorldZOffset, funnel);

            if (!funnel.Transform.gameObject.activeSelf)
            {
                funnel.Transform.gameObject.SetActive(true);
            }

            if (funnel.Transform.parent != null)
            {
                funnel.Transform.SetParent(null);
            }

            yield return context.StartCoroutine(funnel.MoveToPosition(targetPos));

            Vector3 apex = context.CalculatePyramidApex();
            Quaternion lookApex = Quaternion.LookRotation((apex - funnel.Transform.position).normalized);

            while (Quaternion.Angle(funnel.Transform.rotation, lookApex) > 0.1f)
            {
                funnel.Transform.rotation = Quaternion.RotateTowards(
                    funnel.Transform.rotation,
                    lookApex,
                    context.RotationSpeed * Time.deltaTime
                );
                yield return null;
            }

            context.StartCoroutine(StandByRaycastCoroutine(context, funnel));
            activeCoroutineCount--;
        }

        private IEnumerator StandByRaycastCoroutine(FunnelSystem context, Funnel funnel)
        {
            while (context.Mode == FunnelSystem.FunnelMode.StandBy)
            {
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