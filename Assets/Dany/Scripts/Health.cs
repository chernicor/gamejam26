using UnityEngine;

namespace Dany.Legacy
{
    public class Health : MonoBehaviour
    {
        public float maxHealth = 100f;
        private float currentHealth;

        [SerializeField] private InventoryItem[] possibleItems;


        [SerializeField] private GameObject explosionPrefab;

         void Start()
        {
            currentHealth = maxHealth;
        }

        public void TakeDamage(float damage)
        {
            currentHealth -= damage;
            Debug.Log($"����: {damage}. ������� ��������: {currentHealth}");

            if (currentHealth <= 0)
            {
                Die();
            }
        }

        private void Die()
        {
            Debug.Log("������ ��������!");

            if (explosionPrefab != null)
            {
                GameObject explosionEffect = Instantiate(explosionPrefab, transform.position, transform.rotation);

                ParticleSystem ps = explosionEffect.GetComponent<ParticleSystem>();
                if (ps != null)
                {
                    Destroy(explosionEffect, ps.main.duration + ps.main.startDelay.constantMax);
                }
                else
                {
                    Destroy(explosionEffect, 2f);
                }

                Debug.Log("������ ������ �������!");
            }
            else
            {
                Debug.LogWarning("������ ������ �� �������� � Health!");
            }

            if (possibleItems != null && possibleItems.Length > 0)
            {
                int randomIndex = Random.Range(0, possibleItems.Length);
                InventoryItem selectedItem = possibleItems[randomIndex];

                if (selectedItem.worldPickupPrefab != null)
                {
                    Instantiate(selectedItem.worldPickupPrefab, transform.position + Vector3.up * 0.5f,
                        Quaternion.identity);
                    Debug.Log($"������� item: {selectedItem.itemName}");
                }
                else
                {
                    Debug.LogWarning($"� item '{selectedItem.itemName}' �� ����� worldPickupPrefab!");
                }
            }

            Destroy(gameObject);
        }
    }
}