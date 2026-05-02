using SiberianGJ26.YouAreDoing.Antos.Abstraction;
using SiberianGJ26.YouAreDoing.Antos.Singleton;
using SiberianGJ26.YouAreDoing.Antos.Readonly;
using UnityEngine;
using DG.Tweening;

namespace SiberianGJ26.YouAreDoing.Antos.Items
{
    public class HealthPack : MonoBehaviour, IMonoUpdate
    {
        [SerializeField] private HealthPackData data;
        [SerializeField] private float axisY;
        [SerializeField] private float duration;

        private Sequence _sequence;

        //Singleton
        private MonoUpdater _monoUpdater;

        private void Start()
        {
            _monoUpdater = MonoUpdater.Instance;
            _monoUpdater?.Add(this);

            _sequence = DOTween.Sequence();
            _sequence.Append(transform.DOMoveY(axisY, duration));
            _sequence.SetLoops(-1, LoopType.Yoyo);
            _sequence.Play();
        }

        private void OnDestroy()
        {
            _monoUpdater?.Remove(this);
            _sequence?.Kill();
        }

        private void OnDrawGizmos()
        {
            if (data == null) return;
            Gizmos.color = data.GizmosColor;
            Gizmos.DrawSphere(transform.position, data.Radius);
        }

        public void OnUpdate()
        {
            if (Physics.SphereCast(transform.position, data.Radius, transform.forward, out var hit, data.DetectLayer))
            {
                if (hit.collider.TryGetComponent(out IHealth health) && health.TrySet(data.Value))
                {
                    if (data.Effect != null)
                    {
                        var effect = Instantiate(data.Effect.Prefab);
                        effect.Init(hit.collider.transform);
                    }
                    Destroy(gameObject);
                }
            }
        }
    }
}