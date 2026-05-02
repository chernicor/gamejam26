using System.Collections.Generic;
using FMODUnity;
using SiberianGJ26.YouAreDoing.Antos.Modules;
using UnityEngine;

namespace Dany
{
    /// <summary>
    /// Пока рядом живой враг (<see cref="EnemyBase"/>), через случайные промежутки проигрывает FMOD-события (реплики).
    /// Если рядом живой босс (<see cref="QuestEnemy.IsBoss"/>), берутся события из <see cref="bossBarkEvents"/> (если список не пуст).
    /// Повесь на корень игрока рядом с <see cref="MonoHealth"/>.
    /// </summary>
    public class CombatVoiceBarks : MonoBehaviour
    {
        [SerializeField] private MonoHealth playerHealth;
        [Tooltip("Радиус поиска врагов от игрока.")]
        [SerializeField] private float detectionRadius = 18f;
        [SerializeField] private LayerMask enemyDetectionLayers = ~0;

        [Tooltip("Минимум секунд между фразами.")]
        [SerializeField] private float minIntervalSeconds = 10f;
        [Tooltip("Максимум секунд между фразами.")]
        [SerializeField] private float maxIntervalSeconds = 22f;

        [Tooltip("FMOD one-shot: обычный бой. Пустые записи пропускаются.")]
        [SerializeField] private EventReference[] combatBarkEvents;

        [Tooltip("FMOD one-shot: рядом босс (QuestEnemy с IsBoss). Если пусто — играются только обычные реплики.")]
        [SerializeField] private EventReference[] bossBarkEvents;

        private float _nextBarkAllowedTime;
        private readonly List<int> _validIndices = new List<int>(8);

        private void Awake()
        {
            if (playerHealth == null)
                playerHealth = GetComponent<MonoHealth>() ?? GetComponentInParent<MonoHealth>();

            ScheduleNextBark();
        }

        private void Update()
        {
            if (GamePause.IsPaused) return;
            if (playerHealth == null || !playerHealth.IsAlive) return;
            if (!HasAnyConfiguredBarks()) return;
            if (Time.time < _nextBarkAllowedTime) return;
            if (!ScanNearbyEnemies(out bool bossInRange)) return;

            TryPlayRandomBark(bossInRange);
            ScheduleNextBark();
        }

        private void ScheduleNextBark()
        {
            float lo = Mathf.Min(minIntervalSeconds, maxIntervalSeconds);
            float hi = Mathf.Max(minIntervalSeconds, maxIntervalSeconds);
            _nextBarkAllowedTime = Time.time + Random.Range(lo, hi);
        }

        private bool HasAnyConfiguredBarks()
        {
            return CountValidIn(combatBarkEvents) > 0 || CountValidIn(bossBarkEvents) > 0;
        }

        private static int CountValidIn(EventReference[] arr)
        {
            if (arr == null || arr.Length == 0) return 0;
            int n = 0;
            for (int i = 0; i < arr.Length; i++)
            {
                if (!arr[i].IsNull) n++;
            }

            return n;
        }

        /// <summary>Есть ли живой враг в радиусе; <paramref name="bossInRange"/> — среди них есть живой босс (QuestEnemy).</summary>
        private bool ScanNearbyEnemies(out bool bossInRange)
        {
            bossInRange = false;
            var cols = Physics.OverlapSphere(transform.position, detectionRadius, enemyDetectionLayers,
                QueryTriggerInteraction.Collide);
            if (cols == null || cols.Length == 0) return false;

            bool anyAlive = false;
            foreach (var col in cols)
            {
                if (col == null) continue;
                var enemy = col.GetComponentInParent<EnemyBase>();
                if (enemy == null) continue;

                var health = enemy.GetComponent<Health>() ?? enemy.GetComponentInParent<Health>();
                if (health != null)
                {
                    if (!health.IsAlive) continue;
                }

                anyAlive = true;
                var questEnemy = enemy.GetComponent<QuestEnemy>() ?? enemy.GetComponentInParent<QuestEnemy>();
                if (questEnemy != null && questEnemy.IsBoss)
                    bossInRange = true;
            }

            return anyAlive;
        }

        private void TryPlayRandomBark(bool bossInRange)
        {
            EventReference[] pool = bossInRange && CountValidIn(bossBarkEvents) > 0
                ? bossBarkEvents
                : combatBarkEvents;

            if (pool == null || pool.Length == 0) return;

            _validIndices.Clear();
            for (int i = 0; i < pool.Length; i++)
            {
                if (!pool[i].IsNull)
                    _validIndices.Add(i);
            }

            if (_validIndices.Count == 0) return;

            int pick = _validIndices[Random.Range(0, _validIndices.Count)];
            RuntimeManager.PlayOneShot(pool[pick], transform.position);
        }
    }
}
