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

    /// <summary>Последовательность задач уровня и прогресс.</summary>
    public class LevelQuestController : MonoBehaviour
    {
        [SerializeField] private QuestObjective[] objectives = Array.Empty<QuestObjective>();

        [Header("UI (опционально)")]
        [SerializeField] private QuestTaskPanelUI panel;

        private int _currentIndex;
        private int _killsInStage;
        private int _collectiblesInStage;
        private bool _allComplete;

        public int CurrentStageIndex => _currentIndex;
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
            if (panel != null)
                panel.Bind(this);

            Notify();
        }

        private void OnItemPickedUp(InventoryItem item)
        {
            if (_allComplete || objectives == null || _currentIndex >= objectives.Length) return;

            var o = objectives[_currentIndex];
            if (o.kind != QuestObjectiveKind.CollectItem) return;
            if (o.itemToCollect == null || item != o.itemToCollect) return;

            Advance();
        }

        private void OnCollectiblePickedUp(CollectibleDefinition def)
        {
            if (_allComplete || objectives == null || _currentIndex >= objectives.Length) return;

            var o = objectives[_currentIndex];
            if (o.kind != QuestObjectiveKind.CollectCollectibles) return;
            if (o.collectibleDefinition == null || def != o.collectibleDefinition) return;

            _collectiblesInStage++;
            if (_collectiblesInStage >= Mathf.Max(1, o.collectiblesRequired))
                Advance();
            else
                Notify();
        }

        private void OnTrackedEnemyDied(bool isBoss)
        {
            if (_allComplete || objectives == null || _currentIndex >= objectives.Length) return;

            var o = objectives[_currentIndex];

            if (o.kind == QuestObjectiveKind.DefeatBoss && isBoss)
            {
                Advance();
                return;
            }

            if (o.kind == QuestObjectiveKind.ClearEnemies && !isBoss)
            {
                _killsInStage++;
                if (_killsInStage >= Mathf.Max(1, o.enemiesRequired))
                    Advance();
                else
                    Notify();
            }
        }

        private void Advance()
        {
            _killsInStage = 0;
            _collectiblesInStage = 0;
            _currentIndex++;

            if (_currentIndex >= objectives.Length)
                _allComplete = true;

            Notify();
        }

        private void Notify()
        {
            OnDisplayChanged?.Invoke();
        }

        /// <summary>Текст для панели: текущий этап и прогресс.</summary>
        public string BuildPanelText()
        {
            if (objectives == null || objectives.Length == 0)
                return "";

            var sb = new StringBuilder();

            for (int i = 0; i < objectives.Length; i++)
            {
                bool done = i < _currentIndex;
                bool isCurrent = i == _currentIndex && !_allComplete;

                string line = FormatObjectiveLine(objectives[i], done);
                if (string.IsNullOrEmpty(line)) continue;

                if (done)
                    sb.AppendLine($"<color=#888888>✓ {line}</color>");
                else if (isCurrent)
                    sb.AppendLine($"<color=#FFFFFF><b>► {line}</b></color>");
                else
                    sb.AppendLine($"<color=#666666>○ {line}</color>");
            }

            if (_allComplete)
                sb.AppendLine("<color=#88FF88>Все задачи выполнены!</color>");

            return sb.ToString().TrimEnd();
        }

        private string FormatObjectiveLine(QuestObjective o, bool done)
        {
            int req = Mathf.Max(1, o.enemiesRequired);
            int killsShown = o.kind == QuestObjectiveKind.ClearEnemies && done ? req : _killsInStage;

            int collReq = Mathf.Max(1, o.collectiblesRequired);
            int collShown = o.kind == QuestObjectiveKind.CollectCollectibles && done ? collReq : _collectiblesInStage;

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
