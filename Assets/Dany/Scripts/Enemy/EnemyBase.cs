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

        protected Transform Target { get; private set; }
        private NavMeshAgent _navAgent;

        protected virtual void Awake()
        {
            _navAgent = GetComponent<NavMeshAgent>();
            if (_navAgent != null)
                _navAgent.updateRotation = !facePlayerWhenMoving;

            RefreshTarget();
        }

        protected virtual void OnEnable()
        {
            TryWarpOntoNavMesh();
            RefreshTarget();
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

                // Первое не-«я» по пути: если это игрок — видим; иначе преграда (земля, стена).
                return hit.collider.GetComponentInParent<FirstPersonController>() != null;
            }

            return true;
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
    }
}
