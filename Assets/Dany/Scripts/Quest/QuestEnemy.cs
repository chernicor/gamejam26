using UnityEngine;

namespace Dany
{
    /// <summary>
    /// Повесь на корень врага (рядом с <see cref="Health"/>).
    /// Считает убийство для этапа «зачистка»; при <see cref="isBoss"/> — для этапа «босс».
    /// </summary>
    [DisallowMultipleComponent]
    public class QuestEnemy : MonoBehaviour
    {
        [Tooltip("Если включено — смерть засчитывается как убийство босса, а не обычного врага.")]
        [SerializeField] private bool isBoss;

        [Tooltip("Учитывать в счётчике обычных врагов (этап зачистки). Для босса обычно выключить.")]
        [SerializeField] private bool countsAsRegularEnemy = true;

        private Health _health;
        private bool _subscribed;

        public bool IsBoss => isBoss;

        private void Awake()
        {
            _health = GetComponent<Health>();
            if (_health == null)
                _health = GetComponentInParent<Health>();
        }

        private void OnEnable()
        {
            if (_health == null) return;
            _health.OnDeadEv += OnDead;
            _subscribed = true;
        }

        private void OnDisable()
        {
            if (_health != null && _subscribed)
            {
                _health.OnDeadEv -= OnDead;
                _subscribed = false;
            }
        }

        private void OnDead() => RegisterDestroyedForQuest();

        /// <summary>
        /// Если враг уничтожен не через <see cref="Health.Die"/> (суицид, кастомный скрипт) — вызови перед уничтожением объекта.
        /// </summary>
        public void RegisterDestroyedForQuest()
        {
            if (isBoss)
                QuestEvents.RaiseTrackedEnemyDied(true);
            else if (countsAsRegularEnemy)
                QuestEvents.RaiseTrackedEnemyDied(false);
        }
    }
}
