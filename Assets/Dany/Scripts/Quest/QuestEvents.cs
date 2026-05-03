using System;
using UnityEngine;

namespace Dany
{
    /// <summary>События для связи инвентаря и квестов без жёстких ссылок.</summary>
    public static class QuestEvents
    {
        public static event Action<InventoryItem> ItemPickedUp;
        public static event Action<bool> TrackedEnemyDied;
        public static event Action<CollectibleDefinition> CollectiblePickedUp;

        public static void RaiseItemPickedUp(InventoryItem item)
        {
            if (item != null)
                ItemPickedUp?.Invoke(item);
        }

        /// <param name="isBoss">true — погиб объект с меткой босса.</param>
        public static void RaiseTrackedEnemyDied(bool isBoss)
        {
            TrackedEnemyDied?.Invoke(isBoss);
        }

        public static void RaiseCollectiblePickedUp(CollectibleDefinition definition)
        {
            if (definition != null)
                CollectiblePickedUp?.Invoke(definition);
        }
    }
}
