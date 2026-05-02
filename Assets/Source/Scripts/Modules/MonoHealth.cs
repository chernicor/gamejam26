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
        /// <summary>Срабатывает при любом изменении HP или брони (для UI).</summary>
        public event Action OnStatsChanged;

        [SerializeField] private float curent;
        [SerializeField] private float max;

        [Header("Armor (damage hits armor first)")]
        [SerializeField] private float armorCurent;
        [SerializeField] private float armorMax;

        [Tooltip("При старте сцены выставить броню равной максимуму.")]
        [SerializeField] private bool fillArmorOnStart = true;

        public float Curent => curent;
        public float Max => max;
        public float ArmorCurent => armorCurent;
        public float ArmorMax => armorMax;

        public bool IsAlive => curent > 0f;

        private void Start()
        {
            if (fillArmorOnStart && armorMax > 0f)
                armorCurent = armorMax;

            armorCurent = Mathf.Clamp(armorCurent, 0f, armorMax);
            curent = Mathf.Clamp(curent, 0f, max);
            NotifyStats();
        }

        public bool TrySet(float value)
        {
            if (!IsAlive) return false;

            if (value >= 0f)
            {
                curent += value;
                curent = Mathf.Clamp(curent, 0f, max);
                OnHealEv?.Invoke(value);
                NotifyStats();
                return true;
            }

            float damage = -value;
            float absorbedByArmor = Mathf.Min(damage, armorCurent);
            armorCurent -= absorbedByArmor;
            float toHealth = damage - absorbedByArmor;
            curent -= toHealth;
            curent = Mathf.Clamp(curent, 0f, max);
            armorCurent = Mathf.Clamp(armorCurent, 0f, armorMax);

            OnDamageEv?.Invoke(value);
            NotifyStats();

            if (curent <= 0f)
                OnDeadEv?.Invoke();

            return true;
        }

        private void NotifyStats()
        {
            OnStatsChanged?.Invoke();
        }
    }
}
