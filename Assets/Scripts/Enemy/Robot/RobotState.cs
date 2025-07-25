using UnityEngine;

namespace VolleyFire.Enemy
{
    public abstract class RobotState
    {
        protected RobotBehavior robot;
        protected Animator animator;
        protected Transform playerTransform;

        public RobotState(RobotBehavior robot)
        {
            this.robot = robot;
            this.animator = robot.GetAnimator();
            this.playerTransform = GameObject.FindGameObjectWithTag("Player")?.transform;
        }

        public virtual void Enter() { }
        public virtual void Execute() { }
        public virtual void Exit() { }

        protected void LookAtPlayer(float turnSpeed)
        {
            if (playerTransform == null) return;

            Vector3 directionToPlayer = playerTransform.position - robot.transform.position;
            directionToPlayer.y = 0;

            if (directionToPlayer != Vector3.zero)
            {
                Quaternion targetRotation = Quaternion.LookRotation(directionToPlayer, Vector3.up);
                robot.transform.rotation = Quaternion.Lerp(robot.transform.rotation, targetRotation, turnSpeed * Time.deltaTime);
            }
        }

        protected void TargetLock(Transform weaponTransform, Vector3? offset = null)
        {
            if (playerTransform == null || weaponTransform == null) return;
            
            Vector3 realOffset = offset ?? Vector3.zero;
            Vector3 lockPoint = playerTransform.position + realOffset;
            Vector3 toTarget = lockPoint - weaponTransform.position;
            
            if (toTarget == Vector3.zero) return;

            Vector3 stableUp = Vector3.up;
            Quaternion desiredWeaponRotation = Quaternion.LookRotation(toTarget.normalized, stableUp);
            Quaternion rotationDelta = desiredWeaponRotation * Quaternion.Inverse(weaponTransform.rotation);
            robot.transform.rotation = Quaternion.Lerp(robot.transform.rotation, desiredWeaponRotation, 0.25f);
        }
    }
} 