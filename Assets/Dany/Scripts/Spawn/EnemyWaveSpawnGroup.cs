using System;
using SiberianGJ26.YouAreDoing.Antos.Singleton;
using UnityEngine;

namespace Dany
{
    /// <summary>
    /// Волна врагов по слотам: триггер спавнит только тех, кого ещё не убили навсегда.
    /// При смерти игрока живые враги этой группы исчезают; после респавна при повторном входе в триггер
    /// появляются только выжившие слоты (убитые в бою не возвращаются).
    /// </summary>
    [DisallowMultipleComponent]
    public class EnemyWaveSpawnGroup : MonoBehaviour
    {
        [Serializable]
        public class Entry
        {
            [Tooltip("Префаб врага. Смерть через Health или вызов EnemyWaveSpawnSlotTracker.MarkPermanentRemovalWithoutHealth() до Destroy помечает слот навсегда; иначе после смерти игрока враг может появиться снова.")]
            public GameObject prefab;

            [Tooltip("Если пусто — позиция и поворот этого объекта (EnemyWaveSpawnGroup).")]
            public Transform spawnPoint;
        }

        [SerializeField] private Entry[] entries = Array.Empty<Entry>();
        [SerializeField] private string playerTag = "Player";
        [SerializeField] private bool spawnWhenPlayerEntersTrigger = true;
        [SerializeField] private bool despawnLivingEnemiesOnPlayerDeath = true;

        private GameObject[] _spawned;
        private bool[] _permanentlyDead;
        private bool _subscribedPlayerDeath;

        private void Awake()
        {
            int n = entries != null ? entries.Length : 0;
            _spawned = new GameObject[n];
            _permanentlyDead = new bool[n];
        }

        private void Start()
        {
            if (!despawnLivingEnemiesOnPlayerDeath) return;
            if (PlayerSpawnState.Instance == null) return;
            PlayerSpawnState.Instance.OnDestroyEv += OnPlayerDied;
            _subscribedPlayerDeath = true;
        }

        private void OnDestroy()
        {
            if (!_subscribedPlayerDeath) return;
            _subscribedPlayerDeath = false;
            if (PlayerSpawnState.Instance != null)
                PlayerSpawnState.Instance.OnDestroyEv -= OnPlayerDied;
        }

        private void OnValidate()
        {
            var c = GetComponent<Collider>();
            if (c != null && spawnWhenPlayerEntersTrigger && !c.isTrigger)
                c.isTrigger = true;
        }

        /// <summary>Явный вызов спавна (кнопка, скрипт, другой триггер).</summary>
        public void SpawnWave()
        {
            if (entries == null) return;
            for (int i = 0; i < entries.Length; i++)
            {
                if (_permanentlyDead[i]) continue;
                if (_spawned[i] != null) continue;
                SpawnSlot(i);
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!spawnWhenPlayerEntersTrigger) return;
            if (string.IsNullOrEmpty(playerTag) || !other.CompareTag(playerTag)) return;
            SpawnWave();
        }

        private void OnPlayerDied()
        {
            if (entries == null || _spawned == null) return;
            for (int i = 0; i < _spawned.Length; i++)
            {
                if (_permanentlyDead[i]) continue;
                var go = _spawned[i];
                if (go == null) continue;
                var h = go.GetComponent<Health>() ?? go.GetComponentInChildren<Health>();
                if (h != null && !h.IsAlive) continue;
                Destroy(go);
            }
        }

        private void SpawnSlot(int index)
        {
            var e = entries[index];
            if (e.prefab == null) return;
            Transform t = e.spawnPoint != null ? e.spawnPoint : transform;
            var go = Instantiate(e.prefab, t.position, t.rotation);
            _spawned[index] = go;
            var tracker = go.AddComponent<EnemyWaveSpawnSlotTracker>();
            tracker.Init(this, index);
        }

        internal void NotifySlotReleased(int index, bool permanentRemoval)
        {
            if (_spawned == null || index < 0 || index >= _spawned.Length) return;
            _spawned[index] = null;
            if (permanentRemoval)
                _permanentlyDead[index] = true;
        }
    }

    /// <summary>
    /// Вешается на инстанс волны автоматически. Отличает смерть через <see cref="Health"/>,
    /// суицид/скрипт без HP и принудительный Destroy при смерти игрока.
    /// </summary>
    public sealed class EnemyWaveSpawnSlotTracker : MonoBehaviour
    {
        private EnemyWaveSpawnGroup _group;
        private int _index;
        private bool _permanentRemoval;
        private Health _health;

        public void Init(EnemyWaveSpawnGroup group, int index)
        {
            _group = group;
            _index = index;
        }

        /// <summary>
        /// Если враг уничтожается не через <see cref="Health.Die"/> (суицид, таймер, кат-сцена) —
        /// вызови один раз перед <c>Destroy</c>, чтобы слот не респавнился.
        /// </summary>
        public void MarkPermanentRemovalWithoutHealth()
        {
            _permanentRemoval = true;
        }

        private void Start()
        {
            _health = GetComponent<Health>() ?? GetComponentInChildren<Health>();
            if (_health != null)
                _health.OnDeadEv += OnHealthDeath;
        }

        private void OnDestroy()
        {
            if (_health != null)
                _health.OnDeadEv -= OnHealthDeath;
            _group?.NotifySlotReleased(_index, _permanentRemoval);
        }

        private void OnHealthDeath() => _permanentRemoval = true;
    }
}
