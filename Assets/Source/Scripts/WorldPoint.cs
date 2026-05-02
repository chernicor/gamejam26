using SiberianGJ26.YouAreDoing.Antos.Readonly;
using UnityEngine;

namespace SiberianGJ26.YouAreDoing.Antos
{
    public class WorldPoint : MonoBehaviour
    {
        [field: SerializeField] public WorldPointData Data { get; private set; }

        private void OnDrawGizmos()
        {
            if (Data == null) return;

            Gizmos.color = Data.Color;
            Gizmos.DrawSphere(transform.position,Data.Radius);
        }
    }
}