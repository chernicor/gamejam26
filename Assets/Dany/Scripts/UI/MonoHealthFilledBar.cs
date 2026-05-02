using System.Collections;
using SiberianGJ26.YouAreDoing.Antos.Modules;
using SiberianGJ26.YouAreDoing.Antos.Spawns;
using UnityEngine;
using UnityEngine.UI;

namespace Dany
{
    /// <summary>
    /// Заполняет UI Image (Type = Filled) по доле здоровья и брони.
    /// При включённом авто-поиске подписывается на спавн игрока и находит MonoHealth сам.
    /// </summary>
    [DisallowMultipleComponent]
    public class MonoHealthFilledBar : MonoBehaviour
    {
        [Header("Source")]
        [Tooltip("Если включено — Mono Health можно не задавать: берётся с игрока при появлении.")]
        [SerializeField] private bool autoBindPlayer = true;
        [SerializeField] private MonoHealth health;
        [SerializeField] private Image healthFill;
        [SerializeField] private Image armorFill;

        [Header("Behaviour")]
        [SerializeField] private bool hideArmorWhenEmpty = true;

        private PlayerSpawn _playerSpawn;

        private void OnEnable()
        {
            if (autoBindPlayer)
                StartCoroutine(CoAutoBind());
            else
            {
                Subscribe();
                Refresh();
            }
        }

        private void OnDisable()
        {
            StopAllCoroutines();
            if (_playerSpawn != null)
            {
                _playerSpawn.OnSpawnEv -= HandlePlayerSpawned;
                _playerSpawn = null;
            }

            Unsubscribe();
        }

        private IEnumerator CoAutoBind()
        {
            for (int i = 0; i < 120 && enabled; i++)
            {
                if (_playerSpawn == null)
                    _playerSpawn = FindObjectOfType<PlayerSpawn>();

                if (_playerSpawn != null)
                {
                    _playerSpawn.OnSpawnEv -= HandlePlayerSpawned;
                    _playerSpawn.OnSpawnEv += HandlePlayerSpawned;
                    break;
                }

                yield return null;
            }

            TryBindExistingPlayer();

            for (int i = 0; i < 180 && enabled && health == null; i++)
            {
                TryBindExistingPlayer();
                yield return null;
            }
        }

        private void HandlePlayerSpawned(FirstPersonController player)
        {
            if (player != null && player.Health != null)
                SetHealthSource(player.Health);
        }

        private void TryBindExistingPlayer()
        {
            var fpc = FindObjectOfType<FirstPersonController>();
            if (fpc != null && fpc.Health != null)
                SetHealthSource(fpc.Health);
        }

        /// <summary>Подмена источника в рантайме (например после спавна игрока).</summary>
        public void SetHealthSource(MonoHealth source)
        {
            Unsubscribe();
            health = source;
            Subscribe();
            Refresh();
        }

        private void Subscribe()
        {
            if (health == null) return;
            health.OnStatsChanged += Refresh;
            health.OnDeadEv += OnDead;
        }

        private void Unsubscribe()
        {
            if (health == null) return;
            health.OnStatsChanged -= Refresh;
            health.OnDeadEv -= OnDead;
        }

        private void OnDead()
        {
            Refresh();
        }

        public void Refresh()
        {
            if (health == null) return;

            if (healthFill != null)
            {
                healthFill.type = Image.Type.Filled;
                healthFill.fillAmount = health.Max > 0.001f ? Mathf.Clamp01(health.Curent / health.Max) : 0f;
            }

            if (armorFill != null)
            {
                bool showArmor = !hideArmorWhenEmpty || health.ArmorMax > 0.001f;
                armorFill.gameObject.SetActive(showArmor);
                if (showArmor)
                {
                    armorFill.type = Image.Type.Filled;
                    armorFill.fillAmount = health.ArmorMax > 0.001f
                        ? Mathf.Clamp01(health.ArmorCurent / health.ArmorMax)
                        : 0f;
                }
            }
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (!autoBindPlayer && health != null && isActiveAndEnabled)
                Refresh();
        }
#endif
    }
}
