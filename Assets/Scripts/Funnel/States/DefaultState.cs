using UnityEngine;

namespace VolleyFire.Funnel.States
{
    public class DefaultState : IFunnelState
    {
        public void EnterState(FunnelSystem context)
        {
            foreach (var funnel in context.GetFunnels())
            {
                funnel.Transform.gameObject.SetActive(false);
            }
        }

        public void UpdateState(FunnelSystem context)
        {
            // Default state does nothing in update
        }

        public void ExitState(FunnelSystem context)
        {
            // Default state has no cleanup
        }
    }
} 