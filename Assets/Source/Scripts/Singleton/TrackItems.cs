using SiberianGJ26.YouAreDoing.Antos.Abstraction;
using System.Collections.Generic;
using UnityEngine;
using Dany;

namespace SiberianGJ26.YouAreDoing.Antos.Singleton
{
    public class TrackItems : Singleton<TrackItems>
    {
        [SerializeField] private PlayerSpawnState playerSpawnState;

        private List<ObjectTracking> _objectTrackings = new();
        private IHealth _health;

        private bool _isInit = false;

        private void Start()
        {
            playerSpawnState.OnSpawnEv += Init;
        }

        private void Init(FirstPersonController player)
        {
            playerSpawnState.OnSpawnEv -= Init;
            _health = player.Health;
            _health.OnDeadEv += PlayerDestroy;

            if (_isInit) return;
            _isInit = true;
            foreach (var objectTracking in _objectTrackings)
                objectTracking.Init(Instantiate(objectTracking.Prefab, objectTracking.transform));
        }

        public void Add(ObjectTracking objectTracking)
        {
            _objectTrackings.Add(objectTracking);
        }

        public void Remove(ObjectTracking objectTracking)
        {
            _objectTrackings.Remove(objectTracking);
        }

        private void PlayerDestroy()
        {
            _health.OnDeadEv += PlayerDestroy;
            foreach (var objectTracking in _objectTrackings)
            {
                if (objectTracking.IsItemTake())
                    objectTracking.Init(Instantiate(objectTracking.Prefab, objectTracking.transform));
            }

            playerSpawnState.OnSpawnEv += Init;
        }
    }
}