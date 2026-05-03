using SiberianGJ26.YouAreDoing.Antos.Abstraction;
using SiberianGJ26.YouAreDoing.Antos.Readonly;
using UnityEngine.SceneManagement;
using UnityEngine;
using Dany;
using TMPro;

namespace SiberianGJ26.YouAreDoing.Antos.UI
{
    public class UI_WindowMenu : MonoView
    {
        [SerializeField] private UI_WindowMenuData data;
        [SerializeField] private UI_SettingPanel settingPanel;
        [SerializeField] private UI_MenuButtonsPanel menuButtonsPanel;
        [SerializeField] private Canvas self;
        [SerializeField] private TextMeshProUGUI labelPlayButton;
        [SerializeField] private TextMeshProUGUI labelExitButton;
        [SerializeField] private PauseMenuController pauseMenuController;

        private void Start()
        {
            if (data != null)
            {
                labelPlayButton.SetText(data.GetPlayTextToScene(SceneManager.GetActiveScene().buildIndex));
                labelExitButton.SetText(data.GetExitTextToScene(SceneManager.GetActiveScene().buildIndex));
                if (IsCurentScene(data.MenuSceneIndex))
                    UnlockCursorForMenu();
            }
        }

        private static void UnlockCursorForMenu()
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        public override void Show()
        {
            self.enabled = true;
            menuButtonsPanel.Show();
        }

        public override void Hide()
        {
            settingPanel?.Hide();
            self.enabled = false;
        }

        public void Play()
        {
            if (data == null) return;
            if (IsCurentScene(data.LevelSceneIndex))
            {
                ContinueGameplayFromPause();
                return;
            }

            SceneManager.LoadScene(data.LevelSceneIndex);
        }

        /// <summary>
        /// Кнопка «Продолжить»: на сцене уровня — снять паузу и скрыть окно; на сцене меню — загрузить уровень (как «Играть»).
        /// Повесь OnClick на этот метод.
        /// </summary>
        public void ContinueGame()
        {
            if (data == null) return;
            if (IsCurentScene(data.LevelSceneIndex))
                ContinueGameplayFromPause();
            else
                SceneManager.LoadScene(data.LevelSceneIndex);
        }

        private void ContinueGameplayFromPause()
        {
            var pause = pauseMenuController != null
                ? pauseMenuController
                : FindFirstObjectByType<PauseMenuController>();
            pause?.Resume();
            Hide();
        }

        public void Exit()
        {
            if (IsCurentScene(data.MenuSceneIndex))
            {
                Application.Quit();
                return;
            }

            SceneManager.LoadScene(data.MenuSceneIndex);
        }

        private bool IsCurentScene(int indexScene)
        {
            return SceneManager.GetActiveScene().buildIndex == indexScene;
        }
    }
}