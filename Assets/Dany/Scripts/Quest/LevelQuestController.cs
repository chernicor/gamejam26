using System;
using System.Text;
using UnityEngine;

namespace Dany
{
    public enum QuestObjectiveKind
    {
        CollectItem,
        ClearEnemies,
        DefeatBoss,
        /// <summary>Собрать N коллекционок одного типа (<see cref="CollectibleDefinition"/>).</summary>
        CollectCollectibles
    }

    [Serializable]
    public class QuestObjective
    {
        [Tooltip("Тип этапа.")]
        public QuestObjectiveKind kind;

        [Tooltip("Для CollectItem — какой ScriptableObject должен подобрать игрок.")]
        public InventoryItem itemToCollect;

        [Tooltip("Для CollectCollectibles — какой тип коллекционки считать (CollectiblePickup + CollectibleDefinition).")]
        public CollectibleDefinition collectibleDefinition;

        [Tooltip("Для CollectCollectibles — сколько экземпляров нужно подобрать.")]
        public int collectiblesRequired = 1;

        [Tooltip("Для ClearEnemies — сколько врагов нужно убить (см. QuestEnemy на префабах).")]
        public int enemiesRequired = 1;

        [TextArea(2, 4)]
        [Tooltip("Если заполнено — показывается вместо автотекста.")]
        public string titleOverride;
    }

    /// <summary>Параллельные задачи уровня: все цели активны сразу, порядок выполнения любой.</summary>
    public class LevelQuestController : MonoBehaviour
    {
        /// <summary>Rich Text TMP: цвет текста задач (активные, выполненные, итог).</summary>
        private const string QuestTaskColorHex = "1DFF00";

        [SerializeField] private QuestObjective[] objectives = Array.Empty<QuestObjective>();

        [Header("UI (опционально)")]
        [SerializeField] private QuestTaskPanelUI panel;

        private bool[] _done;
        private int[] _killProgress;
        private int[] _collectProgress;
        private bool _allComplete;

        /// <summary>Сколько целей уже выполнено (для отладки/UI).</summary>
        public int CompletedObjectivesCount
        {
            get
            {
                if (_done == null || objectives == null) return 0;
                int c = 0;
                for (int i = 0; i < Mathf.Min(_done.Length, objectives.Length); i++)
                {
                    if (_done[i]) c++;
                }

                return c;
            }
        }

        public int TotalStages => objectives != null ? objectives.Length : 0;
        public bool AllComplete => _allComplete;

        public event Action OnDisplayChanged;

        private void OnEnable()
        {
            QuestEvents.ItemPickedUp += OnItemPickedUp;
            QuestEvents.TrackedEnemyDied += OnTrackedEnemyDied;
            QuestEvents.CollectiblePickedUp += OnCollectiblePickedUp;
        }

        private void OnDisable()
        {
            QuestEvents.ItemPickedUp -= OnItemPickedUp;
            QuestEvents.TrackedEnemyDied -= OnTrackedEnemyDied;
            QuestEvents.CollectiblePickedUp -= OnCollectiblePickedUp;
        }

        private void Start()
        {
            EnsureStateArrays();
            RecomputeAllComplete();

            if (panel != null)
                panel.Bind(this);

            Notify();
        }

        private void EnsureStateArrays()
        {
            int n = objectives != null ? objectives.Length : 0;
            if (n == 0)
            {
                _done = Array.Empty<bool>();
                _killProgress = Array.Empty<int>();
                _collectProgress = Array.Empty<int>();
                return;
            }

            if (_done != null && _done.Length == n)
                return;

            _done = new bool[n];
            _killProgress = new int[n];
            _collectProgress = new int[n];
        }

        private void OnItemPickedUp(InventoryItem item)
        {
            if (_allComplete || objectives == null || item == null) return;

            EnsureStateArrays();

            bool touched = false;
            for (int i = 0; i < objectives.Length; i++)
            {
                if (_done[i]) continue;
                var o = objectives[i];
                if (o.kind != QuestObjectiveKind.CollectItem) continue;
                if (o.itemToCollect == null || item != o.itemToCollect) continue;

                _done[i] = true;
                touched = true;
            }

            if (touched)
            {
                RecomputeAllComplete();
                Notify();
            }
        }

