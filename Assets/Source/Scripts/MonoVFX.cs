using SiberianGJ26.YouAreDoing.Antos.Readonly;
using System.Collections;
using UnityEngine;

namespace SiberianGJ26.YouAreDoing.Antos.Vfx
{
    public class MonoVFX : MonoBehaviour
    {
        [SerializeField] private VfxData data;

        private WaitForSeconds _wait;
        private Coroutine _coroutine;
        
        public void Init(Transform target)
        {
            transform.position = target.position;
            _wait = new(data.Duration);
            if (_coroutine == null)
                _coroutine = StartCoroutine(Waiting());
        }

        private IEnumerator Waiting()
        {
            yield return _wait;
            Destroy(gameObject);
        }
    }
}