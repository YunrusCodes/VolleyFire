using UnityEngine;
using System.Collections;

namespace VolleyFire.Funnel
{
    public class Funnel
    {
        public Transform Transform { get; private set; }
        public BaseHealth Health { get; private set; }
        public AudioSource Audio { get; private set; }
        public FunnelStrategy Strategy { get; set; }
        
        private float lastShootTime;
        private GameObject bulletPrefab;
        private float movementSpeed;
        private float rotationSpeed;
        private AudioClip moveSound;

        public Funnel(Transform transform, BaseHealth health, AudioSource audio, GameObject bulletPrefab, float movementSpeed, float rotationSpeed, AudioClip moveSound)
        {
            Transform = transform;
            Health = health;
            Audio = audio;
            this.bulletPrefab = bulletPrefab;
            this.movementSpeed = movementSpeed;
            this.rotationSpeed = rotationSpeed;
            this.moveSound = moveSound;
            lastShootTime = 0f;
        }

        public IEnumerator MoveToPosition(Vector3 targetPosition, bool faceTarget = true)
        {
            PlaySound();
            
            while (Vector3.Distance(Transform.position, targetPosition) > 0.1f)
            {
                Vector3 dir = (targetPosition - Transform.position).normalized;
                Transform.position = Vector3.MoveTowards(Transform.position, targetPosition, movementSpeed * Time.deltaTime);

                if (dir != Vector3.zero && faceTarget)
                {
                    Quaternion targetRot = Quaternion.LookRotation(dir);
                    Transform.rotation = Quaternion.RotateTowards(Transform.rotation, targetRot, rotationSpeed * Time.deltaTime);
                }

                yield return null;
            }
        }

        public IEnumerator MoveToPositionWithRotation(Vector3 targetPosition, bool isAimingAtPlayer)
        {
            PlaySound();
            GameObject player = GameObject.FindGameObjectWithTag("Player");

            if (player == null && isAimingAtPlayer) yield break;

            Vector3 startPos = Transform.position;
            Quaternion startRot = Transform.rotation;
            float distance = Vector3.Distance(startPos, targetPosition);
            float startTime = Time.time;

            while (Vector3.Distance(Transform.position, targetPosition) > 0.1f)
            {
                float elapsed = (Time.time - startTime) * movementSpeed;
                float t = Mathf.Clamp01(elapsed / distance);

                Transform.position = Vector3.MoveTowards(Transform.position, targetPosition, movementSpeed * Time.deltaTime);

                Quaternion targetRot = isAimingAtPlayer && player != null
                    ? Quaternion.LookRotation((player.transform.position - Transform.position).normalized)
                    : Quaternion.LookRotation((targetPosition - Transform.position).normalized);

                Quaternion lerped = Quaternion.Lerp(startRot, targetRot, t);
                Transform.rotation = Quaternion.RotateTowards(Transform.rotation, lerped, rotationSpeed * Time.deltaTime);

                yield return null;
            }

            if (isAimingAtPlayer && player != null)
            {
                Transform.rotation = Quaternion.LookRotation((player.transform.position - Transform.position).normalized);
            }
            else
            {
                Transform.rotation = Quaternion.LookRotation((targetPosition - startPos).normalized);
            }
        }

        public void PlaySound()
        {
            if (Audio != null && moveSound != null)
            {
                Audio.Stop();
                Audio.clip = moveSound;
                Audio.Play();
            }
        }

        public void Shoot()
        {
            if (bulletPrefab != null)
            {
                Object.Instantiate(bulletPrefab, Transform.position, Transform.rotation);
                lastShootTime = Time.time;
            }
        }

        public bool CanShoot(float cooldown)
        {
            return Time.time - lastShootTime >= cooldown;
        }
    }
} 