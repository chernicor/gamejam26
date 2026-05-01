using UnityEngine;

namespace SiberianGJ26.YouAreDoing.Antos.Readonly
{
    [CreateAssetMenu(menuName = "Game/Configs/HealtPack")]
    public class HealthPackData : ScriptableObject
    {
        [field: SerializeField] public float Value { get; private set; } = 10f;
        [field: SerializeField] public float Range { get; private set; } = 1f;
        [field: SerializeField] public LayerMask DetectLayer { get; private set; }
    }
}