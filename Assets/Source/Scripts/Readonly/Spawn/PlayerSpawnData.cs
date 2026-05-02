using UnityEngine;
using Dany;

namespace SiberianGJ26.YouAreDoing.Antos.Readonly
{
    [CreateAssetMenu(menuName = "Game/Configs/Spawn/PlayerSpawn")]
    public class PlayerSpawnData : ScriptableObject
    {
        [field: SerializeField] public FirstPersonController PlayerPrefab { get; private set; }
        [field: SerializeField] public bool IsRespawnAfterDead { get; private set; } = true;
        [field: SerializeField] public float Duration { get; private set; } = 1f;
    }
}