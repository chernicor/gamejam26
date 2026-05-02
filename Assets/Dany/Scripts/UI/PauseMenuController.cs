using FMODUnity;
using UnityEngine;

namespace Dany
{
    /// <summary>ESC — панель паузы, остановка времени и звука (FMOD).</summary>
    public class PauseMenuController : MonoBehaviour
    {
        [SerializeField] private GameObject pausePanel;

        private void Awake()
        {
            if (pausePanel != null)
                pausePanel.SetActive(false);
        }

        private void OnDestroy()
        {
            if (GamePause.IsPaused)
                Resume();
        }

        private void Update()
        {
            if (!Input.GetKeyDown(KeyCode.Escape))
                return;

            if (GamePause.IsPaused)
                Resume();
            else
                Pause();
        }

        public void Pause()
        {
            GamePause.SetPaused(true);
            if (pausePanel != null)
                pausePanel.SetActive(true);
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            RuntimeManager.PauseAllEvents(true);
        }

        /// <summary>Вызов с кнопки «Продолжить» на Canvas.</summary>
        public void Resume()
        {
            GamePause.SetPaused(false);
            if (pausePanel != null)
                pausePanel.SetActive(false);
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            RuntimeManager.PauseAllEvents(false);
        }
    }
}
