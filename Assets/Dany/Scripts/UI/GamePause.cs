using UnityEngine;

namespace Dany
{
    /// <summary>Глобальная пауза: только масштаб времени и флаг (курсор/UI — в <see cref="PauseMenuController"/>).</summary>
    public static class GamePause
    {
        public static bool IsPaused { get; private set; }

        private static float _savedTimeScale = 1f;

        public static void SetPaused(bool paused)
        {
            if (paused == IsPaused) return;

            IsPaused = paused;
            if (paused)
            {
                _savedTimeScale = Time.timeScale;
                Time.timeScale = 0f;
            }
            else
            {
                Time.timeScale = _savedTimeScale > 0f ? _savedTimeScale : 1f;
            }
        }
    }
}
