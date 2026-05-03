using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using FMODUnity;

namespace Sechin
{
    /// <summary>
    /// Слайдеры громкости → FMOD VCA. Если VCA нет в загруженных банках, исключение не бросается (одно предупреждение в консоль).
    /// Пути можно переопределить в инспекторе под твой FMOD-проект.
    /// </summary>
    public class VCA : MonoBehaviour
    {
        [SerializeField] private FMOD.Studio.EventInstance vcaEvent;

        [Header("Пути VCA в FMOD (vca:/Имя как в Mixer)")]
        [SerializeField] private string musicVcaPath = "vca:/MusicVCA";
        [SerializeField] private string voiceVcaPath = "vca:/VoiceVCA";
        [SerializeField] private string generalVcaPath = "vca:/GeneralVCA";
        [SerializeField] private string effectVcaPath = "vca:/EffectVCA";

        [Header("Громкость голоса")]
        [SerializeField] public Slider volumeSliderVoice;
        private const string VolumeKeyV = "VolumeLevelVoice";

        [Header("Громкость общая")]
        [SerializeField] public Slider volumeSliderGeneral;
        private const string VolumeKeyG = "VolumeLevelGeneral";

        [Header("Громкость музыки")]
        [SerializeField] public Slider volumeSliderMusic;
        private const string VolumeKeyM = "VolumeLevelMusic";

        [Header("Громкость эффектов")]
        [SerializeField] public Slider volumeSliderSound;
        private const string VolumeKeyS = "VolumeLevelSound";

        private static readonly HashSet<string> WarnedMissingVcaPaths = new HashSet<string>();

        private void Start()
        {
            LoadVolumeToSlidersWithoutNotify();
            if (volumeSliderVoice != null)
                volumeSliderVoice.onValueChanged.AddListener(SetVolumeVoice);
            if (volumeSliderGeneral != null)
                volumeSliderGeneral.onValueChanged.AddListener(SetVolumeGeneral);
            if (volumeSliderMusic != null)
                volumeSliderMusic.onValueChanged.AddListener(SetVolumeMusic);
            if (volumeSliderSound != null)
                volumeSliderSound.onValueChanged.AddListener(SetVolumeSound);
            SyncFmodFromSliders();
        }

        public void LoadVolume()
        {
            LoadVolumeToSlidersWithoutNotify();
            SyncFmodFromSliders();
        }

        private void LoadVolumeToSlidersWithoutNotify()
        {
            SetSliderFromPrefs(volumeSliderVoice, VolumeKeyV, 1f);
            SetSliderFromPrefs(volumeSliderGeneral, VolumeKeyG, 1f);
            SetSliderFromPrefs(volumeSliderMusic, VolumeKeyM, 1f);
            SetSliderFromPrefs(volumeSliderSound, VolumeKeyS, 1f);
        }

        private static void SetSliderFromPrefs(Slider slider, string key, float defaultValue)
        {
            if (slider == null) return;
            float v = PlayerPrefs.HasKey(key) ? PlayerPrefs.GetFloat(key) : defaultValue;
            slider.SetValueWithoutNotify(v);
        }

        private void SyncFmodFromSliders()
        {
            if (volumeSliderMusic != null)
                TrySetVcaVolume(musicVcaPath, volumeSliderMusic.value);
            if (volumeSliderVoice != null)
                TrySetVcaVolume(voiceVcaPath, volumeSliderVoice.value);
            if (volumeSliderGeneral != null)
                TrySetVcaVolume(generalVcaPath, volumeSliderGeneral.value);
            if (volumeSliderSound != null)
                TrySetVcaVolume(effectVcaPath, volumeSliderSound.value);
        }

        public void SetVolumeMusic(float volume)
        {
            volume = Mathf.Clamp01(volume);
            TrySetVcaVolume(musicVcaPath, volume);
            PlayerPrefs.SetFloat(VolumeKeyM, volume);
            PlayerPrefs.Save();
        }

        public void SetVolumeVoice(float volume)
        {
            volume = Mathf.Clamp01(volume);
            TrySetVcaVolume(voiceVcaPath, volume);
            PlayerPrefs.SetFloat(VolumeKeyV, volume);
            PlayerPrefs.Save();
        }

        public void SetVolumeGeneral(float volume)
        {
            volume = Mathf.Clamp01(volume);
            TrySetVcaVolume(generalVcaPath, volume);
            PlayerPrefs.SetFloat(VolumeKeyG, volume);
            PlayerPrefs.Save();
        }

        public void SetVolumeSound(float volume)
        {
            volume = Mathf.Clamp01(volume);
            TrySetVcaVolume(effectVcaPath, volume);
            PlayerPrefs.SetFloat(VolumeKeyS, volume);
            PlayerPrefs.Save();
        }

        private void TrySetVcaVolume(string path, float volume)
        {
            if (string.IsNullOrWhiteSpace(path)) return;
            if (!RuntimeManager.IsInitialized) return;

            var system = RuntimeManager.StudioSystem;
            if (!system.isValid()) return;

            if (system.getVCA(path, out FMOD.Studio.VCA vca) != FMOD.RESULT.OK || !vca.isValid())
            {
                if (WarnedMissingVcaPaths.Add(path))
                {
                    Debug.LogWarning(
                        $"[FMOD] VCA «{path}» не найден в загруженных банках. " +
                        "Проверь FMOD → Banks (Master + группа с музыкой), строки в Master Strings, " +
                        "и что имя VCA в Mixer совпадает с путём. Путь можно сменить на объекте с компонентом VCA.",
                        this);
                }

                return;
            }

            vca.setVolume(volume);
        }
    }
}
