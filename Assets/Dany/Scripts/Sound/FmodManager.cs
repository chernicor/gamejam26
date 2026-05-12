using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Sechin
{
    public class FmodManager : MonoBehaviour
    {
        [Header("?????????? ????????")]

        [Header("??????? ?????")]
        public Slider volumeSliderGeneral;
        private const string VolumeKeyGeneral = "VolumeLevelGeneral";

        [Header("??????? ?????")]
        public Slider volumeSliderSound;
        private const string VolumeKeySound = "VolumeLevelSound";

        [Header("??????? ??????")]
        public Slider volumeSliderMusic;
        private const string VolumeKeyMusic = "VolumeLevelMusic";

        [Header("??????? ??????")]
        public Slider volumeSliderVoice;
        private const string VolumeKeyVoice = "VolumeLevelVoice";



        void Start()
        {
            LoadVolume();
        }
        public void LoadVolume()
        {
            // ?????? ?????? ? Slider.value ??? ??????: ??? ???????? onValueChanged ? VCA ? FMOD ??
            // ?????????? ????????????? RuntimeManager ?? WebGL (memory access out of bounds).
            SetSliderFromPrefs(volumeSliderGeneral, VolumeKeyGeneral, 1f);
            SetSliderFromPrefs(volumeSliderMusic, VolumeKeyMusic, 1f);
            SetSliderFromPrefs(volumeSliderSound, VolumeKeySound, 1f);
            SetSliderFromPrefs(volumeSliderVoice, VolumeKeyVoice, 1f);
        }

        private static void SetSliderFromPrefs(Slider slider, string key, float defaultValue)
        {
            if (slider == null) return;
            float v = PlayerPrefs.HasKey(key) ? PlayerPrefs.GetFloat(key) : defaultValue;
            slider.SetValueWithoutNotify(v);
        }
        public void SaveVolume()
        {
            if (volumeSliderMusic != null)
                PlayerPrefs.SetFloat(VolumeKeyMusic, volumeSliderMusic.value);
            if (volumeSliderSound != null)
                PlayerPrefs.SetFloat(VolumeKeySound, volumeSliderSound.value);
            PlayerPrefs.Save();
        }
    }

}