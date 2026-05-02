using SiberianGJ26.YouAreDoing.Antos.Vfx;
using UnityEngine;

namespace SiberianGJ26.YouAreDoing.Antos.Readonly
{
    [CreateAssetMenu(menuName = "Game/Configs/VFX")]
    public class VfxData : ScriptableObject
    {
        [field: SerializeField] public MonoVFX Prefab { get; private set; }
        [field: SerializeField] public float Duration { get; private set; }
    }
}