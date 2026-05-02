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

        private WaitForSeconds _wait;
        private IHealth _playerHealth;

        private void Start()
        {
            isRespawn = data.IsRespawnAfterDead;
            _wait = new(data.Duration);
            StartCoroutine(Spawn(null));
            //StartCoroutine(Damage());
        }

        private IEnumerator Spawn(WaitForSeconds wait)
        {
            if (wait != null) yield return wait;
            yield return null;
            var player = Instantiate(data.PlayerPrefab);
            var spawnPos = playerSpawnPoint.transform.position;
            var spawnRot = playerSpawnPoint.transform.rotation;
            // CharacterController keeps internal capsule at old world pose unless disabled during teleport.
            var cc = player.GetComponent<CharacterController>();
            if (cc != null) cc.enabled = false;
            player.transform.SetPositionAndRotation(spawnPos, spawnRot);
            if (cc != null) cc.enabled = true;
            _playerHealth = player.Health;
            _playerHealth.OnDeadEv += OnDeadPlayer;
            player.Init(manager);
            OnSpawnEv?.Invoke(player);
        }

        private IEnumerator Damage()
        {
            yield return new WaitForSeconds(10f);
           _playerHealth.TrySet(-_playerHealth.Max);
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