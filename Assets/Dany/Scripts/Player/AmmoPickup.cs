using UnityEngine;

namespace Dany
{
    [RequireComponent(typeof(Collider))]
    public class AmmoPickup : MonoBehaviour
    {
        [Header("Ammo type (matches InventoryItem.weaponType)")]
        public InventoryItem.WeaponType weaponType = InventoryItem.WeaponType.Gan;
        public int amount = 10;

        [Header("UI")]
        public string displayName = "патроны";

        private void Reset()
        {
            var c = GetComponent<Collider>();
            c.isTrigger = true;
        }

        public string GetHintText()
        {
            string namePart = string.IsNullOrWhiteSpace(displayName) ? "патроны" : displayName;
            return $"Нажми E, чтобы подобрать {namePart}";
        }
    }
}

