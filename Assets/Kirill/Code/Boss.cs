using System.Collections;
using System.Collections.Generic;
using FMODUnity;
using UnityEngine;
using UnityEngine.AI;
using Dany;
using SiberianGJ26.YouAreDoing.Antos.Modules;


namespace Kirill
{
    public class Boss : MonoBehaviour, IDeath
    {
        [SerializeField] private string state = "idle"; //idle, rush, meleeAttack, rotate, rangeAttack, ?reloading?, chase
        [SerializeField] private string phase = "melee"; //melee, range
        [SerializeField] private NavMeshAgent agent;
        [SerializeField] private Animator anim;
        [SerializeField] private GameObject ballisticPrefab;
        [SerializeField] private Transform ballisticSpawnPoint;
        private Transform player;
        private MonoHealth playerHealth;
        [SerializeField] private Vector3 lastPosition;
        [SerializeField] private Vector3 playerVelocity;
        [SerializeField] private List<GameObject> animsObj;
        [Header("Settings")]
        [SerializeField] private float timeToMeleePhase;
        [SerializeField] private float timeToRangePhase;
        [SerializeField] private float meleeAttackTime;
        [SerializeField] private float inaccuracy;
        [SerializeField] private float ballSpeed;
        [SerializeField] private bool isBallistic;
        [SerializeField] private float UpryazhdenieCoef;
        [SerializeField] private float rangeAttackTime;
        [SerializeField] private float rangeAttackMaxDistance;
        [SerializeField] private float rangeDamage;
        [SerializeField] private float preferredDistanceToPlayer;
        [SerializeField] private float rotateChance;
        [SerializeField] private float meleeAttackDistance;
        [SerializeField] private float meleeDamage;

        [Header("FMOD (опционально)")]
        [SerializeField] private EventReference fmodDeathEvent;
        [SerializeField] private EventReference fmodMeleeAttackEvent;
        [SerializeField] private EventReference fmodRangeAttackEvent;

