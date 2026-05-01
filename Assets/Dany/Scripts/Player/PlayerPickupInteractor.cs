using UnityEngine;

namespace Dany
{
    public class PlayerPickupInteractor : MonoBehaviour
    {
        [SerializeField] private KeyCode pickupKey = KeyCode.E;

        [Header("Detection")]
        [SerializeField] private float radius = 1.5f;
        [SerializeField] private LayerMask pickupMask = ~0;

        private InventoryManager inventoryManager;

        private void Awake()
        {
            inventoryManager = GetComponent<InventoryManager>();
            if (!inventoryManager) inventoryManager = GetComponentInChildren<InventoryManager>();
        }

        private void Update()
        {
            if (!Input.GetKeyDown(pickupKey)) return;
            if (!inventoryManager) return;

            var hits = Physics.OverlapSphere(transform.position, radius, pickupMask, QueryTriggerInteraction.Collide);

            AmmoPickup best = null;
            float bestDist = float.MaxValue;

            foreach (var h in hits)
            {
                var p = h.GetComponentInParent<AmmoPickup>();
                if (!p) continue;

                float d = (p.transform.position - transform.position).sqrMagnitude;
                if (d < bestDist)
                {
                    bestDist = d;
                    best = p;
                }
            }

            if (best) inventoryManager.PickupAmmo(best);
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(1f, 0.85f, 0.1f, 0.25f);
            Gizmos.DrawSphere(transform.position, radius);
        }
    }
}

