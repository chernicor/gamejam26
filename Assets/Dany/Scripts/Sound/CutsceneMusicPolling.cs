using UnityEngine;
using FMODUnity;
using FMOD.Studio;
using System.Runtime.InteropServices;
using UnityEngine.Playables;

namespace Sechin
{
    public class CutsceneMusicPolling : MonoBehaviour
    {
        public PlayableDirector director; // Присвой в Inspector
        public double markerBTime = 10.0; // Время маркера B в Timeline (секунды)
        public FMODUnity.EventReference fmodEvent; // Один FMOD event с маркерами A и B
        public int markerBPosition = 10000; // Позиция маркера B в FMOD (миллисекунды, настрой под свой event)

        private bool isSwitched = false;
        private FMOD.Studio.EventInstance fmodInstance;

        void Start()
        {
            // Запуск одного FMOD event
            if (!string.IsNullOrEmpty(fmodEvent.ToString()))
            {
                fmodInstance = FMODUnity.RuntimeManager.CreateInstance(fmodEvent);
                fmodInstance.start();
            }

            director.Play();
        }

        // Метод для внешнего переключения (например, от UI)
        public void SetSwitched(bool switched)
        {
            isSwitched = switched;
            Debug.Log("Переключение установлено: " + isSwitched);
        }

        // Сигнал для маркера A
        public void OnSignalA()
        {
            Debug.Log("Сигнал A: Достигли маркера A");

            if (!isSwitched)
            {
                // Продолжаем без изменений
                Debug.Log("Состояние не переключено — продолжаем");
            }
            else
            {
                // Перепрыгиваем к B в Timeline и FMOD
                director.time = markerBTime;
                director.Evaluate(); // Обнови Timeline сразу
                Debug.Log("Перепрыгнули к маркеру B");

                // Синхронизируем FMOD: Перепрыгиваем к позиции B в том же event'е
                if (fmodInstance.isValid())
                {
                    fmodInstance.setTimelinePosition(markerBPosition); // Перепрыгиваем в FMOD
                    Debug.Log("FMOD перепрыгнул к позиции B: " + markerBPosition + " мс");
                }
            }
        }

        // Сигнал для маркера B (после перепрыгивания или естественного достижения)
        public void OnSignalB()
        {
            Debug.Log("Сигнал B: Достигли маркера B — продолжаем кат-сцену");
            // Timeline и FMOD продолжают играть без остановки
            // Добавь здесь логику: диалог, эффекты и т.д.
        }

        void OnDestroy()
        {
            if (fmodInstance.isValid())
            {
                fmodInstance.stop(FMOD.Studio.STOP_MODE.IMMEDIATE);
                fmodInstance.release();
            }
        }

    }
}

