using SiberianGJ26.YouAreDoing.Antos.Abstraction;
using System.Collections.Generic;

namespace SiberianGJ26.YouAreDoing.Antos.Singleton
{
    public class MonoUpdater : Singleton<MonoUpdater>
    {
        private List<IMonoUpdate> _updates = new();

        public void Add(IMonoUpdate obj)
        {
            _updates.Add(obj);
        }

        public void Remove(IMonoUpdate obj)
        {
            _updates.Remove(obj);
        }

        private void Update()
        {
            for (var i = _updates.Count - 1; i >= 0; i--)
                _updates[i]?.OnUpdate();
        }
    }
}