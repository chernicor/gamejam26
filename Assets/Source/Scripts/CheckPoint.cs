using UnityEngine;
using System;
using Dany;

namespace SiberianGJ26.YouAreDoing.Antos
{
    public class CheckPoint : WorldPoint
    {
        public event Action<CheckPoint> OnTriggerEv;

        private void OnTriggerEnter(Collider other)
        {
            if (other.TryGetComponent(out FirstPersonController player))
                OnTriggerEv?.Invoke(this);
        }
    }
}