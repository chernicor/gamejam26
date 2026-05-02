using UnityEngine;

namespace Dany
{
    /// <summary>
    /// Простой снаряд вперёд по локальной оси Z; при попадании наносит урон один раз.
    /// </summary>
    public class EnemyProjectile : MonoBehaviour
    {
        [SerializeField] private float speed = 24f;
        [SerializeField] private float maxLifetime = 5f;
        [SerializeField] private float hitVfxAutoDestroySeconds = 4f;

        private float _damage;
        private LayerMask _obstacleMask;
        private float _age;
        private GameObject _hitVfxPrefab;

        public void Initialize(float damage, LayerMask obstacleMask, GameObject hitVfxPrefab = null)
        {
            _damage = damage;
            _obstacleMask = obstacleMask;
            _hitVfxPrefab = hitVfxPrefab;
        }

        private void Update()
        {
            float step = speed * Time.deltaTime;
            Vector3 next = transform.position + transform.forward * step;

            if (Physics.Linecast(transform.position, next, out RaycastHit hit, _obstacleMask, QueryTriggerInteraction.Ignore))
            {
                EnemyDamage.Apply(hit.collider, _damage);
                SpawnHitVfx(hit.point, hit.normal);
                Destroy(gameObject);
                return;
            }

            transform.position = next;
            _age += Time.deltaTime;
            if (_age >= maxLifetime)
                Destroy(gameObject);
        }

        private void SpawnHitVfx(Vector3 point, Vector3 normal)
        {
            if (_hitVfxPrefab == null) return;
            Quaternion r = normal.sqrMagnitude > 1e-6f
                ? Quaternion.LookRotation(normal.normalized)
                : Quaternion.identity;
            var instance = Instantiate(_hitVfxPrefab, point, r);
            if (!instance.TryGetComponent<ParticleSystem>(out var ps))
                ps = instance.GetComponentInChildren<ParticleSystem>();
            if (ps != null)
            {
                ps.Play();
                float d = ps.main.duration + ps.main.startDelay.constantMax;
                Destroy(instance, Mathf.Max(hitVfxAutoDestroySeconds, d));
            }
            else
            {
                Destroy(instance, hitVfxAutoDestroySeconds);
            }
        }
    }
}
