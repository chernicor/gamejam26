using SiberianGJ26.YouAreDoing.Antos.Abstraction;
using SiberianGJ26.YouAreDoing.Antos.Readonly;
using UnityEngine.SceneManagement;
using UnityEngine;
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

        private void Start()
        {
            labelPlayButton.SetText(data.GetPlayTextToScene(SceneManager.GetActiveScene().buildIndex));
            labelExitButton.SetText(data.GetExitTextToScene(SceneManager.GetActiveScene().buildIndex));
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
            if (IsCurentScene(data.LevelSceneIndex))
            {
                Hide();
                return;
            }

            SceneManager.LoadScene(data.LevelSceneIndex);
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