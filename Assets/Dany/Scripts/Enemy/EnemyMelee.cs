using UnityEngine;

namespace Dany
{
    /// <summary>
    /// Подходит к игроку и наносит урон в радиусе ближнего боя.
    /// </summary>
    public class EnemyMelee : EnemyBase
    {
        [Header("Melee")]
        [SerializeField] private float attackRange = 1.5f;
        [SerializeField] private float attackDamage = 15f;
        [SerializeField] private float attackCooldown = 1.1f;

        private float _nextAttackTime;

        private void Update()
        {
            RefreshTarget();
            if (Target == null) return;

            if (MoveTowardsPlayer(attackRange, Time.deltaTime))
            {
                if (Time.time >= _nextAttackTime)
                {
                    float dist = HorizontalDistance(transform.position, Target.position);
                    if (dist <= attackRange)
                    {
                        EnemyDamage.Apply(Target.gameObject, attackDamage);
                        _nextAttackTime = Time.time + attackCooldown;
                    }
                }
            }
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(1f, 0.4f, 0.2f, 0.6f);
            Gizmos.DrawWireSphere(transform.position + Vector3.up * 0.5f, attackRange);
        }
    }
}
