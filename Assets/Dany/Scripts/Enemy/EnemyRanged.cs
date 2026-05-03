using UnityEngine;
using UnityEngine.AI;

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
    /// Анимации: ходьба/простой — <see cref="EnemyBase.animRunSpeedFloat"/> / <see cref="EnemyBase.animRunBool"/>; смерть — в базе (триггер или состояние + задержка Destroy на Health);
    /// стрельба — поля секции Animation (стрельба) ниже.
    /// </summary>
    public class EnemyRanged : EnemyBase
    {
        [Header("Range")]
        [SerializeField] private float minShootRange = 4f;
        [SerializeField] private float maxShootRange = 22f;
        [Tooltip("Использовать капсулу CharacterController игрока: одна и та же точка для выстрела и LOS (корпус, не голова).")]
        [SerializeField] private bool aimAtPlayerTorsoFromCapsule = true;
        [Tooltip("Точка по вертикали капсулы: 0 = низ, 1 = верх. ~0.35–0.5 = грудь/живот.")]
        [SerializeField, Range(0.15f, 0.65f)] private float playerTorsoAlongCapsule = 0.4f;
        [Tooltip("Запасной вариант, если нет CharacterController: высота от pivot игрока до «корпуса».")]
        [SerializeField] private float aimHeightOffset = 0.95f;
        [Tooltip("Если Muzzle не задан: насколько поднять «дуло» над позицией врага (не путать с прицелом по игроку).")]
        [SerializeField] private float fallbackMuzzleHeight = 1.2f;

        [Header("Shooting")]
        [SerializeField] private EnemyRangedAttackMode attackMode = EnemyRangedAttackMode.Raycast;
        [SerializeField] private float damage = 12f;
        [SerializeField] private float fireCooldown = 1.4f;
        [Tooltip("Если включено — выстрел только при прямой видимости игрока (луч не упирается в стену/землю раньше цели).")]
        [SerializeField] private bool requireLineOfSight = true;
        [Tooltip("При включённом LOS: поднять точку старта луча, чтобы не упираться в землю у ног.")]
        [SerializeField] private float lineOfSightOriginLift = 1.2f;
        [Tooltip("Длина луча при режиме Raycast (должна быть не меньше max shoot range).")]
        [SerializeField] private float raycastMaxDistance = 32f;
        [Tooltip("Только для Raycast: если луч не дошёл до игрока (стена, пустота) — всё равно нанести урон. Включай только для «магического» прострела сквозь укрытия.")]
        [SerializeField] private bool applyDamageIfRayHitsNothing = false;
        [Tooltip("Префаб с компонентом EnemyProjectile (на корне или на дочернем объекте).")]
        [SerializeField] private GameObject projectilePrefab;
        [SerializeField] private Transform muzzle;
        [SerializeField] private LayerMask obstacleMask = ~0;

        [Header("Flanking (когда LOS закрыт стеной)")]
        [Tooltip("Обход по краю дистанции стрельбы: NavMeshAgent идёт к игроку, пока не откроется луч. Без агента — простое движение вперёд (может упираться в геометрию).")]
        [SerializeField] private float flankHoldMargin = 0.35f;
        [Tooltip("Если агент дошёл до цели по графу, но LOS всё ещё нет — сместить точку маршрута вбок от игрока (обход угла).")]
        [SerializeField] private float flankOrbitRadius = 2.75f;
        [SerializeField] private float flankOrbitSampleRadius = 3.5f;
        [SerializeField] private float flankOrbitCooldown = 1.05f;

        [Header("VFX")]
        [Tooltip("Точка появления вспышки выстрела (если пусто — используется Muzzle / расчётная позиция).")]
        [SerializeField] private Transform shootVfxSpawn;
        [SerializeField] private GameObject shootVfxPrefab;
        [SerializeField] private GameObject hitVfxPrefab;
        [SerializeField] private float vfxAutoDestroySeconds = 4f;

        [Header("Spacing (стрелки не толпятся)")]
        [Tooltip("Радиус: отталкиваться от других врагов с EnemyBase, чтобы не стоять вплотную и не перекрывать выстрел.")]
        [SerializeField] private float allySeparationRadius = 2.35f;
        [SerializeField] private float allySeparationStrength = 5f;
        [Tooltip("Если включено — только от других EnemyRanged; иначе от любого EnemyBase.")]
        [SerializeField] private bool separationOnlyRangedAllies = true;

        [Header("Accuracy (misses)")]
        [Tooltip("Вероятность полного промаха за выстрел (0 = всегда в цель, 1 = всегда мимо). Кулдаун всё равно тратится.")]
        [SerializeField, Range(0f, 1f)] private float missChance = 0.15f;
        [Tooltip("Случайный разброс направления луча/снаряда в градусах (0 = идеально в цель).")]
        [SerializeField, Range(0f, 15f)] private float aimJitterMaxDegrees = 2.5f;

        [Header("Animation (стрельба)")]
        [Tooltip("Триггер выстрела в Animator. Нужны переходы из Idle и из Walk/Run.")]
        [SerializeField] private string animShootTrigger = "";
        [Tooltip("Имя оранжевого состояния выстрела в Animator (как в окне). Заполни обязательно — стрелок часто в Idle, триггер без перехода из Idle не сработает. Под-машина: путь вида Combat.Shoot.")]
        [SerializeField] private string animShootStateName = "";
        [SerializeField] private int shootAnimatorLayer = 0;
        [Tooltip("Смягчение входа в Shoot, если задано имя состояния: 0 = мгновенный Play (надёжнее).")]
        [SerializeField, Min(0f)] private float shootCrossFadeDuration = 0f;
        [Tooltip("Не перебивать выстрел параметрами ходьбы столько секунд (≈ длина клипа стрельбы).")]
        [SerializeField] private float shootAnimationDuration = 0.5f;
        [Tooltip("На это же время не двигаться к игроку / от него и не отталкиваться от союзников (пока идёт выстрел).")]
        [SerializeField] private bool freezeMovementWhileShooting = true;
        [Tooltip("Если > 0 — замирать дольше анимации (сек). Иначе длительность как у Shoot Animation Duration.")]
        [SerializeField] private float shootMovementLockExtraSeconds = 0f;

        private float _nextShotTime;
        private float _shootMovementLockUntil;
        private float _nextFlankOrbitTime;
        private float _flankOrbitSide = 1f;
        private bool _flankOrbitPathPending;
        private int _animHashShoot = -1;

        protected override void Awake()
        {
            base.Awake();
            RebuildShootAnimHash();
        }

        private void RebuildShootAnimHash()
        {
            _animHashShoot = string.IsNullOrEmpty(animShootTrigger) ? -1 : Animator.StringToHash(animShootTrigger);
        }

        private void Update()
        {
            if (IsDead) return;

            RefreshTarget();
            if (Target == null) return;

            float dist = HorizontalDistance(transform.position, Target.position);

            if (IsShootMovementLocked())
            {
                StopNavAgentForShootLock();
                if (dist <= maxShootRange)
                    FaceTowardPlayer(Time.deltaTime);
                return;
            }

            if (dist < minShootRange)
                MoveAwayFromPlayer(minShootRange + 0.5f, Time.deltaTime);
            else if (dist > maxShootRange)
                MoveTowardsPlayer(maxShootRange * 0.92f, Time.deltaTime);
            else if (requireLineOfSight && !HasShootingLineOfSight())
                UpdateFlankWhileBlocked(Time.deltaTime);
            else
                TryShoot();

            // В боевой дистанции стрелок всегда смотрит на игрока (в т.ч. стоя на месте в коридоре стрельбы).
            if (dist <= maxShootRange)
                FaceTowardPlayer(Time.deltaTime);

            ApplyAllySeparation(Time.deltaTime);
        }

        private bool IsShootMovementLocked()
        {
            return freezeMovementWhileShooting && Time.time < _shootMovementLockUntil;
        }

        private void StopNavAgentForShootLock()
        {
            var agent = NavAgent;
            if (agent == null || !agent.enabled || !agent.isOnNavMesh) return;
            agent.isStopped = true;
        }

        private void BeginShootMovementLock()
        {
            if (!freezeMovementWhileShooting) return;
            float d = shootAnimationDuration + Mathf.Max(0f, shootMovementLockExtraSeconds);
            float until = Time.time + d;
            if (until > _shootMovementLockUntil)
                _shootMovementLockUntil = until;
            StopNavAgentForShootLock();
        }

        /// <summary>Та же проверка, что перед выстрелом: дуло видит точку прицела у игрока.</summary>
        private bool HasShootingLineOfSight()
        {
            if (Target == null) return false;
            Vector3 origin = MuzzleWorld();
            Vector3 aim = GetPlayerAimWorldPoint();
            return HasLineOfSightElevated(origin, aim, lineOfSightOriginLift, obstacleMask)
                   || HasLineOfSight(origin, aim, obstacleMask);
        }

        /// <summary>Идём к игроку по NavMesh (обход стен) или смещаем цель вбок, если упёрлись в угол без LOS.</summary>
        private void UpdateFlankWhileBlocked(float deltaTime)
        {
            var agent = NavAgent;

            if (_flankOrbitPathPending && agent != null && agent.enabled && agent.isOnNavMesh)
            {
                if (agent.pathPending)
                    return;
                if (agent.hasPath && agent.remainingDistance > agent.stoppingDistance + 0.12f)
                    return;
                _flankOrbitPathPending = false;
            }

            if (agent != null && agent.enabled && agent.isOnNavMesh
                && Time.time >= _nextFlankOrbitTime
                && !agent.pathPending
                && agent.pathStatus == NavMeshPathStatus.PathComplete
                && agent.remainingDistance <= agent.stoppingDistance + 0.15f
                && agent.velocity.sqrMagnitude < 0.06f
                && TrySetFlankOrbitDestination(agent))
            {
                _nextFlankOrbitTime = Time.time + flankOrbitCooldown;
                _flankOrbitSide = -_flankOrbitSide;
                _flankOrbitPathPending = true;
                return;
            }

            float stop = Mathf.Max(0.6f, minShootRange + flankHoldMargin);
            MoveTowardsPlayer(stop, deltaTime);
        }

        private bool TrySetFlankOrbitDestination(NavMeshAgent agent)
        {
            if (Target == null) return false;

            Vector3 flat = Target.position - transform.position;
            flat.y = 0f;
            if (flat.sqrMagnitude < 0.01f) return false;

            Vector3 tangent = Vector3.Cross(Vector3.up, flat.normalized);
            Vector3 candidate = Target.position + tangent * (_flankOrbitSide * flankOrbitRadius);
            if (NavMesh.SamplePosition(candidate, out NavMeshHit hit, flankOrbitSampleRadius, NavMesh.AllAreas))
            {
                agent.SetDestination(hit.position);
                return true;
            }

            return false;
        }

        private void TryShoot()
        {
            RefreshTarget();
            if (Target == null) return;

            if (Time.time < _nextShotTime) return;

            Vector3 origin = MuzzleWorld();
            Vector3 aim = GetPlayerAimWorldPoint();

            if (requireLineOfSight && !HasShootingLineOfSight())
                return;

            bool useProjectile = attackMode == EnemyRangedAttackMode.Projectile
                                 && projectilePrefab != null
                                 && projectilePrefab.GetComponentInChildren<EnemyProjectile>(true) != null;
            bool rolledMiss = Random.value < missChance;

            PlayShootVfx(origin, aim);
            PlayFmodAttackAt(origin);
            PlayShootAnimation();
            SuppressLocomotionAnimation(shootAnimationDuration);
            BeginShootMovementLock();

            if (useProjectile)
            {
                if (!rolledMiss)
                {
                    Vector3 raw = aim - origin;
                    Vector3 dir = raw.sqrMagnitude > 0.0001f
                        ? GetJitteredDirection(raw.normalized)
                        : transform.forward;
                    Quaternion rot = Quaternion.LookRotation(dir);
                    var go = Instantiate(projectilePrefab, origin, rot);
                    var proj = go.GetComponentInChildren<EnemyProjectile>(true);
                    if (proj == null)
                    {
                        Destroy(go);
                        _nextShotTime = Time.time + fireCooldown;
                        return;
                    }

                    proj.Initialize(damage, obstacleMask, hitVfxPrefab, this);
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

                            if (IsOtherEnemyCollider(hit.collider))
                                continue;

                            if (hit.collider.GetComponentInParent<FirstPersonController>() != null)
                            {
                                EnemyDamage.Apply(hit.collider, damage);
                                dealt = true;
                                playerHit = hit;
                                break;
                            }

                            // Первое препятствие на пути — не игрок: луч не «просвечивает» дальше.
                            break;
                        }
                    }

                    if (!dealt && applyDamageIfRayHitsNothing)
                    {
                        EnemyDamage.Apply(Target.gameObject, damage);
                        if (Physics.Raycast(origin, dirN, out RaycastHit envHit, maxDist, obstacleMask,
                                QueryTriggerInteraction.Ignore)
                            && !IsThisEnemyCollider(envHit.collider)
                            && !IsOtherEnemyCollider(envHit.collider))
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

        private void PlayShootAnimation()
        {
            var anim = enemyAnimator != null
                ? enemyAnimator
                : GetComponentInChildren<Animator>(true);

            if (anim == null || !anim.isActiveAndEnabled || anim.runtimeAnimatorController == null)
            {
#if UNITY_EDITOR
                if (!string.IsNullOrEmpty(animShootTrigger) || !string.IsNullOrEmpty(animShootStateName))
                    Debug.LogWarning($"{nameof(EnemyRanged)} на «{name}»: нет Animator, он выключен или нет Controller — анимация стрельбы не сыграет.", this);
#endif
                return;
            }

            if (!string.IsNullOrEmpty(animShootStateName))
            {
                if (shootCrossFadeDuration > 0.001f)
                    anim.CrossFadeInFixedTime(animShootStateName, shootCrossFadeDuration, shootAnimatorLayer, 0f);
                else
                    anim.Play(animShootStateName, shootAnimatorLayer, 0f);
                return;
            }

            if (_animHashShoot < 0)
            {
#if UNITY_EDITOR
                Debug.LogWarning(
                    $"{nameof(EnemyRanged)} на «{name}»: задай {nameof(animShootTrigger)} и переходы Idle/Walk→Shoot в Animator, либо укажи {nameof(animShootStateName)} (имя состояния).",
                    this);
#endif
                return;
            }

            anim.ResetTrigger(_animHashShoot);
            anim.SetTrigger(_animHashShoot);
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
            return transform.position + Vector3.up * fallbackMuzzleHeight + transform.forward * 0.3f;
        }

        /// <summary>Одна точка для луча видимости и направления выстрела — корпус игрока (капсула), не голова.</summary>
        private Vector3 GetPlayerAimWorldPoint()
        {
            if (Target == null) return Vector3.zero;

            if (aimAtPlayerTorsoFromCapsule)
            {
                var cc = Target.GetComponentInParent<CharacterController>();
                if (cc != null && cc.height > 0.05f)
                {
                    Transform capRoot = cc.transform;
                    Vector3 worldCenter = capRoot.TransformPoint(cc.center);
                    float half = cc.height * 0.5f;
                    Vector3 bottom = worldCenter - Vector3.up * half;
                    float t = Mathf.Clamp01(playerTorsoAlongCapsule);
                    return bottom + Vector3.up * (cc.height * t);
                }
            }

            return Target.position + Vector3.up * aimHeightOffset;
        }

        private void ApplyAllySeparation(float deltaTime)
        {
            if (allySeparationRadius <= 0.1f || allySeparationStrength <= 0f)
                return;

            Vector3 center = transform.position + Vector3.up * 0.65f;
            var cols = Physics.OverlapSphere(center, allySeparationRadius, ~0,
                QueryTriggerInteraction.Ignore);
            if (cols == null || cols.Length == 0)
                return;

            Vector3 push = Vector3.zero;
            int n = 0;
            foreach (var c in cols)
            {
                if (c == null) continue;
                var ally = c.GetComponentInParent<EnemyBase>();
                if (ally == null || ally == this) continue;
                if (separationOnlyRangedAllies && ally.GetComponent<EnemyRanged>() == null)
                    continue;

                Vector3 delta = transform.position - c.transform.position;
                delta.y = 0f;
                float d = delta.magnitude;
                if (d < 0.05f)
                    delta = Random.insideUnitSphere;
                delta.y = 0f;
                if (delta.sqrMagnitude < 1e-6f) continue;
                delta.Normalize();
                float w = 1f - Mathf.Clamp01(d / allySeparationRadius);
                push += delta * (w * w);
                n++;
            }

            if (n == 0 || push.sqrMagnitude < 1e-6f)
                return;

            push = push.normalized * (allySeparationStrength * deltaTime);

            var agent = NavAgent;
            if (agent != null && agent.enabled && agent.isOnNavMesh)
                agent.Move(push);
            else
                transform.position += push;
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
            Vector3 a = GetPlayerAimWorldPoint();
            float maxDist = Mathf.Max(raycastMaxDistance, maxShootRange + 2f);
            Gizmos.color = new Color(1f, 0.3f, 0.2f, 0.85f);
            Gizmos.DrawLine(o, o + (a - o).normalized * Mathf.Min(maxDist, Vector3.Distance(o, a)));
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            RebuildShootAnimHash();
            if (projectilePrefab == null) return;
            if (attackMode != EnemyRangedAttackMode.Projectile) return;
            if (projectilePrefab.GetComponentInChildren<EnemyProjectile>(true) != null) return;
            Debug.LogWarning(
                $"EnemyRanged на «{name}»: режим Projectile, но у префаба «{projectilePrefab.name}» нет компонента EnemyProjectile (ни на корне, ни на детях).",
                this);
        }
#endif
    }
}
