using FMODUnity;
using UnityEngine;

namespace Dany
{
    /// <summary>Объект в мире: подбор по E (через <see cref="InventoryManager"/> или <see cref="PlayerPickupInteractor"/>).</summary>
    public class CollectiblePickup : MonoBehaviour
    {
        [SerializeField] private CollectibleDefinition definition;

        [Header("Audio (опционально)")]
        [SerializeField] private EventReference pickupFmodEvent;

        private bool _collected;

        public CollectibleDefinition Definition => definition;

        public string GetHintText()
        {
            string n = definition != null && !string.IsNullOrEmpty(definition.displayName)
                ? definition.displayName
                : "коллекционный предмет";
            return $"Нажми E, чтобы подобрать {n}";
        }

        /// <summary>Уведомляет квесты и уничтожает объект. Защита от двойного вызова в один кадр.</summary>
        public void Collect()
        {
            if (_collected) return;
            if (definition == null)
            {
                Debug.LogWarning($"{nameof(CollectiblePickup)}: не задан {nameof(definition)}.", this);
                return;
            }

            _collected = true;
            QuestEvents.RaiseCollectiblePickedUp(definition);
            if (!pickupFmodEvent.IsNull)
                RuntimeManager.PlayOneShot(pickupFmodEvent, transform.position);

            Destroy(gameObject);
        }
    }
}
