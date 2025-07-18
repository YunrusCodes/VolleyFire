using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace VolleyFire.Funnel.States
{
    public class ActivateState : IFunnelState
    {
        private List<Coroutine> activateCoroutines = new List<Coroutine>();
        private int completedAttackCount = 0;
        private int destroyedDuringAttackCount = 0;
        private int totalAttackCount = 0;
        private int runningCoroutineCount = 0;
        public int CompletedAttackCount => completedAttackCount;
        public int DestroyedDuringAttackCount => destroyedDuringAttackCount;

        public void EnterState(FunnelSystem context)
        {
            activateCoroutines.Clear();

            var funnels = context.GetFunnels();
            completedAttackCount = 0;
            destroyedDuringAttackCount = 0;
            totalAttackCount = funnels.Count;
            runningCoroutineCount = 0;
            for (int i = 0; i < funnels.Count; i++)
            {
                var funnel = funnels[i];
                if (funnel != null && funnel.Health != null && !funnel.Health.IsDead())
                {
                    var coroutine = context.StartCoroutine(ActivatePatternCoroutine(context, funnel, i * 0.75f));
                    activateCoroutines.Add(coroutine);
                }
            }
        }

        public void UpdateState(FunnelSystem context)
        {
            if (runningCoroutineCount == 0)
            {
                context.StandBy();
            }
        }

        public void ExitState(FunnelSystem context)
        {
            foreach (var coroutine in activateCoroutines)
            {
                if (coroutine != null)
                {
                    context.StopCoroutine(coroutine);
                }
            }
            activateCoroutines.Clear();
        }

        private IEnumerator ActivatePatternCoroutine(FunnelSystem context, Funnel funnel, float startDelay)
        {
            runningCoroutineCount++;
            bool destroyed = false;
            bool attacked = false;
            try
            {
                yield return new WaitForSeconds(startDelay);
                if(funnel.Transform.parent != null) funnel.Transform.SetParent(null);
                if(!funnel.Transform.gameObject.activeSelf) funnel.Transform.gameObject.SetActive(true);

                if (funnel.Health == null || funnel.Health.IsDead()) { destroyed = true; yield break; }
                // Step 1: 移動到主平面 (Z=0)
                Vector3 masterZTarget = context.GetRandomPositionOnPlane(0, funnel);
                yield return context.StartCoroutine(funnel.MoveToPosition(masterZTarget));

                if (funnel.Health == null || funnel.Health.IsDead()) { destroyed = true; yield break; }
                // Step 2: 移動至 +Z 平面並轉向玩家
                Vector3 positiveZTarget = context.GetRandomPositionOnPlane(-context.WorldZOffset, funnel);
                yield return context.StartCoroutine(funnel.MoveToPositionWithRotation(positiveZTarget, true));

                if (funnel.Health == null || funnel.Health.IsDead()) { destroyed = true; yield break; }
                // Step 3: 等待並發射子彈
                yield return new WaitForSeconds(0.1f);
                funnel.Shoot();
                attacked = true;

                // 攻擊後再檢查是否還活著
                if (funnel.Health == null || funnel.Health.IsDead()) { destroyed = true; yield break; }
                completedAttackCount++;

                // 之後的動作不影響計數
                masterZTarget = context.GetRandomPositionOnPlane(context.WorldZOffset, funnel);
                yield return context.StartCoroutine(funnel.MoveToPosition(masterZTarget));

                if (funnel.Health == null || funnel.Health.IsDead()) yield break;
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
            }
            finally
            {
                if (!attacked && destroyed) destroyedDuringAttackCount++;
                runningCoroutineCount--;
            }
        }
    }
} 