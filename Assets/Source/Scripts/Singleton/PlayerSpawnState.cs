using SiberianGJ26.YouAreDoing.Antos.Abstraction;
using SiberianGJ26.YouAreDoing.Antos.Spawns;
using System.Collections;
using UnityEngine;
using System;
using Dany;

namespace SiberianGJ26.YouAreDoing.Antos.Singleton
{
    public class PlayerSpawnState : Singleton<PlayerSpawnState>
    {
        public event Action<FirstPersonController> OnSpawnEv;
        public event Action OnDestroyEv;

        [SerializeField] private PlayerSpawn playerSpawn;
        
        private FirstPersonController _player;
        private Coroutine _coroutine;

        public override void Awake()
        {
            playerSpawn.OnSpawnEv += Init;
            playerSpawn.OnDestroyEv += () => OnDestroyEv.Invoke();
            base.Awake();
        }

        private void Init(FirstPersonController player)
        {
            _player = player;
            if (_coroutine == null)
                _coroutine = StartCoroutine(Action());
        }

        private IEnumerator Action()
        {
            while (true)
            {
                yield return null;
                if (_player != null)
                    OnSpawnEv?.Invoke(_player);
            }
        }
    }
}