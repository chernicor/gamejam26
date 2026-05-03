using Dany;
using FMODUnity;
using UnityEngine;

namespace SiberianGJ26.YouAreDoing.Antos.Readonly
{
    [CreateAssetMenu(menuName = "Game/Configs/Spawn/PlayerSpawn")]
    public class PlayerSpawnData : ScriptableObject
    {
        [field: SerializeField] public FirstPersonController PlayerPrefab { get; private set; }
        [field: SerializeField] public bool IsRespawnAfterDead { get; private set; } = true;
        [field: SerializeField] public float Duration { get; private set; } = 1f;

        [field: SerializeField, Header("Audio (FMOD)")]
        [Tooltip("Озвучка при респавне после смерти (не играет при первом появлении на уровне).")]
        public EventReference RespawnFmodEvent { get; private set; }
    }
}