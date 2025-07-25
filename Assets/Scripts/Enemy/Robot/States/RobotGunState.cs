using UnityEngine;
using System.Collections.Generic;

namespace VolleyFire.Enemy
{
    public class RobotGunState : RobotState
    {
        private List<Vector3> targetPoints = new List<Vector3>();
        private bool isPostShootWaiting = false;
        private float postShootWaitTimer = 0f;
        private int bulletsFired = 0;

        public RobotGunState(RobotBehavior robot) : base(robot) { }

        public override void Enter()
        {
            animator.SetBool("DrawingGun", true);
            GenerateTargetPoints();
        }

        public override void Execute()
        {
            if (animator.GetBool("DrawingGun"))
            {
                HandleGunMode();
                GunModeBehavior();
            }
        }

        public override void Exit()
        {
            animator.SetBool("DrawingGun", false);
            targetPoints.Clear();
        }

        private void HandleGunMode()
        {
            if (robot.sheathbool)
            {
                robot.TransitionToState(RobotBehavior.RobotMode.Idle);
            }
        }

        private void GunModeBehavior()
        {
            if (isPostShootWaiting)
            {
                postShootWaitTimer -= Time.deltaTime;
                if (postShootWaitTimer <= 0f)
                {
                    isPostShootWaiting = false;
                }
                return;
            }

            if (playerTransform != null && robot.gunTransform != null)
            {
                Vector3 originalPosition = robot.transform.position;
                robot.transform.position = targetPoints[1];
                TargetLock(robot.gunTransform, new Vector3(8, -3, 1));
                robot.transform.position = originalPosition;
            }

            if (targetPoints.Count < 2) return;

            Vector3 toTarget = targetPoints[1] - robot.transform.position;
            float totalDistance = Vector3.Distance(targetPoints[0], targetPoints[1]);
            float fromStart = Vector3.Distance(robot.transform.position, targetPoints[0]);

            if (fromStart >= totalDistance * 0.9f)
            {
                HandleTargetPointReached();
                return;
            }

            MoveTowardsTarget();
        }

        private void HandleTargetPointReached()
        {
            if (bulletsFired < robot.MAX_BULLETS && animator.GetCurrentAnimatorStateInfo(0).normalizedTime >= 1f)
            {
                FireBullet();
                bulletsFired++;
                isPostShootWaiting = true;
                postShootWaitTimer = 0.1f;

                if (bulletsFired >= robot.MAX_BULLETS)
                {
                    robot.sheathbool = true;
                    bulletsFired = 0;
                }
            }

            targetPoints.RemoveAt(0);
            Vector3 last = targetPoints[0];
            Vector3 newPoint = GenerateNewPatrolPoint(last);
            targetPoints.Add(newPoint);
        }

        private void MoveTowardsTarget()
        {
            robot.transform.position = Vector3.Lerp(
                robot.transform.position, 
                targetPoints[1], 
                robot.moveSpeed * Time.deltaTime
            );
        }

        private void FireBullet()
        {
            if (robot.gunBulletPrefab != null && robot.gunTransform != null)
            {
                Object.Instantiate(robot.gunBulletPrefab, robot.gunTransform.position, robot.gunTransform.rotation);
            }
        }

        private void GenerateTargetPoints()
        {
            targetPoints.Clear();
            targetPoints.Add(robot.transform.position);
            targetPoints.Add(GenerateNewPatrolPoint(robot.transform.position));
        }

        private Vector3 GenerateNewPatrolPoint(Vector3 from)
        {
            for (int i = 0; i < 30; i++)
            {
                float angle = Random.Range(0f, 360f);
                float rad = angle * Mathf.Deg2Rad;
                float maxDist = CalculateMaxDistance(from, rad);
                float minDist = robot.minTargetPointDistance;

                if (maxDist < minDist) continue;

                float dist = Random.Range(minDist, maxDist);
                Vector3 offset = new Vector3(Mathf.Cos(rad), Mathf.Sin(rad), 0f) * dist;
                Vector3 candidate = from + offset;

                if (IsWithinBoundary(candidate))
                    return candidate;
            }
            return from;
        }

        private float CalculateMaxDistance(Vector3 from, float rad)
        {
            float maxDist = float.MaxValue;
            if (Mathf.Cos(rad) > 0)
                maxDist = Mathf.Min(maxDist, (robot.boundaryX.x - from.x) / Mathf.Cos(rad));
            else if (Mathf.Cos(rad) < 0)
                maxDist = Mathf.Min(maxDist, (robot.boundaryX.y - from.x) / Mathf.Cos(rad));

            if (Mathf.Sin(rad) > 0)
                maxDist = Mathf.Min(maxDist, (robot.boundaryY.x - from.y) / Mathf.Sin(rad));
            else if (Mathf.Sin(rad) < 0)
                maxDist = Mathf.Min(maxDist, (robot.boundaryY.y - from.y) / Mathf.Sin(rad));

            return maxDist;
        }

        private bool IsWithinBoundary(Vector3 position)
        {
            return position.x >= robot.boundaryX.y && position.x <= robot.boundaryX.x &&
                   position.y >= robot.boundaryY.y && position.y <= robot.boundaryY.x;
        }
    }
} 