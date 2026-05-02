using UnityEngine;

namespace Dany
{
    public enum EnemyRangedAttackMode
    {
        /// <summary>Мгновенный урон по лучу (Physics.Raycast).</summary>
        Raycast = 0,
        /// <summary>Снаряд <see cref="EnemyProjectile"/>.</summary>
        Projectile = 1
    }

    /// <summary>
    /// Держит дистанцию и стреляет рейкастом (по умолчанию) или снарядом.
    /// </summary>
    public class EnemyRanged : EnemyBase
    {
        [Header("Range")]
        [SerializeField] private float minShootRange = 4f;
        [SerializeField] private float maxShootRange = 22f;
        [SerializeField] private float aimHeightOffset = 1.2f;

        [Header("Shooting")]
        [SerializeField] private EnemyRangedAttackMode attackMode = EnemyRangedAttackMode.Raycast;
        [SerializeField] private float damage = 12f;
        [SerializeField] private float fireCooldown = 1.4f;
        [Tooltip("Если выключено — стреляет по дистанции без луча «вижу/не вижу» (рекомендуется для джема).")]
        [SerializeField] private bool requireLineOfSight = false;
        [Tooltip("При включённом LOS: поднять точку старта луча, чтобы не упираться в землю у ног.")]
        [SerializeField] private float lineOfSightOriginLift = 1.2f;
        [Tooltip("Длина луча при режиме Raycast (должна быть не меньше max shoot range).")]
        [SerializeField] private float raycastMaxDistance = 32f;
        [Tooltip("Если луч не нашёл коллайдер игрока (маска слоёв, стена ближе и т.д.), всё равно нанести урон по цели в зоне стрельбы.")]
        [SerializeField] private bool applyDamageIfRayHitsNothing = true;
        [SerializeField] private EnemyProjectile projectilePrefab;
        [SerializeField] private Transform muzzle;
        [SerializeField] private LayerMask obstacleMask = ~0;

        [Header("VFX")]
        [Tooltip("Точка появления вспышки выстрела (если пусто — используется Muzzle / расчётная позиция).")]
        [SerializeField] private Transform shootVfxSpawn;
        [SerializeField] private GameObject shootVfxPrefab;
        [SerializeField] private GameObject hitVfxPrefab;
        [SerializeField] private float vfxAutoDestroySeconds = 4f;

        [Header("Accuracy (misses)")]
        [Tooltip("Вероятность полного промаха за выстрел (0 = всегда в цель, 1 = всегда мимо). Кулдаун всё равно тратится.")]
        [SerializeField, Range(0f, 1f)] private float missChance = 0.15f;
        [Tooltip("Случайный разброс направления луча/снаряда в градусах (0 = идеально в цель).")]
        [SerializeField, Range(0f, 15f)] private float aimJitterMaxDegrees = 2.5f;

        private float _nextShotTime;

        private void Update()
        {
            RefreshTarget();
            if (Target == null) return;

            float dist = HorizontalDistance(transform.position, Target.position);

            if (dist < minShootRange)
                MoveAwayFromPlayer(minShootRange + 0.5f, Time.deltaTime);
            else if (dist > maxShootRange)
                MoveTowardsPlayer(maxShootRange * 0.92f, Time.deltaTime);

            if (dist >= minShootRange && dist <= maxShootRange)
                TryShoot();

            // В боевой дистанции стрелок всегда смотрит на игрока (в т.ч. стоя на месте в коридоре стрельбы).
            if (dist <= maxShootRange)
                FaceTowardPlayer(Time.deltaTime);
        }

