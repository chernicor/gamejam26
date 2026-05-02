using System.Collections.Generic;
using SiberianGJ26.YouAreDoing.Antos.Modules;
using UnityEngine;

namespace Dany
{
    /// <summary>
    /// Бежит к игроку и при срабатывании дистанции наносит урон по сфере и уничтожается.
    /// </summary>
    public class EnemySuicide : EnemyBase
    {
        [Header("Explosion")]
        [SerializeField] private float triggerDistance = 2.2f;
        [SerializeField] private float explosionRadius = 4f;
        [SerializeField] private float damageToPlayer = 40f;
        [SerializeField] private float damageToOthers = 30f;
        [SerializeField] private GameObject explosionEffectPrefab;

        [Header("Physics")]
        [SerializeField] private LayerMask damageLayers = ~0;

        private bool _exploded;

        private void Update()
        {
            if (_exploded) return;

            RefreshTarget();
            if (Target == null) return;

            if (MoveTowardsPlayer(triggerDistance, Time.deltaTime))
                Explode();
        }

        private void Explode()
        {
            if (_exploded) return;
            _exploded = true;

            Vector3 center = transform.position + Vector3.up * 0.5f;

            if (explosionEffectPrefab != null)
                Instantiate(explosionEffectPrefab, center, Quaternion.identity);

            var hits = Physics.OverlapSphere(center, explosionRadius, damageLayers, QueryTriggerInteraction.Collide);
            var seenTargets = new HashSet<int>();
            foreach (Collider col in hits)
            {
                var mh = col.GetComponentInParent<MonoHealth>();
                if (mh != null)
                {
                    if (!seenTargets.Add(mh.GetInstanceID())) continue;
                    EnemyDamage.Apply(mh.gameObject, damageToPlayer);
                    continue;
                }

                Component receiver = EnemyDamage.GetDestructibleDamageReceiver(col.gameObject);
                if (receiver != null)
                {
                    if (!seenTargets.Add(receiver.GetInstanceID())) continue;
                    EnemyDamage.Apply(receiver.gameObject, damageToOthers);
                }
            }

            Destroy(gameObject);
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(1f, 0.2f, 0.2f, 0.35f);
            Gizmos.DrawWireSphere(transform.position + Vector3.up * 0.5f, triggerDistance);
            Gizmos.color = new Color(1f, 0.5f, 0f, 0.25f);
            Gizmos.DrawWireSphere(transform.position + Vector3.up * 0.5f, explosionRadius);
        }
    }
}
