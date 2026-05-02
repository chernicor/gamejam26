using SiberianGJ26.YouAreDoing.Antos.Singleton;
using System.Collections.Generic;
using System.Collections;
using UnityEngine;
using System;
using Dany;

namespace SiberianGJ26.YouAreDoing.Antos.Spawns
{
    [Serializable]
    public struct ItemSpawnContainer
    {
        [field: SerializeField] public GameObject Item { get; private set; }
        [field: SerializeField] public WorldPoint SpawnPoint { get; private set; }
    }
    
    public class ItemsSpawn : MonoBehaviour
    {
        [SerializeField] private ItemSpawnContainer[] containers;
        [SerializeField] private Transform content;
        [SerializeField] private float durationBeetwen = .1f;

        private List<GameObject> _items;
        private WaitForSeconds _wait;
        private Coroutine _coroutine;
        
        //Singleton
        private PlayerSpawnState _playerSpawnState;

        private void Start()
        {
            _playerSpawnState = PlayerSpawnState.Instance;
            _playerSpawnState.OnSpawnEv += Init;
        }

        public void Init(FirstPersonController player)
        {
            _playerSpawnState.OnSpawnEv -= Init;
            _playerSpawnState.OnDestroyEv += PlayerDead;
            _wait ??= new(durationBeetwen);
            _items ??= new(containers.Length);
            
            if (_coroutine != null)
                StopCoroutine(_coroutine);

            if (_items.Count > 0)
                for (var i = _items.Count - 1; i >= 0; i--)
                    if (_items[i] != null) 
                        Destroy(_items[i]);
            
            _items.Clear();

            _coroutine = StartCoroutine(Spawn());
        }

        private IEnumerator Spawn()
        {
            foreach (var container in containers)
            {
                yield return _wait;
                var item = Instantiate(container.Item, content);
                item.transform.position = container.SpawnPoint.transform.position;
                _items.Add(item);
            }
        }

        private void PlayerDead()
        {
            _playerSpawnState.OnDestroyEv -= PlayerDead;
            _playerSpawnState.OnSpawnEv += Init;
        }
    }
}