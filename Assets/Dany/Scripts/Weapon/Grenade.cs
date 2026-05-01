using UnityEngine;

namespace Dany
{
public class Grenade : MonoBehaviour
{
    public float explosionDelay = 3f;
    private float timer = 0f;
    private bool timerStarted = false;
    private bool hasExploded = false;

    public InventoryItem item; 

    public void StartTimer()
    {
        if (!timerStarted)
        {
            timerStarted = true;
            Debug.Log("������ ������� �������!");
        }
    }

    void Update()
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

    private void Explode()
    {
        Debug.Log("Boom! ������� ����������.");

        if (item != null && item.decalPrefab != null)
        {
            Instantiate(item.decalPrefab, transform.position, Quaternion.identity);
        }

        if (item != null)
        {
            Collider[] hitColliders = Physics.OverlapSphere(transform.position, item.explosionRadius);
            foreach (Collider hit in hitColliders)
            {
                Health health = hit.GetComponent<Health>();
                if (health != null)
                {
                    health.TakeDamage(item.explosionDamage);
                }
            }
        }

        Destroy(gameObject);
    }
}
}