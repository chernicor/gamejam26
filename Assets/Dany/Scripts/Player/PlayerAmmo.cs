using UnityEngine;

namespace Dany
{
    public enum AmmoType
    {
        Pistol,
        Rifle,
        Shotgun
    }

    public class PlayerAmmo : MonoBehaviour
    {
        [Header("Ammo counts")]
        public int pistol;
        public int rifle;
        public int shotgun;

        public void Add(AmmoType type, int amount)
        {
            if (amount <= 0) return;

            switch (type)
            {
                case AmmoType.Pistol:
                    pistol += amount;
                    break;
                case AmmoType.Rifle:
                    rifle += amount;
                    break;
                case AmmoType.Shotgun:
                    shotgun += amount;
                    break;
            }
        }

        public bool TrySpend(AmmoType type, int amount)
        {
            if (amount <= 0) return true;

            switch (type)
            {
                case AmmoType.Pistol:
                    if (pistol < amount) return false;
                    pistol -= amount;
                    return true;
                case AmmoType.Rifle:
                    if (rifle < amount) return false;
                    rifle -= amount;
                    return true;
                case AmmoType.Shotgun:
                    if (shotgun < amount) return false;
                    shotgun -= amount;
                    return true;
            }

            return false;
        }
    }
}