        public void SetPlayerLinks(Transform playerTransform, MonoHealth playerHealth)
        {
            player = playerTransform;
            this.playerHealth = playerHealth;
        }
        private void Start()
        {
            if (phase == "melee") StartCoroutine(ChangePhase(timeToRangePhase));
            else if (phase == "range") StartCoroutine(ChangePhase(timeToMeleePhase));
        }
        public void Death()
        {
            PlayFmodOneShot(fmodDeathEvent, transform.position + Vector3.up);

            animsObj[2].SetActive(true);
            animsObj[2].GetComponent<Animator>().SetTrigger("Play");
            animsObj[2].transform.SetParent(transform.parent);
            Destroy(gameObject);
        }
        void PlayAnim(string animName)
        {
            switch (animName)
            {
                case "Idle":
                    animsObj[0].SetActive(true);

                    animsObj[1].SetActive(false);
                    animsObj[2].SetActive(false);
                    animsObj[3].SetActive(false);
                    animsObj[4].SetActive(false);
                    break;
                case "Run":
                    animsObj[1].SetActive(true);

                    animsObj[0].SetActive(false);
                    animsObj[2].SetActive(false);
                    animsObj[3].SetActive(false);
                    animsObj[4].SetActive(false);
                    break;
                case "Attack":
                    animsObj[3].SetActive(true);
                    animsObj[3].GetComponent<Animator>().SetTrigger("Play");

                    animsObj[0].SetActive(false);
                    animsObj[1].SetActive(false);
                    animsObj[2].SetActive(false);
                    animsObj[4].SetActive(false);
                    break;
                case "Proj":
                    animsObj[4].SetActive(true);
                    animsObj[4].GetComponent<Animator>().SetTrigger("Play");

                    animsObj[0].SetActive(false);
                    animsObj[1].SetActive(false);
                    animsObj[2].SetActive(false);
                    animsObj[3].SetActive(false);
                    break;
            }
        }
        public IEnumerator ChangePhase(float delay)
        {
            yield return new WaitForSeconds(delay);
            if (phase == "melee")
            {
                phase = "range";
                if (state != "meleeAttack") state = "idle";
                StartCoroutine(ChangePhase(timeToMeleePhase));
            }
            else if (phase == "range")
            {
                phase = "melee";
                if(state != "rangeAttack") state = "idle";
                StartCoroutine(ChangePhase(timeToRangePhase));
            }
        }
        private void FixedUpdate()
        {
            if (player == null)
            {
                FirstPersonController p = FindObjectOfType<FirstPersonController>();
                if(p!=null)SetPlayerLinks(p.transform, p.GetComponent<MonoHealth>());
            }
            else
            {
                playerVelocity = (player.position - lastPosition) / Time.deltaTime;
                lastPosition = player.position;
            }
            if (phase == "melee")
            {
                if (state == "rush" || state == "idle") Rush();
            }
            else if(phase == "range")
            {
                transform.LookAt(player.position, Vector3.up);
                if (state == "idle" && Vector3.Distance(transform.position, player.position) < 7)
                {
                    phase = "melee";
                    if (state != "rangeAttack") state = "idle";
                    StopAllCoroutines();
                    StartCoroutine(ChangePhase(timeToRangePhase));
                }
                else if (state == "idle")
                {
                    if (Random.Range(0f, 1f) < rotateChance) Rotate();
                    else StartCoroutine(RangeAttack());
                }
                else if (state == "chase" && Vector3.Distance(transform.position, player.position) < preferredDistanceToPlayer)
                {
                    if (isBossSeePlayer()) StartCoroutine(RangeAttack());
                }
                else if (agent.velocity.magnitude < 0.1f && (state == "rotate" || state == "chase")) StartCoroutine(RangeAttack());
                else if (state == "rotate" && Vector3.Distance(transform.position, player.position) < 15) StartCoroutine(RangeAttack());
            }
            if (state == "chase" || state == "rotate" || state == "rush") PlayAnim("Run");
            else if (state == "idle") PlayAnim("Idle");
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
            PlayAnim("Attack");
            PlayFmodOneShot(fmodMeleeAttackEvent, transform.position + Vector3.up * 0.5f);
            playerHealth.TrySet(-meleeDamage);
            if (player == null)
            {
                FirstPersonController p = FindObjectOfType<FirstPersonController>();
                SetPlayerLinks(p.transform, p.GetComponent<MonoHealth>());
            }
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
            Ray _ray = new Ray(ballisticSpawnPoint.position, player.position - ballisticSpawnPoint.position);
            Debug.DrawRay(ballisticSpawnPoint.position, player.position - ballisticSpawnPoint.position);
            Physics.Raycast(_ray, out RaycastHit _hit);
            //Debug.Log(_hit.collider.tag);
            if (_hit.distance > rangeAttackMaxDistance || !_hit.collider.CompareTag("Player"))
            {
                agent.isStopped = false;
                RangePlayerChasing();
                yield break;
            }
            state = "rangeAttack";
            if(isBallistic) BallisticAttack();
            else RayCastAttack();
            PlayFmodOneShot(fmodRangeAttackEvent, RangeAttackSoundPosition());
            PlayAnim("Proj");
            yield return new WaitForSeconds(rangeAttackTime);
            state = "idle";
            agent.isStopped = false;
        }
        private void BallisticAttack()
        {
            GameObject ball = Instantiate(ballisticPrefab, ballisticSpawnPoint);
            ball.transform.parent = transform.parent;
            //ballisticSpawnPoint.LookAt(player.position);
            float distanceToPlayer = Vector3.Distance(player.position + playerVelocity, ballisticSpawnPoint.position) / 20;
            Vector3 velocity = (player.position + playerVelocity * UpryazhdenieCoef * distanceToPlayer + Vector3.up * Mathf.Pow(Vector3.Distance(player.position + playerVelocity * UpryazhdenieCoef, ballisticSpawnPoint.position), 2) / 75 - ballisticSpawnPoint.position).normalized;
            //Debug.Log("!!!!!!!!!!!" + player.GetComponent<CharacterController>().);
            velocity = velocity * ballSpeed;
            ball.GetComponent<Rigidbody>().velocity = velocity;
            ball.GetComponent<Ball>().damage = rangeDamage;

        }
        private void RayCastAttack()
        {
            Ray _ray = new Ray(transform.position, player.position - transform.position);
            Debug.DrawRay(transform.position, player.position - transform.position);
            Vector3 shootDir = player.position + Vector3.up * Random.Range(-inaccuracy, +inaccuracy) + Vector3.right * Random.Range(-inaccuracy, +inaccuracy) - transform.position;
            Debug.DrawRay(transform.position, shootDir);
            Physics.Raycast(_ray, out RaycastHit hit);
            if (hit.collider.CompareTag("Player"))
            {
                playerHealth.TrySet(-rangeDamage);
            }
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

        private Vector3 RangeAttackSoundPosition()
        {
            if (ballisticSpawnPoint != null)
                return ballisticSpawnPoint.position;
            return transform.position + Vector3.up;
        }

        private static void PlayFmodOneShot(EventReference eventRef, Vector3 worldPosition)
        {
            if (eventRef.IsNull) return;
            if (GamePause.IsPaused) return;
            RuntimeManager.PlayOneShot(eventRef, worldPosition);
        }
    }
}

