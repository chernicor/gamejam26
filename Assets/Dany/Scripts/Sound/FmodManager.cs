using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Sechin
{
    public class FmodManager : MonoBehaviour
    {
        [Header("СОХРОНЕНИЕ НАСТРОЕК")]

        [Header("Слайдер общий")]
        public Slider volumeSliderGeneral;
        private const string VolumeKeyGeneral = "VolumeLevelGeneral";

        [Header("Слайдер звука")]
        public Slider volumeSliderSound;
        private const string VolumeKeySound = "VolumeLevelSound";

        [Header("Слайдер музыки")]
        public Slider volumeSliderMusic;
        private const string VolumeKeyMusic = "VolumeLevelMusic";

        [Header("Слайдер голоса")]
        public Slider volumeSliderVoice;
        private const string VolumeKeyVoice = "VolumeLevelVoice";



        void Start()
        {
            LoadVolume();
        }
        public void LoadVolume()
        {
            if (PlayerPrefs.HasKey(VolumeKeyGeneral))//General
            {
                float volume = PlayerPrefs.GetFloat(VolumeKeyGeneral);
                volumeSliderGeneral.value = volume;
            }
            else
            {
                volumeSliderGeneral.value = 1.0f;
            }
            if (PlayerPrefs.HasKey(VolumeKeyMusic))//Music
            {
                float volume = PlayerPrefs.GetFloat(VolumeKeyMusic);
                volumeSliderMusic.value = volume;
            }
            else
            {
                volumeSliderMusic.value = 1.0f;
            }
            if (PlayerPrefs.HasKey(VolumeKeySound))//Sound
            {
                float volume = PlayerPrefs.GetFloat(VolumeKeySound);
                volumeSliderSound.value = volume;
            }
            else
            {
                volumeSliderSound.value = 1.0f;
            }
            if (PlayerPrefs.HasKey(VolumeKeyVoice))//Sound
            {
                float volume = PlayerPrefs.GetFloat(VolumeKeyVoice);
                volumeSliderVoice.value = volume;
            }
            else
            {
                volumeSliderVoice.value = 1.0f;
            }
        }
        public void SaveVolume()
        {
            PlayerPrefs.SetFloat(VolumeKeyMusic, volumeSliderMusic.value);
            PlayerPrefs.SetFloat(VolumeKeySound, volumeSliderSound.value);
            PlayerPrefs.Save();
        }
    }

}