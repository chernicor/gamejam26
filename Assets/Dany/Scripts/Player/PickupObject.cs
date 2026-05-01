using UnityEngine;
namespace Dany
{
public class PickupObject : MonoBehaviour
{
    public InventoryItem item;

    public float pickupRange = 3f;

    private InventoryManager inventoryManager;

    void Start()
    {
        GameObject player = GameObject.FindWithTag("Player");
        if (player != null)
        {
            inventoryManager = player.GetComponent<InventoryManager>();
        }
    }

    public void Pickup()
    {
        if (inventoryManager != null && inventoryManager.HasFreeSlot())
        {
            Rigidbody rb = GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.isKinematic = true;
                rb.velocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }

            inventoryManager.PickupItem(item);
            Destroy(gameObject);
        }
    }
}
}