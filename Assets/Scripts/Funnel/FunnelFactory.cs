using UnityEngine;

namespace VolleyFire.Funnel
{
    public class FunnelFactory
    {
        private GameObject bulletPrefab;
        private float movementSpeed;
        private float rotationSpeed;
        private AudioClip moveSound;

        public FunnelFactory(GameObject bulletPrefab, float movementSpeed, float rotationSpeed, AudioClip moveSound)
        {
            this.bulletPrefab = bulletPrefab;
            this.movementSpeed = movementSpeed;
            this.rotationSpeed = rotationSpeed;
            this.moveSound = moveSound;
        }

        public Funnel CreateFunnel(Transform transform)
        {
            // 檢查是否已有 FunnelHealth 組件，如果沒有則添加
            var health = transform.GetComponent<FunnelHealth>();
            if (health == null)
            {
                // 如果有其他 BaseHealth 組件，先移除
                var oldHealth = transform.GetComponent<BaseHealth>();
                if (oldHealth != null)
                {
                    Object.DestroyImmediate(oldHealth);
                }
                health = transform.gameObject.AddComponent<FunnelHealth>();
            }

            var audio = transform.GetComponent<AudioSource>() ?? transform.gameObject.AddComponent<AudioSource>();
            
            if (moveSound != null)
            {
                audio.clip = moveSound;
            }

            return new Funnel(transform, health, audio, bulletPrefab, movementSpeed, rotationSpeed, moveSound);
        }
    }
} 