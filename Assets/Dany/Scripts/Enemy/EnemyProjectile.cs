using UnityEngine;

namespace Dany
{
    /// <summary>
    /// Простой снаряд вперёд по локальной оси Z; при попадании наносит урон один раз.
    /// Проходит сквозь других врагов (<see cref="EnemyBase"/>) без урона, чтобы не было дружественного огня.
    /// </summary>
    public class EnemyProjectile : MonoBehaviour
    {
        [SerializeField] private float speed = 24f;
        [SerializeField] private float maxLifetime = 5f;
        [SerializeField] private float hitVfxAutoDestroySeconds = 4f;
        [Tooltip("Насколько проталкивать снаряд вперёд при «скольжении» мимо коллайдера другого врага.")]
        [SerializeField] private float passThroughEnemySlop = 0.09f;

        private float _damage;
        private LayerMask _obstacleMask;
        private float _age;
        private GameObject _hitVfxPrefab;
        private EnemyBase _shooter;
        private const int MaxPenetrationStepsPerFrame = 10;

        public void Initialize(float damage, LayerMask obstacleMask, GameObject hitVfxPrefab = null,
            EnemyBase shooter = null)
        {
            _damage = damage;
            _obstacleMask = obstacleMask;
            _hitVfxPrefab = hitVfxPrefab;
            _shooter = shooter;
        }

        private void Update()
        {
            float distanceLeft = speed * Time.deltaTime;

            for (int i = 0; i < MaxPenetrationStepsPerFrame && distanceLeft > 1e-5f; i++)
            {
                Vector3 pos = transform.position;
                Vector3 step = transform.forward * distanceLeft;

                if (!Physics.Linecast(pos, pos + step, out RaycastHit hit, _obstacleMask,
                        QueryTriggerInteraction.Ignore))
                {
                    transform.position = pos + step;
                    break;
                }

                if (ShouldPassThrough(hit.collider))
                {
                    float along = Vector3.Distance(pos, hit.point) + passThroughEnemySlop;
                    along = Mathf.Max(along, passThroughEnemySlop);
                    along = Mathf.Min(along, distanceLeft);
                    transform.position = pos + transform.forward * along;
                    distanceLeft -= along;
                    continue;
                }

                EnemyDamage.Apply(hit.collider, _damage);
                SpawnHitVfx(hit.point, hit.normal);
                Destroy(gameObject);
                return;
            }

            _age += Time.deltaTime;
            if (_age >= maxLifetime)
                Destroy(gameObject);
        }

        private bool ShouldPassThrough(Collider col)
        {
            if (col == null) return false;
            if (_shooter != null &&
                (col.transform == _shooter.transform || col.transform.IsChildOf(_shooter.transform)))
                return true;
            return col.GetComponentInParent<EnemyBase>() != null;
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
