using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using Dany;
using SiberianGJ26.YouAreDoing.Antos.Modules;


namespace Kirill
{
    public class Boss : MonoBehaviour
    {
        [SerializeField] private string state = "idle"; //idle, rush, meleeAttack, rotate, rangeAttack, ?reloading?, chase
        [SerializeField] private string phase = "melee"; //melee, range
        [SerializeField] private NavMeshAgent agent;
        [SerializeField] private Animator anim;
        private Transform player;
        private MonoHealth playerHealth;
        [Header("Settings")]
        [SerializeField] private float timeToMeleePhase;
        [SerializeField] private float timeToRangePhase;
        [SerializeField] private float meleeAttackTime;
        [SerializeField] private float inaccuracy;
        [SerializeField] private float rangeAttackTime;
        [SerializeField] private float rangeAttackMaxDistance;
        [SerializeField] private float rangeDamage;
        [SerializeField] private float preferredDistanceToPlayer;
        [SerializeField] private float rotateChance;
        [SerializeField] private float meleeAttackDistance;
        [SerializeField] private float meleeDamage;

        public void SetPlayerLinks(Transform playerTransform, MonoHealth playerHealth)
        {
            player = playerTransform;
            this.playerHealth = playerHealth;
        }
        private void Start()
        {
            FirstPersonController p = FindObjectOfType<FirstPersonController>();
            SetPlayerLinks(p.transform, p.GetComponent<MonoHealth>());
            if (phase == "melee") StartCoroutine(ChangePhase(timeToRangePhase));
            else if (phase == "range") StartCoroutine(ChangePhase(timeToMeleePhase));
        }
        public IEnumerator ChangePhase(float delay)
        {
            yield return new WaitForSeconds(delay);
            if (phase == "melee")
            {
                phase = "range";
                state = "idle";
                StartCoroutine(ChangePhase(timeToMeleePhase));
            }
            else if (phase == "range")
            {
                phase = "melee";
                state = "idle";
                StartCoroutine(ChangePhase(timeToRangePhase));
            }
        }
        private void FixedUpdate()
        {
            if (phase == "melee")
            {
                if (state == "rush" || state == "idle") Rush();
            }
            else if(phase == "range")
            {
                if (state == "idle")
                {
                    if (Random.Range(0f, 1f) < rotateChance) Rotate();
                    else StartCoroutine(RangeAttack());
                }
                else if (state == "chase" && Vector3.Distance(transform.position, player.position) < preferredDistanceToPlayer)
                {
                    if (isBossSeePlayer()) StartCoroutine(RangeAttack());
                }
                else if (agent.velocity.magnitude < 0.1f && (state == "rotate" || state == "chase")) StartCoroutine(RangeAttack());
                else if (state == "rotate" && Vector3.Distance(transform.position, player.position) < 10) StartCoroutine(RangeAttack());
            }
            if(agent.velocity.magnitude > 0.1f) anim.SetBool("Walk", true);
            else anim.SetBool("Walk", false);
        }
        private void Rush()
        {
            agent.SetDestination(player.position);
            state = "rush";
            if (Vector3.Distance(transform.position, player.position) < meleeAttackDistance)
            {
                StartCoroutine(MeleeAttack());
                state = "meleeAttack";
            }
        }
        private IEnumerator MeleeAttack()
        {
            anim.SetTrigger("Punch");
            playerHealth.TrySet(-meleeDamage);
            if (player == null)
            {
                FirstPersonController p = FindObjectOfType<FirstPersonController>();
                SetPlayerLinks(p.transform, p.GetComponent<MonoHealth>());
            }
            //анимация
            yield return new WaitForSeconds(meleeAttackTime);
            state = "idle";
        }
        private void RangePlayerChasing()
        {
            state = "chase";
            agent.SetDestination(player.position + (Vector3.right * Random.Range(-1f, 1f) + Vector3.forward * Random.Range(-1f, 1f)) * preferredDistanceToPlayer * 0.8f);
        }
        private void Rotate()
        {
            state = "rotate";
            agent.SetDestination(player.position + (Vector3.right * Random.Range(-1f, 1f) + Vector3.forward * Random.Range(-1f, 1f)).normalized * preferredDistanceToPlayer * Random.Range(0.7f, 1.2f));
        }
        private IEnumerator RangeAttack()
        {
            agent.isStopped = true;
            Ray _ray = new Ray(transform.position, player.position - transform.position);
            Debug.DrawRay(transform.position, player.position - transform.position);
            Physics.Raycast(_ray, out RaycastHit _hit);
            //Debug.Log(_hit.collider.tag);
            if (_hit.distance > rangeAttackMaxDistance || !_hit.collider.CompareTag("Player"))
            {
                agent.isStopped = false;
                RangePlayerChasing();
                yield break;
            }
            state = "rangeAttack";
            anim.SetTrigger("Shoot");
            Vector3 shootDir = player.position + Vector3.up * Random.Range(-inaccuracy, +inaccuracy) + Vector3.right * Random.Range(-inaccuracy, +inaccuracy) - transform.position;
            Ray _shoot = new Ray(transform.position, shootDir);
            Debug.DrawRay(transform.position, shootDir);
            Physics.Raycast(_ray, out RaycastHit hit);
            if (hit.collider.CompareTag("Player"))
            {
                playerHealth.TrySet(-rangeDamage);
                if(player == null)
                {
                    FirstPersonController p = FindObjectOfType<FirstPersonController>();
                    SetPlayerLinks(p.transform, p.GetComponent<MonoHealth>());
                }
            }

            //анимация
            yield return new WaitForSeconds(rangeAttackTime);
            state = "idle";
            agent.isStopped = false;
        }
        private bool isBossSeePlayer()
        {
            Ray _ray = new Ray(transform.position, player.position - transform.position);
            Physics.Raycast(_ray, out RaycastHit _hit);
            if (_hit.collider.CompareTag("Player"))
            {
                return true;
            }
            return false;
        }
    }
}

