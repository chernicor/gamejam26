using System.Collections;
using UnityEngine;
namespace Dany
{
public class Decal : MonoBehaviour
{
    public float fadeDuration = 10f;
    private ParticleSystem particleSystem;

    void Start()
    {
        particleSystem = GetComponent<ParticleSystem>();
        if (particleSystem != null)
        {
            particleSystem.Play();
            StartCoroutine(WaitAndDestroy());
        }
        else
        {
            Debug.LogWarning("ParticleSystem �� ������ �� ������! �������, ��� ������ ����� ParticleSystem.");
            Destroy(gameObject);
        }
    }

    private IEnumerator WaitAndDestroy()
    {
        float particleLifetime = particleSystem.main.startLifetime.constant;
        float waitTime = Mathf.Min(fadeDuration, particleLifetime + 1f);

        yield return new WaitForSeconds(waitTime);

        particleSystem.Stop();
        Destroy(gameObject);
    }
}
}