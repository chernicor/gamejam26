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

        private void Awake()
        {
            if (windowMenu == null)
                windowMenu = FindFirstObjectByType<UI_WindowMenu>();
        }

        private void Start()
        {
            _monoUpdater = MonoUpdater.Instance;
            _monoUpdater.Add(this);
        }

        private void OnDestroy()
        {
            _monoUpdater?.Remove(this);
            // Не вызывать Resume(): при выгрузке сцены (выход в меню с открытой паузой) он снова
            // заблокирует курсор — в следующей сцене курсор останется скрытым.
            if (GamePause.IsPaused)
            {
                GamePause.SetPaused(false);
                RuntimeManager.PauseAllEvents(false);
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
        }

        public void Pause()
        {
            GamePause.SetPaused(true); 
            windowMenu?.Show();
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            RuntimeManager.PauseAllEvents(true);
        }

        /// <summary>Для кнопки «Продолжить» (UnityEvent) — снять паузу и вернуться в игру.</summary>
        public void ContinuePlaying() => Resume();

        /// <summary>Снять паузу (ESC или кнопка).</summary>
        public void Resume()
        {
            if (!GamePause.IsPaused) return;

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