        private void OnCollectiblePickedUp(CollectibleDefinition def)
        {
            if (_allComplete || objectives == null || def == null) return;

            EnsureStateArrays();

            bool touched = false;
            for (int i = 0; i < objectives.Length; i++)
            {
                if (_done[i]) continue;
                var o = objectives[i];
                if (o.kind != QuestObjectiveKind.CollectCollectibles) continue;
                if (o.collectibleDefinition == null || def != o.collectibleDefinition) continue;

                int req = Mathf.Max(1, o.collectiblesRequired);
                _collectProgress[i]++;
                if (_collectProgress[i] >= req)
                    _done[i] = true;

                touched = true;
            }

            if (touched)
            {
                RecomputeAllComplete();
                Notify();
            }
        }

        private void OnTrackedEnemyDied(bool isBoss)
        {
            if (_allComplete || objectives == null) return;

            EnsureStateArrays();
            bool changed = false;

            for (int i = 0; i < objectives.Length; i++)
            {
                if (_done[i]) continue;
                var o = objectives[i];

                if (o.kind == QuestObjectiveKind.DefeatBoss && isBoss)
                {
                    _done[i] = true;
                    changed = true;
                    continue;
                }

                if (o.kind == QuestObjectiveKind.ClearEnemies && !isBoss)
                {
                    int req = Mathf.Max(1, o.enemiesRequired);
                    _killProgress[i]++;
                    if (_killProgress[i] >= req)
                    {
                        _done[i] = true;
                        changed = true;
                    }
                    else
                        changed = true;
                }
            }

            if (changed)
            {
                RecomputeAllComplete();
                Notify();
            }
        }

        private void RecomputeAllComplete()
        {
            if (objectives == null || objectives.Length == 0)
            {
                _allComplete = true;
                return;
            }

            for (int i = 0; i < objectives.Length; i++)
            {
                if (!_done[i])
                {
                    _allComplete = false;
                    return;
                }
            }

            _allComplete = true;
        }

        private void Notify()
        {
            OnDisplayChanged?.Invoke();
        }

        /// <summary>Текст для панели: все цели и прогресс.</summary>
        public string BuildPanelText()
        {
            if (objectives == null || objectives.Length == 0)
                return "";

            EnsureStateArrays();

            var sb = new StringBuilder();

            for (int i = 0; i < objectives.Length; i++)
            {
                bool done = _done[i];
                string line = FormatObjectiveLine(i, objectives[i], done);
                if (string.IsNullOrEmpty(line)) continue;

                if (done)
                    sb.AppendLine($"<color=#{QuestTaskColorHex}>✓ {line}</color>");
                else
                    sb.AppendLine($"<color=#{QuestTaskColorHex}><b>► {line}</b></color>");
            }

            if (_allComplete)
                sb.AppendLine($"<color=#{QuestTaskColorHex}>Все задачи выполнены!</color>");

            return sb.ToString().TrimEnd();
        }

        private string FormatObjectiveLine(int index, QuestObjective o, bool done)
        {
            int req = Mathf.Max(1, o.enemiesRequired);
            int killsShown = done ? req : (_killProgress != null && index < _killProgress.Length ? _killProgress[index] : 0);

            int collReq = Mathf.Max(1, o.collectiblesRequired);
            int collShown = done ? collReq : (_collectProgress != null && index < _collectProgress.Length ? _collectProgress[index] : 0);

            if (!string.IsNullOrWhiteSpace(o.titleOverride))
            {
                if (o.kind == QuestObjectiveKind.ClearEnemies)
                    return $"{o.titleOverride} ({killsShown}/{req})";
                if (o.kind == QuestObjectiveKind.CollectCollectibles)
                    return $"{o.titleOverride} ({collShown}/{collReq})";
                return o.titleOverride;
            }

            switch (o.kind)
            {
                case QuestObjectiveKind.CollectItem:
                {
                    string name = o.itemToCollect != null ? o.itemToCollect.itemName : "?";
                    return $"Подбери предмет: {name}";
                }
                case QuestObjectiveKind.CollectCollectibles:
                {
                    string name = o.collectibleDefinition != null ? o.collectibleDefinition.displayName : "?";
                    return $"Собери коллекционки: {name} ({collShown}/{collReq})";
                }
                case QuestObjectiveKind.ClearEnemies:
                    return $"Зачисти уровень: врагов {killsShown}/{req}";
                case QuestObjectiveKind.DefeatBoss:
                    return "Победи босса";
                default:
                    return "";
            }
        }
    }
}
