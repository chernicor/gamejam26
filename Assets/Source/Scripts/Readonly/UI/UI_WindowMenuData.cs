using UnityEngine;

namespace SiberianGJ26.YouAreDoing.Antos.Readonly
{
    [CreateAssetMenu(menuName = "Game/Configs/UI/WindowMenu")]
    public class UI_WindowMenuData : ScriptableObject
    {
        [field: SerializeField] public int MenuSceneIndex { get; private set; }
        [field: SerializeField] public int LevelSceneIndex { get; private set; }
        [SerializeField] private string[] playButtonText;
        [SerializeField] private string[] exitButtonText;

        public string GetPlayTextToScene(int indexScene)
        {
            var index = Mathf.Clamp(indexScene, 0, playButtonText.Length - 1);
            return playButtonText[index];
        }
        
        public string GetExitTextToScene(int indexScene)
        {
            var index = Mathf.Clamp(indexScene, 0, playButtonText.Length - 1);
            return exitButtonText[index];
        }
    }
}