        private void TryShoot()
        {
            RefreshTarget();
            if (Target == null) return;

            if (Time.time < _nextShotTime) return;

            Vector3 origin = MuzzleWorld();
            Vector3 aim = Target.position + Vector3.up * aimHeightOffset;

            if (requireLineOfSight)
            {
                bool clear = HasLineOfSightElevated(origin, aim, lineOfSightOriginLift, obstacleMask)
                             || HasLineOfSight(origin, aim, obstacleMask);
                if (!clear)
                    return;
            }

            bool useProjectile = attackMode == EnemyRangedAttackMode.Projectile && projectilePrefab != null;
            bool rolledMiss = Random.value < missChance;

            PlayShootVfx(origin, aim);

            if (useProjectile)
            {
                if (!rolledMiss)
                {
                    Vector3 raw = aim - origin;
                    Vector3 dir = raw.sqrMagnitude > 0.0001f
                        ? GetJitteredDirection(raw.normalized)
                        : transform.forward;
                    Quaternion rot = Quaternion.LookRotation(dir);
                    var proj = Instantiate(projectilePrefab, origin, rot);
                    proj.Initialize(damage, obstacleMask, hitVfxPrefab);
                }
            }
            else
            {
                Vector3 dir = aim - origin;
                float len = dir.magnitude;
                if (len < 0.01f)
                {
                    _nextShotTime = Time.time + fireCooldown;
                    return;
                }

                if (!rolledMiss)
                {
                    Vector3 dirN = GetJitteredDirection(dir / len);
                    float maxDist = Mathf.Max(raycastMaxDistance, maxShootRange + 2f, len);

                    var hits = Physics.RaycastAll(origin, dirN, maxDist, obstacleMask, QueryTriggerInteraction.Ignore);
                    bool dealt = false;
                    RaycastHit? playerHit = null;

                    if (hits != null && hits.Length > 0)
                    {
                        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

                        foreach (var hit in hits)
                        {
                            if (IsThisEnemyCollider(hit.collider))
                                continue;
                            if (hit.collider.GetComponentInParent<FirstPersonController>() == null)
                                continue;

                            EnemyDamage.Apply(hit.collider, damage);
                            dealt = true;
                            playerHit = hit;
                            break;
                        }
                    }

                    if (!dealt && applyDamageIfRayHitsNothing)
                    {
                        EnemyDamage.Apply(Target.gameObject, damage);
                        if (Physics.Raycast(origin, dirN, out RaycastHit envHit, maxDist, obstacleMask,
                                QueryTriggerInteraction.Ignore) && !IsThisEnemyCollider(envHit.collider))
                            PlayHitVfx(envHit.point, envHit.normal);
                        else
                            PlayHitVfx(aim, Vector3.up);
                    }
                    else if (playerHit.HasValue)
                    {
                        PlayHitVfx(playerHit.Value.point, playerHit.Value.normal);
                    }
                }
            }

            _nextShotTime = Time.time + fireCooldown;
        }

        private void PlayShootVfx(Vector3 muzzlePos, Vector3 aimPoint)
        {
            if (shootVfxPrefab == null) return;

            Vector3 p = shootVfxSpawn != null ? shootVfxSpawn.position : muzzlePos;
            Quaternion r;
            if (shootVfxSpawn != null)
                r = shootVfxSpawn.rotation;
            else
                r = (aimPoint - p).sqrMagnitude > 1e-6f
                    ? Quaternion.LookRotation((aimPoint - p).normalized)
                    : transform.rotation;

            SpawnVfxInstance(shootVfxPrefab, p, r);
        }

        private void PlayHitVfx(Vector3 point, Vector3 normal)
        {
            if (hitVfxPrefab == null) return;
            Quaternion r = normal.sqrMagnitude > 1e-6f
                ? Quaternion.LookRotation(normal.normalized)
                : Quaternion.identity;
            SpawnVfxInstance(hitVfxPrefab, point, r);
        }

        private void SpawnVfxInstance(GameObject prefab, Vector3 pos, Quaternion rot)
        {
            var instance = Instantiate(prefab, pos, rot);
            if (!instance.TryGetComponent<ParticleSystem>(out var ps))
                ps = instance.GetComponentInChildren<ParticleSystem>();
            if (ps != null)
            {
                ps.Play();
                float d = ps.main.duration + ps.main.startDelay.constantMax;
                Destroy(instance, Mathf.Max(vfxAutoDestroySeconds, d));
            }
            else
            {
                Destroy(instance, vfxAutoDestroySeconds);
            }
        }

        private Vector3 MuzzleWorld()
        {
            if (muzzle != null) return muzzle.position;
            return transform.position + Vector3.up * aimHeightOffset + transform.forward * 0.3f;
        }

        /// <summary>Случайный поворот единичного направления на небольшой угол (промахи / разброс).</summary>
        private Vector3 GetJitteredDirection(Vector3 dirNormalized)
        {
            if (aimJitterMaxDegrees <= 0.001f)
                return dirNormalized;

            Vector3 rnd = Random.insideUnitSphere;
            Vector3 axis = Vector3.Cross(dirNormalized, rnd);
            if (axis.sqrMagnitude < 1e-8f)
                axis = Vector3.Cross(dirNormalized, Vector3.up);
            axis.Normalize();

            float angle = Random.Range(-aimJitterMaxDegrees, aimJitterMaxDegrees);
            return (Quaternion.AngleAxis(angle, axis) * dirNormalized).normalized;
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0.2f, 0.6f, 1f, 0.25f);
            Vector3 p = transform.position;
            Gizmos.DrawWireSphere(p, minShootRange);
            Gizmos.DrawWireSphere(p, maxShootRange);

            if (attackMode != EnemyRangedAttackMode.Raycast || Target == null) return;
            Vector3 o = MuzzleWorld();
            Vector3 a = Target.position + Vector3.up * aimHeightOffset;
            float maxDist = Mathf.Max(raycastMaxDistance, maxShootRange + 2f);
            Gizmos.color = new Color(1f, 0.3f, 0.2f, 0.85f);
            Gizmos.DrawLine(o, o + (a - o).normalized * Mathf.Min(maxDist, Vector3.Distance(o, a)));
        }
    }
}
