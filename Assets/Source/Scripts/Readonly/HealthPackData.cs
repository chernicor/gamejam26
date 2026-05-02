using UnityEngine;

namespace SiberianGJ26.YouAreDoing.Antos.Readonly
{
    [CreateAssetMenu(menuName = "Game/Configs/Items/HealtPack")]
    public class HealthPackData : ScriptableObject
    {
        [field: SerializeField] public float Value { get; private set; } = 10f;
        [field: SerializeField] public float Radius { get; private set; } = 1f;
        [field: SerializeField] public LayerMask DetectLayer { get; private set; }
        [field: SerializeField] public VfxData Effect { get; private set;}
        [field: SerializeField, Header("Gizmos")] public Color GizmosColor { get; private set; }
    }
}