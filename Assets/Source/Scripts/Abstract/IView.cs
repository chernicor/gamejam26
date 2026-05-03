using UnityEngine;

namespace SiberianGJ26.YouAreDoing.Antos.Abstraction
{
    public interface IView
    {
        public void Show();
        public void Hide();
    }

    public abstract class MonoView : MonoBehaviour, IView
    {
        public virtual void Show()
        {
            gameObject.SetActive(true);
        }

        public virtual void Hide()
        {
            gameObject.SetActive(false);
        }
    }
}