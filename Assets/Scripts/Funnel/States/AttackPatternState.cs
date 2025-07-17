using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace VolleyFire.Funnel.States
{
    public class AttackPatternState : IFunnelState
    {
        private List<Coroutine> attackCoroutines = new List<Coroutine>();
        private int activeCoroutineCount;

        public void EnterState(FunnelSystem context)
        {
            activeCoroutineCount = 0;
            attackCoroutines.Clear();

            var funnels = context.GetFunnels();
            for (int i = 0; i < funnels.Count; i++)
            {
                var funnel = funnels[i];
                if (funnel != null)
                {
                    activeCoroutineCount++;
                    var coroutine = context.StartCoroutine(AttackPatternCoroutine(context, funnel, i * 0.75f));
                    attackCoroutines.Add(coroutine);
                }
            }
        }

        public void UpdateState(FunnelSystem context)
        {
            if (activeCoroutineCount == 0)
            {
                context.StandBy();
            }
        }

        public void ExitState(FunnelSystem context)
        {
            foreach (var coroutine in attackCoroutines)
            {
                if (coroutine != null)
                {
                    context.StopCoroutine(coroutine);
                }
            }
            attackCoroutines.Clear();
        }

        private IEnumerator AttackPatternCoroutine(FunnelSystem context, Funnel funnel, float startDelay)
        {
            yield return new WaitForSeconds(startDelay);

            // Step 1: 移動到主平面 (Z=0)
            Vector3 masterZTarget = context.GetRandomPositionOnPlane(0, funnel);
            yield return context.StartCoroutine(funnel.MoveToPosition(masterZTarget));

            // Step 2: 移動至 +Z 平面並轉向玩家
            Vector3 positiveZTarget = context.GetRandomPositionOnPlane(-context.WorldZOffset, funnel);
            yield return context.StartCoroutine(funnel.MoveToPositionWithRotation(positiveZTarget, true));

            // Step 3: 等待並發射子彈
            yield return new WaitForSeconds(0.1f);
            funnel.Shoot();

            // Step 4: 返回主平面
            masterZTarget = context.GetRandomPositionOnPlane(0, funnel);
            yield return context.StartCoroutine(funnel.MoveToPositionWithRotation(masterZTarget, false));

            // Step 4: 返回主平面
            masterZTarget = context.GetRandomPositionOnPlane(context.WorldZOffset, funnel);
            yield return context.StartCoroutine(funnel.MoveToPosition(masterZTarget));
            
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

            activeCoroutineCount--;
        }
    }
} 