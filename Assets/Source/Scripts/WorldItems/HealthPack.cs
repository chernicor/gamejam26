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
        }

        public void OnUpdate()
        {
            /*if (Physics.SphereCast(transform.position, data.Range, transform.forward, out var hit, data.DetectLayer))
            {
                hit.collider.TryGetComponent(out )
            }*/
        }
    }
}