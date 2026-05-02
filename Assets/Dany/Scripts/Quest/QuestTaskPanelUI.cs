using TMPro;
using UnityEngine;

namespace Dany
{
    /// <summary>Панель с TMP — подписка на <see cref="LevelQuestController"/>.</summary>
    public class QuestTaskPanelUI : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI label;
        private LevelQuestController _controller;

        public void Bind(LevelQuestController controller)
        {
            if (_controller != null)
                _controller.OnDisplayChanged -= Refresh;

            _controller = controller;
            if (_controller != null)
                _controller.OnDisplayChanged += Refresh;

            Refresh();
        }

        private void OnDestroy()
        {
            if (_controller != null)
                _controller.OnDisplayChanged -= Refresh;
        }

        private void Refresh()
        {
            if (label == null || _controller == null) return;
            label.text = _controller.BuildPanelText();
        }
    }
}
