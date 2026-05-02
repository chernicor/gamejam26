using UnityEngine;
using UnityEngine.AI;

namespace Dany
{
    /// <summary>
    /// Поиск цели (игрок) и движение по плоскости XZ.
    /// Если на объекте есть <see cref="NavMeshAgent"/> и он на NavMesh — используется обход препятствий.
    /// </summary>
    public abstract class EnemyBase : MonoBehaviour
    {
        [Header("Target")]
        [Tooltip("Если пусто — ищется FirstPersonController в сцене.")]
        [SerializeField] protected Transform targetOverride;

        [Header("Movement")]
        [SerializeField] protected float moveSpeed = 3.5f;
        [SerializeField] protected float turnSpeed = 540f;
        [SerializeField] protected bool facePlayerWhenMoving = true;

        [Header("Facing (chase)")]
        [Tooltip("Высота точки «глаз» для проверки видимости игрока при повороте во время преследования.")]
        [SerializeField] protected float chasingLosEyeHeight = 1.4f;
        [SerializeField] protected float chasingLosTargetHeight = 1.2f;
        [SerializeField] protected LayerMask chasingLineOfSightMask = ~0;

        [Header("Nav Mesh (optional)")]
        [Tooltip("Добавь NavMesh Agent на префаб врага и запеки NavMesh в сцене — враг пойдёт в обход.")]
        [SerializeField] protected float navMeshSampleRadius = 4f;

        [Header("Animation (Animator)")]
        [Tooltip("Пусто — будет поиск Animator в дочерних объектах.")]
        [SerializeField] protected Animator enemyAnimator;
        [Tooltip("Триггер смерти (по событию Health). Можно оставить пустым, если задаёшь только Anim Death State Name.")]
        [SerializeField] protected string animDeathTrigger = "";
        [Tooltip("Прямой переход в состояние смерти (имя в Animator). Надёжно, если триггер из Idle не настроен.")]
        [SerializeField] protected string animDeathStateName = "";
        [SerializeField] protected int deathAnimatorLayer = 0;
        [SerializeField, Min(0f)] protected float deathCrossFadeDuration = 0.1f;
        [Tooltip("Отключить коллайдеры при смерти (не бить игрока и не мешать).")]
        [SerializeField] protected bool disableCollidersOnDeath = true;
        [Tooltip("Float: скорость по плоскости (0 = стояние). Часто Speed или MoveSpeed.")]
        [SerializeField] protected string animRunSpeedFloat = "";
        [Tooltip("Bool: true, когда враг движется. Альтернатива или дополнение к Float.")]
        [SerializeField] protected string animRunBool = "";
        [SerializeField] protected float animRunSpeedThreshold = 0.08f;
        [SerializeField] protected float animRunSpeedNormalize = 3.5f;

        protected Transform Target { get; private set; }
        private NavMeshAgent _navAgent;

        private Vector3 _animLastPlanarPos;
        private int _animHashDeath = -1;
        private int _animHashRunSpeed = -1;
        private int _animHashRunBool = -1;
        private Health _healthForAnim;
        private bool _subscribedHealthDead;

        /// <summary>HP ≤ 0: логика врага отключена, играется смерть до Destroy на <see cref="Health"/>.</summary>
        protected bool IsDead { get; private set; }
        private bool _deathSequenceStarted;

        /// <summary>
        /// Пока Time.time меньше этого значения, в Animator подаётся скорость 0 (бег не перебивает атаку и т.п.).
        /// </summary>
        private float _animLocomotionSuppressUntil;

        /// <summary>NavMesh Agent с этого врага (если есть) — для логики обхода в наследниках.</summary>
        protected NavMeshAgent NavAgent => _navAgent;

        /// <summary>
        /// Временно не обновлять бег/idle в Animator (например на длину клипа атаки). 0 или меньше — игнорировать.
        /// </summary>
        protected void SuppressLocomotionAnimation(float durationSeconds)
        {
            if (durationSeconds <= 0f) return;
            float until = Time.time + durationSeconds;
            if (until > _animLocomotionSuppressUntil)
                _animLocomotionSuppressUntil = until;
        }

        protected virtual void Awake()
        {
            _navAgent = GetComponent<NavMeshAgent>();
            if (_navAgent != null)
                _navAgent.updateRotation = !facePlayerWhenMoving;

            RefreshTarget();

            if (enemyAnimator == null)
                enemyAnimator = GetComponentInChildren<Animator>(true);

            CacheAnimatorParamHashes();
            _animLastPlanarPos = transform.position;

            _healthForAnim = GetComponent<Health>() ?? GetComponentInParent<Health>();
        }

