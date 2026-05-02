using SiberianGJ26.YouAreDoing.Antos.Abstraction;
using SiberianGJ26.YouAreDoing.Antos.Singleton;
using UnityEngine;

namespace Dany
{
    public class Grenade : MonoBehaviour, IMonoUpdate
    {
        public float explosionDelay = 3f;
        private float timer = 0f;
        private bool timerStarted = false;
        private bool hasExploded = false;

        public InventoryItem item;

        //Singleton
        private MonoUpdater _monoUpdater;

        private void Start()
        {
            _monoUpdater = MonoUpdater.Instance;
            _monoUpdater.Add(this);
        }

        public void OnUpdate()
        {
            if (timerStarted && !hasExploded)
            {
                timer += Time.deltaTime;
                if (timer >= explosionDelay)
                {
                    Explode();
                    hasExploded = true;
                }
            }
        }

        public void StartTimer()
        {
            if (!timerStarted)
            {
                timerStarted = true;
                Debug.Log("������ ������� �������!");
            }
        }

        private void Explode()
        {
            Debug.Log("Boom! ������� ����������.");

            if (item != null && item.decalPrefab != null)
            {
                Instantiate(item.decalPrefab, transform.position, Quaternion.identity);
            }

            if (item != null)
            {
                var hitColliders = Physics.OverlapSphere(transform.position, item.explosionRadius);
                foreach (var hit in hitColliders)
                {
                    var health = hit.GetComponentInParent<IHealth>();
                    if (health != null && health.IsAlive)
                        health.TrySet(-item.explosionDamage);
                }
            }

            _monoUpdater.Remove(this);
            Destroy(gameObject);
        }
    }
}