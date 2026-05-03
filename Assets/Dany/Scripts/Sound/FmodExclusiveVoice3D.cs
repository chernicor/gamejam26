using FMOD.Studio;
using FMODUnity;
using UnityEngine;

namespace Dany
{
    /// <summary>
    /// Одна активная 3D-реплика на канале «не бой»: пока фраза в PLAYING/STARTING/STOPPING,
    /// <see cref="Play"/> без прерывания не стартует новую. С <paramref name="interruptCurrent"/> —
    /// текущая останавливается и играет новая (фраза зоны выхода с квестом).
    /// </summary>
    public static class FmodExclusiveVoice3D
    {
        private static EventInstance _activeVoice;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            ReleaseActiveVoice();
        }

        /// <summary>Идёт ли сейчас реплика с этого канала (хилка, зона выхода, респавн и т.д.).</summary>
        public static bool IsExclusiveVoiceBusy()
        {
            ReleaseIfStopped();
            if (!_activeVoice.isValid())
                return false;

            _activeVoice.getPlaybackState(out PLAYBACK_STATE state);
            return state == PLAYBACK_STATE.PLAYING || state == PLAYBACK_STATE.STARTING ||
                   state == PLAYBACK_STATE.STOPPING;
        }

        public static void Play(EventReference eventRef, Vector3 worldPosition, bool interruptCurrent = false)
        {
            if (eventRef.IsNull)
                return;

            ReleaseIfStopped();

            if (_activeVoice.isValid())
            {
                _activeVoice.getPlaybackState(out PLAYBACK_STATE state);
                bool busy = state == PLAYBACK_STATE.PLAYING || state == PLAYBACK_STATE.STARTING ||
                             state == PLAYBACK_STATE.STOPPING;
                if (busy && !interruptCurrent)
                    return;

                ReleaseActiveVoice();
            }

            EventInstance instance = RuntimeManager.CreateInstance(eventRef);
            if (!instance.isValid())
                return;

            instance.set3DAttributes(RuntimeUtils.To3DAttributes(worldPosition));
            instance.start();
            _activeVoice = instance;
        }

        private static void ReleaseIfStopped()
        {
            if (!_activeVoice.isValid())
                return;

            _activeVoice.getPlaybackState(out PLAYBACK_STATE state);
            if (state == PLAYBACK_STATE.STOPPED)
                ReleaseActiveVoice();
        }

        private static void ReleaseActiveVoice()
        {
            if (!_activeVoice.isValid())
                return;

            _activeVoice.stop(FMOD.Studio.STOP_MODE.IMMEDIATE);
            _activeVoice.release();
            _activeVoice.clearHandle();
        }
    }
}
