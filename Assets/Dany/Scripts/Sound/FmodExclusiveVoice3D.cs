using FMOD.Studio;
using FMODUnity;
using UnityEngine;

namespace Dany
{
    /// <summary>
    /// Одна активная 3D-реплика на всё приложение: пока фраза в PLAYING/STARTING/STOPPING,
    /// новая не стартует (любой другой FMOD event). Для озвучки не из оружия: хилка, квест, респавн, коллекционки.
    /// Выстрелы и <see cref="CombatVoiceBarks"/> сюда не подключаются.
    /// </summary>
    public static class FmodExclusiveVoice3D
    {
        private static EventInstance _activeVoice;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            if (!_activeVoice.isValid())
                return;
            _activeVoice.stop(FMOD.Studio.STOP_MODE.IMMEDIATE);
            _activeVoice.release();
            _activeVoice.clearHandle();
        }

        public static void Play(EventReference eventRef, Vector3 worldPosition)
        {
            if (eventRef.IsNull)
                return;

            if (_activeVoice.isValid())
            {
                _activeVoice.getPlaybackState(out PLAYBACK_STATE state);
                if (state == PLAYBACK_STATE.PLAYING || state == PLAYBACK_STATE.STARTING ||
                    state == PLAYBACK_STATE.STOPPING)
                    return;

                _activeVoice.release();
                _activeVoice.clearHandle();
            }

            EventInstance instance = RuntimeManager.CreateInstance(eventRef);
            if (!instance.isValid())
                return;

            instance.set3DAttributes(RuntimeUtils.To3DAttributes(worldPosition));
            instance.start();
            _activeVoice = instance;
        }
    }
}
