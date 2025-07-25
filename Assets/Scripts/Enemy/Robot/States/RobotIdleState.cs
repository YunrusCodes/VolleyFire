using UnityEngine;

namespace VolleyFire.Enemy
{
    public class RobotIdleState : RobotState
    {
        public RobotIdleState(RobotBehavior robot) : base(robot) { }

        public override void Enter()
        {
            robot.ResetState();
        }

        public override void Execute()
        {
            AnimatorStateInfo idleLayer = animator.GetCurrentAnimatorStateInfo(0);
            
            if (idleLayer.IsName("Idle") && idleLayer.normalizedTime >= 0.25f)
            {
                var nextMode = robot.GetLastAttackMode() == RobotBehavior.RobotMode.SwordMode 
                    ? RobotBehavior.RobotMode.GunMode 
                    : RobotBehavior.RobotMode.SwordMode;
                    
                robot.TransitionToState(nextMode);
                return;
            }

            LookAtPlayer(robot.turnSpeed);
        }
    }
} 