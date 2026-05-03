using SiberianGJ26.YouAreDoing.Antos.Abstraction;
using SiberianGJ26.YouAreDoing.Antos.Readonly;
using System.Collections;
using UnityEngine;
using System;
using Dany;

namespace SiberianGJ26.YouAreDoing.Antos.Spawns
{
    public class PlayerSpawn : MonoBehaviour
    {
        public event Action<FirstPersonController> OnSpawnEv;
        public event Action OnDestroyEv;
        
        [SerializeField] private PlayerSpawnData data;
        [SerializeField] private InventoryManager manager;
        [SerializeField] private WorldPoint playerSpawnPoint;
        [SerializeField] private bool isRespawn;
        [SerializeField] private CheckPoint[] checkPoints;

        private WaitForSeconds _wait;
        private IHealth _playerHealth;

        private Transform _curentSpawnPoint;

        private void Start()
        {
            isRespawn = data.IsRespawnAfterDead;
            _wait = new(data.Duration);
            _curentSpawnPoint = playerSpawnPoint.transform;
            foreach (var checkPoint in checkPoints)
                checkPoint.OnTriggerEv += OnTriggerCheckPoint;
            StartCoroutine(Spawn(null));
        }

        private void OnDestroy()
        {
            foreach (var checkPoint in checkPoints)
                checkPoint.OnTriggerEv -= OnTriggerCheckPoint;
        }

        private void OnTriggerCheckPoint(CheckPoint checkPoint)
        {
            _curentSpawnPoint = checkPoint.transform;
        }

        private IEnumerator Spawn(WaitForSeconds wait)
        {
            if (wait != null) yield return wait;
            yield return null;
            var player = Instantiate(data.PlayerPrefab);
            var spawnPos = _curentSpawnPoint.transform.position;
            var spawnRot = _curentSpawnPoint.transform.rotation;
            // CharacterController keeps internal capsule at old world pose unless disabled during teleport.
            player.CharacterController.enabled = false;
            player.transform.SetPositionAndRotation(spawnPos, spawnRot);
            player.CharacterController.enabled = true;
            _playerHealth = player.Health;
            _playerHealth.OnDeadEv += OnDeadPlayer;
            player.Init(manager);
            OnSpawnEv?.Invoke(player);
        }

        private void OnDeadPlayer()
        {
            _playerHealth.OnDeadEv -= OnDeadPlayer;
             OnDestroyEv?.Invoke();
            if (isRespawn)
                StartCoroutine(Spawn(_wait)); 
        }
    }
}