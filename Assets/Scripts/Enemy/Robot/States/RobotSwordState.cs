using UnityEngine;

namespace VolleyFire.Enemy
{
    public class RobotSwordState : RobotState
    {
        private bool isReturning = false;
        private bool slashing = false;
        private bool drawshooting = false;
        private bool shouldDrawShoot = false;
        private bool hasCalculatedSwordPosition = false;
        private Vector3 desiredSwordPosition;
        private bool hasSlashed = false;
        private bool shouldSlash = false;
        private bool hasUpdatedMidway = false;
        private Vector3 swordStartPosition;
        private float swordStartToTargetDistance;

        public RobotSwordState(RobotBehavior robot) : base(robot) { }

        public override void Enter()
        {
            animator.SetBool("DrawingSword", true);
            ResetState();
        }

        public override void Execute()
        {
            AnimatorStateInfo slashLayer = animator.GetCurrentAnimatorStateInfo(1);
            
            if (shouldDrawShoot && !slashing)
            {
                animator.SetTrigger("DrawAndShoot");
                shouldDrawShoot = false;
                drawshooting = true;
            }
            else if (drawshooting && slashLayer.normalizedTime >= 1f)
            {
                drawshooting = false;
            }

            if (isReturning)
            {
                HandleReturning();
                return;
            }

            if (animator.GetCurrentAnimatorStateInfo(0).normalizedTime >= 1f)
            {
                HandleSwordAttack();
            }

            HandleSwordAnimationStates(slashLayer);
        }

        public override void Exit()
        {
            animator.SetBool("DrawingSword", false);
            ResetState();
        }

        private void ResetState()
        {
            slashing = false;
            drawshooting = false;
            shouldDrawShoot = false;
            hasCalculatedSwordPosition = false;
            hasSlashed = false;
            hasUpdatedMidway = false;
            isReturning = false;
        }

        private void HandleReturning()
        {
            Vector3 directionToStart = robot.initialPosition - robot.transform.position;
            directionToStart.y = 0;
            float distanceToStart = directionToStart.magnitude;

            TargetLock(robot.pistolTransform);
            
            if (distanceToStart > 0.1f)
            {
                robot.transform.position += directionToStart.normalized * robot.returnSpeed * Time.deltaTime;
            }
            else if (!animator.GetCurrentAnimatorStateInfo(0).IsName("TakeSwordShoot"))
            {
                robot.transform.position = new Vector3(robot.initialPosition.x, robot.transform.position.y, robot.initialPosition.z);
                isReturning = false;
                robot.TransitionToState(RobotBehavior.RobotMode.Idle);
            }
        }

        private void HandleSwordAttack()
        {
            if (playerTransform == null || isReturning || robot.swordTransform == null) return;

            Vector3 targetPosition = playerTransform.position;

            if (!hasCalculatedSwordPosition)
            {
                desiredSwordPosition = targetPosition + robot.slashDistance;
                hasCalculatedSwordPosition = true;
                hasUpdatedMidway = false;
                swordStartPosition = robot.swordTransform.position;
                swordStartToTargetDistance = (desiredSwordPosition - swordStartPosition).magnitude;
            }

            Vector3 swordToTarget = desiredSwordPosition - robot.swordTransform.position;
            float swordDistanceToTarget = swordToTarget.magnitude;

            if (!hasUpdatedMidway && swordStartToTargetDistance > 0f && swordDistanceToTarget <= swordStartToTargetDistance / 2f)
            {
                desiredSwordPosition = playerTransform.position + robot.slashDistance;
                hasUpdatedMidway = true;
            }

            Vector3 swordOffset = robot.swordTransform.position - robot.transform.position;
            Vector3 desiredRobotPosition = desiredSwordPosition - swordOffset;
            
            Vector3 directionToPlayer = targetPosition - robot.transform.position;
            directionToPlayer.y = 0;
            
            if (directionToPlayer != Vector3.zero)
            {
                Quaternion targetRotation = Quaternion.LookRotation(directionToPlayer, Vector3.up);
                robot.transform.rotation = Quaternion.Lerp(robot.transform.rotation, targetRotation, robot.turnSpeed * Time.deltaTime);
            }

            if (!slashing && !shouldSlash && !isReturning)
            {
                robot.transform.position = Vector3.Lerp(robot.transform.position, desiredRobotPosition, robot.swordMoveSpeed * Time.deltaTime);

                float distanceToDesiredPosition = Vector3.Distance(robot.transform.position, desiredRobotPosition);
                
                if (distanceToDesiredPosition <= robot.slashTriggerDistance && !isReturning && !slashing)
                {
                    shouldSlash = true;
                }
            }

            if (animator.GetCurrentAnimatorStateInfo(0).IsName("TakeSwordShoot"))
            {
                UpdatePistolRotation(targetPosition);
            }
        }

        private void HandleSwordAnimationStates(AnimatorStateInfo slashLayer)
        {
            if (slashLayer.IsName("Slash"))
            {
                slashing = true;
            }
            else if (slashing)
            {
                slashing = false;
                isReturning = true;
                hasCalculatedSwordPosition = false;
                shouldDrawShoot = true;
                hasUpdatedMidway = false;
            }
            else if (shouldSlash && !drawshooting && !hasSlashed)
            {
                animator.SetTrigger("Slash");
                shouldSlash = false;
                hasSlashed = true;
            }
            else if (hasSlashed && isReturning && Vector3.Distance(robot.transform.position, robot.initialPosition) <= 0.1f)
            {
                robot.TransitionToState(RobotBehavior.RobotMode.Idle);
            }
        }

        private void UpdatePistolRotation(Vector3 targetPosition)
        {
            Vector3 localTargetPosition = robot.transform.InverseTransformPoint(targetPosition);
            Vector3 localPistolPosition = robot.transform.InverseTransformPoint(robot.pistolTransform.position);
            Vector3 localDirectionToPlayer = localTargetPosition - localPistolPosition;

            float yaw = Mathf.Atan2(localDirectionToPlayer.x, localDirectionToPlayer.z) * Mathf.Rad2Deg;
            float pitch = -Mathf.Atan2(localDirectionToPlayer.y, new Vector2(localDirectionToPlayer.x, localDirectionToPlayer.z).magnitude) * Mathf.Rad2Deg;

            Quaternion targetRotation = robot.transform.rotation * Quaternion.Euler(pitch, yaw, 0);
            robot.pistolTransform.rotation = Quaternion.Lerp(robot.pistolTransform.rotation, targetRotation, robot.turnSpeed * Time.deltaTime);
        }
    }
} 