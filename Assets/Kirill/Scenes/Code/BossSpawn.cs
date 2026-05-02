using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Dany;
using SiberianGJ26.YouAreDoing.Antos.Modules;

namespace Kirill
{
    public class BossSpawn : MonoBehaviour
    {
        [SerializeField] private List<BossPartMob> mobes;
        [SerializeField] private Transform destinationPoint;
        [SerializeField] private int mobesInPath;
        [SerializeField] private float stoppingDistance;
        [SerializeField] private float mergeTime;
        [SerializeField] private GameObject Boss;

        private void Start()
        {
            mobesInPath = mobes.Count;
        }
        private void OnTriggerEnter(Collider other)
        {
            if (!other.CompareTag("Player")) return;
            foreach (BossPartMob mob in mobes)
            {
                mob.WakeUp(destinationPoint.position, stoppingDistance);
            }
        }
        public void whenMobeHadAchivedPoint()
        {
            mobesInPath -= 1;
            if (mobesInPath == 0)
            {
                foreach (BossPartMob mob in mobes)
                {
                    mob.Merge(destinationPoint.position, mergeTime);
                }
                mobes.Clear();
                Invoke("SpawnBoss", mergeTime);
            }
        }
        private void SpawnBoss()
        {
            Boss.SetActive(true);
        }
    }
}
