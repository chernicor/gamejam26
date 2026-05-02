using SiberianGJ26.YouAreDoing.Antos.Modules;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Dany
{
    /// <summary>
    /// Тряска камеры + красный тинт (URP Volume) при уроне через <see cref="MonoHealth.OnDamageEv"/>.
    /// </summary>
    [RequireComponent(typeof(FirstPersonController))]
    public class PlayerDamageFeedback : MonoBehaviour
    {
        [SerializeField] private FirstPersonController player;
        [SerializeField] private MonoHealth health;
        [Tooltip("Профиль только с ColorAdjustments + Vignette (копируется в рантайме).")]
        [SerializeField] private VolumeProfile damagePostProfile;

        [Header("Tint")]
        [SerializeField] private float tintBuildPerDamage = 0.045f;
        [SerializeField] private float tintMax = 0.85f;
        [SerializeField] private float tintDecay = 2.2f;
        [ColorUsage(true, true)]
        [SerializeField] private Color damageColor = new Color(1.35f, 0.2f, 0.12f, 1f);
        [SerializeField] private float vignetteIntensityScale = 0.38f;

        private Volume _volume;
        private ColorAdjustments _colorAdj;
        private Vignette _vignette;
        private float _hurt;

        private void Awake()
        {
            if (player == null)
                player = GetComponent<FirstPersonController>();
            if (health == null)
                health = GetComponent<MonoHealth>();

            if (damagePostProfile == null)
            {
                Debug.LogWarning($"{nameof(PlayerDamageFeedback)}: не назначен Volume Profile — только тряска камеры.");
                return;
            }

            var go = new GameObject("DamageFeedbackVolume");
            go.transform.SetParent(transform, false);
            _volume = go.AddComponent<Volume>();
            _volume.isGlobal = true;
            _volume.priority = 25;
            _volume.weight = 0f;
            _volume.profile = Instantiate(damagePostProfile);

            if (!_volume.profile.TryGet(out _colorAdj) || !_volume.profile.TryGet(out _vignette))
            {
                Debug.LogError($"{nameof(PlayerDamageFeedback)}: в профиле нужны ColorAdjustments и Vignette.");
                _volume = null;
                return;
            }

            _colorAdj.active = true;
            _vignette.active = true;
        }

        private void OnEnable()
        {
            if (health != null)
                health.OnDamageEv += OnDamage;
        }

        private void OnDisable()
        {
            if (health != null)
                health.OnDamageEv -= OnDamage;
        }

        private void OnDamage(float delta)
        {
            if (delta >= 0f || player == null)
                return;

            float dmg = -delta;
            player.AddDamageCameraImpulse(dmg);
            _hurt = Mathf.Min(tintMax, _hurt + dmg * tintBuildPerDamage);
        }

        private void LateUpdate()
        {
            if (_volume == null || _colorAdj == null || _vignette == null)
                return;

            _hurt = Mathf.Max(0f, _hurt - Time.deltaTime * tintDecay);

            float w = Mathf.Clamp01(_hurt / Mathf.Max(0.01f, tintMax));
            _volume.weight = w > 0.001f ? 1f : 0f;

            Color tint = Color.Lerp(Color.white, damageColor, w);
            _colorAdj.colorFilter.overrideState = w > 0.02f;
            _colorAdj.colorFilter.value = tint;

            _vignette.intensity.overrideState = w > 0.02f;
            _vignette.intensity.value = w * vignetteIntensityScale;
            _vignette.color.overrideState = true;
            _vignette.color.value = damageColor;
        }
    }
}
