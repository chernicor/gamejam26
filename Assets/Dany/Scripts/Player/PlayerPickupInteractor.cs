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

            AmmoPickup best = null;
            float bestDist = float.MaxValue;

            foreach (var h in hits)
            {
                var p = h.GetComponentInParent<AmmoPickup>();
                if (!p) continue;

                float d = (p.transform.position - _owner.position).sqrMagnitude;
                if (d < bestDist)
                {
                    bestDist = d;
                    best = p;
                }
            }

            if (best) inventoryManager.PickupAmmo(best);
        }
    }
}