        protected virtual void OnEnable()
        {
            TryWarpOntoNavMesh();
            RefreshTarget();

            TrySubscribeHealthDeath();
        }

        protected virtual void OnDisable()
        {
            UnsubscribeHealthDeath();
        }

        private void CacheAnimatorParamHashes()
        {
            _animHashDeath = string.IsNullOrEmpty(animDeathTrigger) ? -1 : Animator.StringToHash(animDeathTrigger);
            _animHashRunSpeed = string.IsNullOrEmpty(animRunSpeedFloat) ? -1 : Animator.StringToHash(animRunSpeedFloat);
            _animHashRunBool = string.IsNullOrEmpty(animRunBool) ? -1 : Animator.StringToHash(animRunBool);
        }

        private void TrySubscribeHealthDeath()
        {
            if (_healthForAnim == null || _subscribedHealthDead) return;
            _healthForAnim.OnDeadEv += OnEnemyHealthDead;
            _subscribedHealthDead = true;
        }

        private void UnsubscribeHealthDeath()
        {
            if (_healthForAnim == null || !_subscribedHealthDead) return;
            _healthForAnim.OnDeadEv -= OnEnemyHealthDead;
            _subscribedHealthDead = false;
        }

        private void OnEnemyHealthDead()
        {
            BeginEnemyDeathSequence();
        }

        /// <summary>
        /// Остановка AI, коллайдеров, проигрывание смерти. Вызывается из Health.OnDeadEv до задержанного Destroy.
        /// </summary>
        protected virtual void BeginEnemyDeathSequence()
        {
            if (_deathSequenceStarted) return;
            _deathSequenceStarted = true;
            IsDead = true;

            if (_navAgent != null)
            {
                _navAgent.isStopped = true;
                _navAgent.ResetPath();
                _navAgent.enabled = false;
            }

            if (disableCollidersOnDeath)
            {
                foreach (var col in GetComponentsInChildren<Collider>())
                {
                    if (col != null) col.enabled = false;
                }
            }

            SuppressLocomotionAnimation(999f);
            PlayDeathAnimation();
        }

        /// <summary>Триггер или CrossFade состояния смерти в Animator.</summary>
        protected void PlayDeathAnimation()
        {
            if (enemyAnimator == null)
                enemyAnimator = GetComponentInChildren<Animator>(true);
            if (enemyAnimator == null || !enemyAnimator.isActiveAndEnabled) return;

            if (!string.IsNullOrEmpty(animDeathStateName))
            {
                enemyAnimator.CrossFadeInFixedTime(animDeathStateName, deathCrossFadeDuration, deathAnimatorLayer, 0f);
                return;
            }

            if (_animHashDeath < 0) return;
            enemyAnimator.ResetTrigger(_animHashDeath);
            enemyAnimator.SetTrigger(_animHashDeath);
        }

        /// <summary>Обновить бег / стояние: float ≈ скорость (0 = idle), bool по порогу.</summary>
        protected void UpdateLocomotionAnimation()
        {
            if (enemyAnimator == null) return;

            float planarSpeed = Time.time < _animLocomotionSuppressUntil ? 0f : GetPlanarSpeedForAnimation();

            if (_animHashRunSpeed >= 0)
            {
                float n = animRunSpeedNormalize > 0.01f ? planarSpeed / animRunSpeedNormalize : planarSpeed;
                enemyAnimator.SetFloat(_animHashRunSpeed, Mathf.Clamp01(n));
            }

            if (_animHashRunBool >= 0)
                enemyAnimator.SetBool(_animHashRunBool, planarSpeed > animRunSpeedThreshold);
        }

        private float GetPlanarSpeedForAnimation()
        {
            if (_navAgent != null && _navAgent.enabled && _navAgent.isOnNavMesh)
            {
                Vector3 v = _navAgent.velocity;
                return new Vector3(v.x, 0f, v.z).magnitude;
            }

            Vector3 p = transform.position;
            Vector3 delta = new Vector3(p.x - _animLastPlanarPos.x, 0f, p.z - _animLastPlanarPos.z);
            float dt = Time.deltaTime;
            return dt > 1e-6f ? delta.magnitude / dt : 0f;
        }

        protected virtual void LateUpdate()
        {
            if (IsDead) return;
            UpdateLocomotionAnimation();
            _animLastPlanarPos = transform.position;
        }

        private void TryWarpOntoNavMesh()
        {
            if (_navAgent == null || !_navAgent.enabled) return;
            if (_navAgent.isOnNavMesh) return;
            if (NavMesh.SamplePosition(transform.position, out NavMeshHit hit, navMeshSampleRadius, NavMesh.AllAreas))
                _navAgent.Warp(hit.position);
        }

