using SiberianGJ26.YouAreDoing.Antos.Abstraction;
using SiberianGJ26.YouAreDoing.Antos.Readonly;
using Dany;
using UnityEngine;
using DG.Tweening;

namespace SiberianGJ26.YouAreDoing.Antos.Items
{
    public class HealthPack : MonoBehaviour
    {
        [SerializeField] private HealthPackData data;
        [SerializeField] private float axisY;
        [SerializeField] private float duration;

        private Sequence _sequence;
        private bool _pickedUp;

        private void Start()
        {
            _sequence = DOTween.Sequence();
            _sequence.Append(transform.DOMoveY(axisY, duration));
            _sequence.SetLoops(-1, LoopType.Yoyo);
            _sequence.Play();
        }

        private void OnDestroy()
        {
            _sequence?.Kill();
        }

        private void OnDrawGizmos()
        {
            if (data == null) return;
            Gizmos.color = data.GizmosColor;
            Gizmos.DrawSphere(transform.position, data.Radius);
        }

        private void OnTriggerEnter(Collider other)
        {
            if (_pickedUp || data == null) return;

            var player = other.GetComponentInParent<FirstPersonController>();
            if (player == null || player.Health == null) return;

            IHealth health = player.Health;
            if (!health.IsAlive) return;
            if (health.Curent >= health.Max) return;

            if (!health.TrySet(data.Value)) return;

            _pickedUp = true;

            if (data.Effect != null)
            {
                var effect = Instantiate(data.Effect.Prefab);
                effect.Init(other.transform);
            }

            Destroy(gameObject);
        }
    }
}
