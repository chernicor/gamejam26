using UnityEngine;
using UnityEngine.UI;
using FMODUnity;

namespace Sechin
{
    public class VCA : MonoBehaviour
    {
        [SerializeField] private FMOD.Studio.EventInstance vcaEvent;

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

        private void Start()
        {
            LoadVolumeToSlidersWithoutNotify();
            volumeSliderVoice.onValueChanged.AddListener(SetVolumeVoice);
            volumeSliderGeneral.onValueChanged.AddListener(SetVolumeGeneral);
            volumeSliderMusic.onValueChanged.AddListener(SetVolumeMusic);
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
            float v = PlayerPrefs.HasKey(key) ? PlayerPrefs.GetFloat(key) : defaultValue;
            slider.SetValueWithoutNotify(v);
        }

        private void SyncFmodFromSliders()
        {
            if (!RuntimeManager.IsInitialized) return;
            RuntimeManager.GetVCA("vca:/MusicVCA").setVolume(volumeSliderMusic.value);
            RuntimeManager.GetVCA("vca:/VoiceVCA").setVolume(volumeSliderVoice.value);
            RuntimeManager.GetVCA("vca:/GeneralVCA").setVolume(volumeSliderGeneral.value);
            RuntimeManager.GetVCA("vca:/EffectVCA").setVolume(volumeSliderSound.value);
        }

        public void SetVolumeMusic(float volume)
        {
            volume = Mathf.Clamp01(volume);
            RuntimeManager.GetVCA("vca:/MusicVCA").setVolume(volume);
            PlayerPrefs.SetFloat(VolumeKeyM, volume);
            PlayerPrefs.Save();
        }

        public void SetVolumeVoice(float volume)
        {
            volume = Mathf.Clamp01(volume);
            RuntimeManager.GetVCA("vca:/VoiceVCA").setVolume(volume);
            PlayerPrefs.SetFloat(VolumeKeyV, volume);
            PlayerPrefs.Save();
        }

        public void SetVolumeGeneral(float volume)
        {
            volume = Mathf.Clamp01(volume);
            RuntimeManager.GetVCA("vca:/GeneralVCA").setVolume(volume);
            PlayerPrefs.SetFloat(VolumeKeyG, volume);
            PlayerPrefs.Save();
        }

        public void SetVolumeSound(float volume)
        {
            volume = Mathf.Clamp01(volume);
            RuntimeManager.GetVCA("vca:/EffectVCA").setVolume(volume);
            PlayerPrefs.SetFloat(VolumeKeyS, volume);
            PlayerPrefs.Save();
        }
    }
}