        protected void RefreshTarget()
        {
            if (targetOverride != null)
            {
                Target = targetOverride;
                return;
            }

            if (Target != null) return;

            var fpc = FindObjectOfType<FirstPersonController>();
            if (fpc != null)
                Target = fpc.transform;
        }

        /// <summary>
        /// Движется к игроку, пока горизонтальная дистанция больше stopDistance.
        /// Возвращает true, если уже в пределах stopDistance.
        /// </summary>
        protected bool MoveTowardsPlayer(float stopDistance, float deltaTime)
        {
            RefreshTarget();
            if (Target == null) return false;

            if (TryMoveWithNavMesh(Target.position, stopDistance, deltaTime, out float distToTarget))
                return distToTarget <= stopDistance;

            Vector3 self = transform.position;
            Vector3 t = Target.position;
            Vector3 flatSelf = new Vector3(self.x, 0f, self.z);
            Vector3 flatT = new Vector3(t.x, 0f, t.z);
            Vector3 dir = flatT - flatSelf;
            float dist = dir.magnitude;
            if (dist <= stopDistance)
            {
                ApplyChasingFacing(deltaTime, flatGlideDir: null);
                return true;
            }

            dir /= dist;
            transform.position += dir * moveSpeed * deltaTime;

            ApplyChasingFacing(deltaTime, dir);
            return false;
        }

        /// <summary>
        /// Отходит от игрока, пока дистанция меньше minDistance.
        /// </summary>
        protected bool MoveAwayFromPlayer(float minDistance, float deltaTime)
        {
            RefreshTarget();
            if (Target == null) return false;

            Vector3 self = transform.position;
            Vector3 t = Target.position;
            Vector3 flatSelf = new Vector3(self.x, 0f, self.z);
            Vector3 flatT = new Vector3(t.x, 0f, t.z);
            Vector3 away = flatSelf - flatT;
            float dist = away.magnitude;

            if (_navAgent != null && _navAgent.enabled && _navAgent.isOnNavMesh)
            {
                _navAgent.speed = moveSpeed;
                _navAgent.isStopped = false;
                _navAgent.stoppingDistance = 0.5f;

                if (dist >= minDistance)
                    return true;

                if (away.sqrMagnitude < 0.0001f)
                    away = transform.forward;
                else
                    away.Normalize();

                Vector3 fleeFlat = flatSelf + away * Mathf.Max(minDistance + 3f, 5f);
                if (NavMesh.SamplePosition(fleeFlat, out NavMeshHit nmHit, navMeshSampleRadius + 2f, NavMesh.AllAreas))
                    _navAgent.SetDestination(nmHit.position);
                else
                    _navAgent.SetDestination(fleeFlat);

                ApplyFleeFacing(deltaTime, away);
                return HorizontalDistance(transform.position, Target.position) >= minDistance;
            }

            if (dist >= minDistance) return true;

            if (dist < 0.001f) away = Vector3.forward;
            else away /= dist;

            transform.position += away * moveSpeed * deltaTime;

            ApplyFleeFacing(deltaTime, away);
            return false;
        }

        /// <summary>
        /// Возвращает true, если движение полностью обрабатывается NavMesh Agent.
        /// </summary>
        private bool TryMoveWithNavMesh(Vector3 destination, float stopDistance, float deltaTime, out float horizontalDistToTarget)
        {
            horizontalDistToTarget = 0f;
            if (_navAgent == null || !_navAgent.enabled || !_navAgent.isOnNavMesh)
                return false;

            _navAgent.speed = moveSpeed;
            _navAgent.stoppingDistance = Mathf.Max(0.05f, stopDistance);
            _navAgent.isStopped = false;
            _navAgent.SetDestination(destination);

            horizontalDistToTarget = Target != null
                ? HorizontalDistance(transform.position, Target.position)
                : 0f;

            ApplyChasingFacing(deltaTime, flatGlideDir: null);
            return true;
        }

        /// <summary>Виден ли игрок лучом для правил поворота при преследовании.</summary>
        protected bool HasLineOfSightToTargetForFacing()
        {
            RefreshTarget();
            if (Target == null) return false;
            Vector3 from = transform.position + Vector3.up * chasingLosEyeHeight;
            Vector3 to = Target.position + Vector3.up * chasingLosTargetHeight;
            return HasLineOfSight(from, to, chasingLineOfSightMask);
        }

