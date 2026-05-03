using SiberianGJ26.YouAreDoing.Antos.Abstraction;
using SiberianGJ26.YouAreDoing.Antos.Singleton;
using SiberianGJ26.YouAreDoing.Antos.UI;
using UnityEngine;
using FMODUnity;

namespace Dany
{
    /// <summary>ESC — панель паузы, остановка времени и звука (FMOD).</summary>
    public class PauseMenuController : MonoBehaviour,IMonoUpdate
    {
        [SerializeField] private UI_WindowMenu windowMenu;
        
        //Singelton
        private MonoUpdater _monoUpdater;

        private void Start()
        {
            _monoUpdater = MonoUpdater.Instance;
            _monoUpdater.Add(this);
        }

        private void OnDestroy()
        {
            _monoUpdater?.Remove(this);
            if (GamePause.IsPaused)
                Resume();
        }

        public void Pause()
        {
            GamePause.SetPaused(true); 
            windowMenu?.Show();
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            RuntimeManager.PauseAllEvents(true);
        }

        /// <summary>Вызов с кнопки «Продолжить» на Canvas.</summary>
        public void Resume()
        {
            GamePause.SetPaused(false); 
            windowMenu?.Hide();
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            RuntimeManager.PauseAllEvents(false);
        }

        public void OnUpdate()
        {
            if (!Input.GetKeyDown(KeyCode.Escape))
                return;

            if (GamePause.IsPaused)
                Resume();
            else
                Pause();
        }
    }
}