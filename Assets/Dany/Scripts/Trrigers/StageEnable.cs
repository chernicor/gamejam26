using UnityEngine;
using UnityEngine.Serialization;

namespace Dany
{
    /// <summary>
    /// При входе игрока в триггер активирует указанный объект сцены (этап).
    /// </summary>
    public class StageEnable : MonoBehaviour
    {
        [FormerlySerializedAs("Stage")]
        [SerializeField] private GameObject stage;

        private void OnTriggerEnter(Collider other)
        {
            if (!other.CompareTag("Player"))
                return;
            if (stage != null)
                stage.SetActive(true);
        }
    }
}
