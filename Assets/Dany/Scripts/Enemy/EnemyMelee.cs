using UnityEngine;

namespace Dany
{
    /// <summary>
    /// Подходит к игроку и наносит урон в радиусе ближнего боя.
    /// Анимации: в базе — триггер смерти и бег; в инспекторе мили — триггер атаки при ударе.
    /// </summary>
    public class EnemyMelee : EnemyBase
    {
        [Header("Melee")]
        [SerializeField] private float attackRange = 1.5f;
        [SerializeField] private float attackDamage = 15f;
        [SerializeField] private float attackCooldown = 1.1f;

        [Header("Animation (ближний бой)")]
        [Tooltip("Триггер атаки (имя параметра в Animator). Нужен переход в Attack и из Idle, и из Run — у цели агент стоит, часто активен Idle.")]
        [SerializeField] private string animAttackTrigger = "";
        [Tooltip("Если задано — атака через CrossFade в это состояние (имя как в окне Animator), надёжно с Idle. Пусто — только триггер.")]
        [SerializeField] private string animAttackStateName = "";
        [SerializeField] private int attackAnimatorLayer = 0;
        [SerializeField, Min(0f)] private float attackCrossFadeDuration = 0.08f;
        [Tooltip("Сколько секунд не слать бег в Animator после удара, чтобы клип атаки успел проиграться (≈ длина анимации).")]
        [SerializeField] private float attackAnimationDuration = 0.65f;

        private float _nextAttackTime;
        private int _animHashAttack = -1;

        protected override void Awake()
        {
            base.Awake();
            RebuildAttackAnimHash();
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            RebuildAttackAnimHash();
        }
#endif

        private void RebuildAttackAnimHash()
        {
            _animHashAttack = string.IsNullOrEmpty(animAttackTrigger) ? -1 : Animator.StringToHash(animAttackTrigger);
        }

        private void Update()
        {
            if (IsDead) return;

            RefreshTarget();
            if (Target == null) return;

            if (MoveTowardsPlayer(attackRange, Time.deltaTime))
            {
                if (Time.time >= _nextAttackTime)
                {
                    float dist = HorizontalDistance(transform.position, Target.position);
                    if (dist <= attackRange)
                    {
                        EnemyDamage.Apply(Target.gameObject, attackDamage);
                        PlayAttackAnimation();
                        SuppressLocomotionAnimation(attackAnimationDuration);
                        _nextAttackTime = Time.time + attackCooldown;
                    }
                }
            }
        }

        private void PlayAttackAnimation()
        {
            var anim = enemyAnimator != null
                ? enemyAnimator
                : GetComponentInChildren<Animator>(true);

            if (anim == null || !anim.isActiveAndEnabled)
            {
#if UNITY_EDITOR
                Debug.LogWarning($"{nameof(EnemyMelee)} на «{name}»: нет активного Animator — анимация атаки не сыграет.", this);
#endif
                return;
            }

            if (!string.IsNullOrEmpty(animAttackStateName))
            {
                anim.CrossFadeInFixedTime(animAttackStateName, attackCrossFadeDuration, attackAnimatorLayer, 0f);
                return;
            }

            if (_animHashAttack < 0)
            {
#if UNITY_EDITOR
                Debug.LogWarning(
                    $"{nameof(EnemyMelee)} на «{name}»: не задан ни {nameof(animAttackTrigger)}, ни {nameof(animAttackStateName)}.", this);
#endif
                return;
            }

            anim.ResetTrigger(_animHashAttack);
            anim.SetTrigger(_animHashAttack);
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(1f, 0.4f, 0.2f, 0.6f);
            Gizmos.DrawWireSphere(transform.position + Vector3.up * 0.5f, attackRange);
        }
    }
}
