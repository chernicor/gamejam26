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
}