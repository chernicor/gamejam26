using UnityEngine;
using System;

namespace SiberianGJ26.YouAreDoing.Antos.Abstraction
{
    public interface IHealth
    {
        public event Action<float> OnDamageEv;
        public event Action<float> OnHealEv;
        public event Action OnDeadEv;

        public float Curent { get; }
        public float Max { get; }
        public bool IsAlive { get; }

        public bool TrySet(float value);
    }

    [Serializable]
    public class Health : IHealth
    {
        public event Action<float> OnDamageEv;
        public event Action<float> OnHealEv;
        public event Action OnDeadEv;

        [SerializeField] private float curent;
        [SerializeField] private float max;

        public float Curent => curent;
        public float Max => max;

        public bool IsAlive => curent > 0f;

        public bool TrySet(float value)
        {
            if (!IsAlive) return false;

            curent += value;
            curent = Mathf.Clamp(curent, 0f, max);

            if (value < 0f)
                OnDamageEv?.Invoke(value);
            else
                OnHealEv?.Invoke(value);

            if (curent <= 0f)
                OnDeadEv?.Invoke();
            
            return true;
        }
    }
}