using UnityEngine;

namespace VolleyFire.Funnel.States
{
    public interface IFunnelState
    {
        void EnterState(FunnelSystem context);
        void UpdateState(FunnelSystem context);
        void ExitState(FunnelSystem context);
    }
} 