using System;
using SiberianGJ26.YouAreDoing.Antos.Abstraction;
using UnityEngine;

namespace Dany
{
    public class Health : MonoBehaviour, IHealth
    {
        public event Action<float> OnDamageEv;
        public event Action<float> OnHealEv;
        public event Action OnDeadEv;

        public float maxHealth = 100f;
        private float currentHealth;

        [SerializeField] private InventoryItem[] possibleItems;

        [SerializeField] private GameObject explosionPrefab;

        public float Curent => currentHealth;
        public float Max => maxHealth;
        public float ArmorCurent => 0f;
        public float ArmorMax => 0f;
        public bool IsAlive => currentHealth > 0f;

        private void Start()
        {
            currentHealth = maxHealth;
        }

        public bool TrySet(float value)
        {
            if (value >= 0f)
            {
                if (!IsAlive) return false;
                currentHealth += value;
                currentHealth = Mathf.Clamp(currentHealth, 0f, maxHealth);
                OnHealEv?.Invoke(value);
                return true;
            }

            if (!IsAlive) return false;
            ApplyDamage(-value);
            return true;
        }

        public void TakeDamage(float damage)
        {
            if (damage <= 0f || !IsAlive) return;
            ApplyDamage(damage);
        }

        private void ApplyDamage(float damage)
        {
            currentHealth -= damage;
            OnDamageEv?.Invoke(-damage);
            Debug.Log($"Урон: {damage}. Осталось здоровья: {currentHealth}");

            if (currentHealth <= 0f)
                Die();
        }

        private void Die()
        {
            OnDeadEv?.Invoke();
            Debug.Log("Объект уничтожен!");

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

                Debug.Log("Эффект взрыва создан!");
            }
            else
            {
                Debug.LogWarning("Префаб взрыва не назначен в Health!");
            }

            if (possibleItems != null && possibleItems.Length > 0)
            {
                int randomIndex = UnityEngine.Random.Range(0, possibleItems.Length);
                InventoryItem selectedItem = possibleItems[randomIndex];

                if (selectedItem.worldPickupPrefab != null)
                {
                    Instantiate(selectedItem.worldPickupPrefab, transform.position + Vector3.up * 0.5f, Quaternion.identity);
                    Debug.Log($"Дроп item: {selectedItem.itemName}");
                }
                else
                {
                    Debug.LogWarning($"У item '{selectedItem.itemName}' нет worldPickupPrefab!");
                }
            }

            Destroy(gameObject);
        }
    }
}
