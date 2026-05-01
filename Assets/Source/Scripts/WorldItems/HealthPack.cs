using SiberianGJ26.YouAreDoing.Antos.Abstraction;
using SiberianGJ26.YouAreDoing.Antos.Singleton;
using SiberianGJ26.YouAreDoing.Antos.Readonly;
using UnityEngine;

namespace SiberianGJ26.YouAreDoing.Antos.Items
{
    public class HealthPack : MonoBehaviour, IMonoUpdate
    {
        [SerializeField] private HealthPackData data;

        //Singleton
        private MonoUpdater _monoUpdater;
        
        private void Start()
        {
            _monoUpdater = MonoUpdater.Instance;
            _monoUpdater?.Add(this);
        }

        private void OnEnable()
        {
            _monoUpdater?.Add(this);
        }

        private void OnDisable()
        {
            _monoUpdater?.Add(this);
        }

        public void OnUpdate()
        {
            if (Physics.SphereCast(transform.position, data.Range, transform.forward, out var hit, data.DetectLayer))
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