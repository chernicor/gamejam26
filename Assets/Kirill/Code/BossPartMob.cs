using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace Kirill
{
    public class BossPartMob : MonoBehaviour
    {
        [SerializeField] private NavMeshAgent agent;
        [SerializeField] private BossSpawn spawner;
        public void WakeUp(Vector3 destinationPoint, float stoppingDistance)
        {
            Invoke("en", 1);
            agent.stoppingDistance = stoppingDistance;
            agent.SetDestination(destinationPoint);
        }
        void en()
        {
            enabled = true;
        }
        public void Merge(Vector3 destinationPoint, float mergeTime)
        {
            agent.stoppingDistance = 0;
            agent.SetDestination(destinationPoint);
            Invoke("DestroyMob", mergeTime);
        }
        private void DestroyMob()
        {
            Destroy(gameObject);
        }
        private void FixedUpdate()
        {
            if (agent.velocity.magnitude < 0.1f) { spawner.whenMobeHadAchivedPoint(); enabled = false; }
        }
    }
}
