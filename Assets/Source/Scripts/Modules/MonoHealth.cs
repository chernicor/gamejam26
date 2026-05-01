using SiberianGJ26.YouAreDoing.Antos.Abstraction;
using UnityEngine;
using System;

namespace SiberianGJ26.YouAreDoing.Antos.Modules
{
    public class MonoHealth : MonoBehaviour, IHealth
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