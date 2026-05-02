using FMODUnity;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Dany
{
    /// <summary>
    /// Триггер выхода: при входе игрока — если все задачи <see cref="LevelQuestController"/> выполнены,
    /// загружается следующая сцена; иначе one-shot FMOD (фраза «сначала выполни задания» и т.п.).
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class QuestLevelExitTrigger : MonoBehaviour
    {
        [SerializeField] private LevelQuestController questController;

        [Tooltip("Тег объекта игрока (коллайдер CharacterController / Rigidbody).")]
        [SerializeField] private string playerTag = "Player";

        [Header("FMOD")]
        [Tooltip("Проигрывается в позиции триггера, если задания ещё не выполнены.")]
        [SerializeField] private EventReference incompleteQuestVoiceLine;

        [Header("Следующий уровень")]
        [Tooltip("Имя сцены как в Build Settings (File → Build Settings).")]
        [SerializeField] private string nextSceneName;

        [SerializeField] private LoadSceneMode loadMode = LoadSceneMode.Single;

        private void Reset()
        {
            var col = GetComponent<Collider>();
            if (col != null)
                col.isTrigger = true;
        }

        private void Awake()
        {
            if (questController == null)
                questController = FindFirstObjectByType<LevelQuestController>();
        }

        private void OnTriggerEnter(Collider other)
        {
            if (playerTag.Length > 0 && !other.CompareTag(playerTag))
                return;

            if (questController == null)
            {
                Debug.LogWarning($"{nameof(QuestLevelExitTrigger)}: нет {nameof(LevelQuestController)}.", this);
                return;
            }

            if (questController.AllComplete)
            {
                if (string.IsNullOrEmpty(nextSceneName))
                {
                    Debug.LogWarning($"{nameof(QuestLevelExitTrigger)}: не задано имя следующей сцены.", this);
                    return;
                }

                SceneManager.LoadScene(nextSceneName, loadMode);
                return;
            }

            if (!incompleteQuestVoiceLine.IsNull)
                RuntimeManager.PlayOneShot(incompleteQuestVoiceLine, transform.position);
        }
    }
}
