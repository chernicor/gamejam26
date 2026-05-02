using System;
using System.Reflection;
using SiberianGJ26.YouAreDoing.Antos.Modules;
using UnityEngine;

namespace Dany
{
    /// <summary>
    /// Урон игроку (MonoHealth) и объектам с компонентом Dany.Health (TakeDamage).
    /// </summary>
    public static class EnemyDamage
    {
        private static Type s_healthType;
        private static MethodInfo s_takeDamage;
        private static bool s_healthLookupDone;

        private static void EnsureHealthType()
        {
            if (s_healthLookupDone) return;
            s_healthLookupDone = true;
            s_healthType = typeof(EnemyDamage).Assembly.GetType("Dany.Health");
            if (s_healthType != null)
                s_takeDamage = s_healthType.GetMethod("TakeDamage", BindingFlags.Instance | BindingFlags.Public, null, new[] { typeof(float) }, null);
        }

        /// <summary>Компонент с TakeDamage из Dany (если есть), иначе null.</summary>
        public static Component GetDestructibleDamageReceiver(GameObject fromObject)
        {
            if (fromObject == null) return null;
            EnsureHealthType();
            if (s_healthType == null) return null;
            return fromObject.GetComponentInParent(s_healthType);
        }

        public static void Apply(GameObject hitObject, float damage)
        {
            if (damage <= 0f || hitObject == null) return;

            var mh = hitObject.GetComponentInParent<MonoHealth>();
            if (mh != null && mh.IsAlive)
            {
                mh.TrySet(-damage);
                return;
            }

            EnsureHealthType();
            if (s_healthType == null || s_takeDamage == null) return;

            var health = hitObject.GetComponentInParent(s_healthType);
            if (health != null)
                s_takeDamage.Invoke(health, new object[] { damage });
        }

        public static void Apply(Collider collider, float damage)
        {
            if (collider == null) return;
            Apply(collider.gameObject, damage);
        }
    }
}
