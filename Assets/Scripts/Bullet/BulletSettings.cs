using UnityEngine;

namespace VolleyFire.Bullets
{
    [CreateAssetMenu(fileName = "BulletSettings", menuName = "VolleyFire/Bullet Settings")]
    public class BulletSettings : ScriptableObject
    {
        [Header("基本設定")]
        public float speed = 60f;
        public float damage = 1f;
        public string targetTag = "Enemy";
        public bool useRigidbody = false;
        public float lifetime = 5f;

        [Header("特效設定")]
        public GameObject explosionPrefab;
    }
}
