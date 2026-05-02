using UnityEngine;

namespace SiberianGJ26.YouAreDoing.Antos.Abstraction
{
    public abstract class Singleton<T> : MonoBehaviour where T : Singleton<T>
    { 
        public static T Instance => _instance as T;

        private static Singleton<T> _instance;

        public virtual void Awake()
        {
            if (_instance)
                Destroy(_instance.gameObject);

            _instance = this;
        }
    }
}