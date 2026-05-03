using SiberianGJ26.YouAreDoing.Antos.Abstraction;
using UnityEngine;
using System;

namespace Dany
{
    [Serializable]
    public class PlayerPickupInteractor : IMonoUpdate
    {
        [SerializeField] private InventoryManager inventoryManager;
        [SerializeField] private KeyCode pickupKey = KeyCode.E;

        [Header("Detection")]
        [SerializeField] private float radius = 1.5f;
        [SerializeField] private LayerMask pickupMask = ~0;

        private Transform _owner;

        public void Init(Transform owner, InventoryManager manager)
        {
            _owner = owner;
            inventoryManager = manager;
        }

        public void OnDrawGizmosSelected()
        {
            if (_owner == null) return;
            Gizmos.color = new Color(1f, 0.85f, 0.1f, 0.25f);
            Gizmos.DrawSphere(_owner.position, radius);
        }

        public void OnUpdate()
        {
            if (!Input.GetKeyDown(pickupKey)) return;
            if (!inventoryManager) return;

            var hits = Physics.OverlapSphere(_owner.position, radius, pickupMask, QueryTriggerInteraction.Collide);

            AmmoPickup bestAmmo = null;
            float bestAmmoDist = float.MaxValue;
            CollectiblePickup bestCollectible = null;
            float bestCollectibleDist = float.MaxValue;

            foreach (var h in hits)
            {
                var c = h.GetComponentInParent<CollectiblePickup>();
                if (c != null && c.Definition != null)
                {
                    float d = (c.transform.position - _owner.position).sqrMagnitude;
                    if (d < bestCollectibleDist)
                    {
                        bestCollectibleDist = d;
                        bestCollectible = c;
                    }
                }

                var p = h.GetComponentInParent<AmmoPickup>();
                if (p != null)
                {
                    float d = (p.transform.position - _owner.position).sqrMagnitude;
                    if (d < bestAmmoDist)
                    {
                        bestAmmoDist = d;
                        bestAmmo = p;
                    }
                }
            }

            if (bestCollectible != null && bestAmmo != null)
            {
                if (bestCollectibleDist <= bestAmmoDist)
                    inventoryManager.PickupCollectible(bestCollectible);
                else
                    inventoryManager.PickupAmmo(bestAmmo);
            }
            else if (bestCollectible != null)
            {
                inventoryManager.PickupCollectible(bestCollectible);
            }
            else if (bestAmmo != null)
            {
                inventoryManager.PickupAmmo(bestAmmo);
            }
        }
    }
}