        /// <summary>Преследование: при видимости игрока — лицом к нему, иначе по направлению движения (NavMesh velocity или скольжение).</summary>
        protected void ApplyChasingFacing(float deltaTime, Vector3? flatGlideDir)
        {
            if (Target == null) return;

            if (HasLineOfSightToTargetForFacing())
            {
                FaceTowardPlayer(deltaTime);
                return;
            }

            if (flatGlideDir.HasValue && flatGlideDir.Value.sqrMagnitude > 0.0001f)
            {
                FaceFlatDirection(deltaTime, flatGlideDir.Value);
                return;
            }

            if (_navAgent != null && _navAgent.isOnNavMesh && _navAgent.velocity.sqrMagnitude > 0.01f)
            {
                Vector3 v = Vector3.ProjectOnPlane(_navAgent.velocity, Vector3.up);
                if (v.sqrMagnitude > 0.0001f)
                    FaceFlatDirection(deltaTime, v);
            }
        }

        /// <summary>Отход: смотреть в сторону бегства (не сквозь стену «на игрока»).</summary>
        protected void ApplyFleeFacing(float deltaTime, Vector3 flatAwayFromPlayer)
        {
            if (flatAwayFromPlayer.sqrMagnitude < 0.0001f) return;
            FaceFlatDirection(deltaTime, flatAwayFromPlayer);
        }

        /// <summary>Всегда повернуть «лоб» по горизонтали к игроку (для стрелка в зоне и т.п.).</summary>
        protected void FaceTowardPlayer(float deltaTime)
        {
            if (Target == null) return;
            Vector3 look = Target.position - transform.position;
            look.y = 0f;
            if (look.sqrMagnitude < 0.0001f) return;
            Quaternion want = Quaternion.LookRotation(look.normalized, Vector3.up);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, want, turnSpeed * deltaTime);
        }

        private void FaceFlatDirection(float deltaTime, Vector3 flatDirection)
        {
            if (flatDirection.sqrMagnitude < 0.0001f) return;
            Quaternion want = Quaternion.LookRotation(flatDirection.normalized, Vector3.up);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, want, turnSpeed * deltaTime);
        }

        private void FaceMovementDirection(Vector3 flatDirection, float deltaTime)
        {
            FaceFlatDirection(deltaTime, flatDirection);
        }

        protected static float HorizontalDistance(Vector3 a, Vector3 b)
        {
            a.y = 0f;
            b.y = 0f;
            return Vector3.Distance(a, b);
        }

        /// <summary>
        /// Луч без коллайдеров-триггеров; считаем попадание в игрока не препятствием.
        /// Попадания в этот же объект (свой коллайдер / дуло внутри капсулы) пропускаются.
        /// </summary>
        protected bool HasLineOfSight(Vector3 from, Vector3 to, LayerMask obstacleMask)
        {
            RefreshTarget();
            if (Target == null) return false;

            Vector3 dir = to - from;
            float dist = dir.magnitude;
            if (dist < 0.01f) return true;
            dir /= dist;

            var hits = Physics.RaycastAll(from, dir, dist, obstacleMask, QueryTriggerInteraction.Ignore);
            if (hits == null || hits.Length == 0)
                return true;

            System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

            foreach (var hit in hits)
            {
                if (IsThisEnemyCollider(hit.collider))
                    continue;

                // Другие враги на линии не заслоняют игрока (дружественный огонь / толпа).
                if (IsOtherEnemyCollider(hit.collider))
                    continue;

                // Первое не-«я» и не союзник: если это игрок — видим; иначе преграда (стена, земля).
                return hit.collider.GetComponentInParent<FirstPersonController>() != null;
            }

            // Все попадания только «я» или другие AI — ни стены, ни коллайдера игрока на луче.
            // Раньше первый враг давал «не видно» и включался обход; true здесь давало бы стрельбу в стену.
            bool anyNonSelfHit = false;
            for (int i = 0; i < hits.Length; i++)
            {
                if (!IsThisEnemyCollider(hits[i].collider))
                {
                    anyNonSelfHit = true;
                    break;
                }
            }

            if (!anyNonSelfHit)
                return true;

            return false;
        }

        /// <summary>Луч к игроку с приподнятой точки (реже цепляет землю у ног).</summary>
        protected bool HasLineOfSightElevated(Vector3 from, Vector3 to, float originLift, LayerMask obstacleMask)
        {
            return HasLineOfSight(from + Vector3.up * originLift, to, obstacleMask);
        }

        /// <summary>Попадание в коллайдер этого врага (чтобы луч не «бился» о себя).</summary>
        protected bool IsThisEnemyCollider(Collider col)
        {
            if (col == null) return false;
            return col.transform == transform || col.transform.IsChildOf(transform);
        }

        /// <summary>Коллайдер принадлежит другому экземпляру с <see cref="EnemyBase"/> (союзный AI).</summary>
        protected bool IsOtherEnemyCollider(Collider col)
        {
            if (col == null) return false;
            var other = col.GetComponentInParent<EnemyBase>();
            return other != null && other != this;
        }
    }
}
