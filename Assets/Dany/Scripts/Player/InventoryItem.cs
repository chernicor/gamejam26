using UnityEngine;
using FMODUnity;
namespace Dany
{
[CreateAssetMenu(fileName = "InventoryItem", menuName = "Scriptable Objects/InventoryItem")]
public class InventoryItem : ScriptableObject
{
    
    public string firePointPath = "FirePoint";
    public string itemName;
    public Sprite icon;
    public GameObject handModel;
    public bool isConsumable;
    public int maxStack = 1;

    public bool canShoot = false;
    public enum FireMode { Single, Automatic } 
    public FireMode fireMode = FireMode.Single;
    public float bulletSpeed = 50f;
    public int damage = 10;
    public float fireRate = 0.5f;
    public ParticleSystem muzzleEffect;
    
    [Header("Ammo (only for ranged weapons)")]
    public bool usesAmmo = false;
    public int magazineSize = 30;
    public int startingAmmoInMagazine = 30;
    public int reserveAmmoMax = 90;
    public int startingReserveAmmo = 90;
    public float reloadTime = 1.5f;
    
    [Header("Recoil (camera kick)")]
    public bool useRecoil = true;
    public float recoilKickUp = 2f;
    public float recoilKickSide = 0.6f;
    public float recoilReturnSpeed = 18f;
    public float recoilSnappiness = 22f;
   
    public enum WeaponType
    {
        Gan,
        Automat,
        Shotgun
      
    }
    public WeaponType weaponType;

    [Header("Audio (FMOD)")]
    public EventReference shootFmodEvent;
 
    public GameObject decalPrefab;


    public bool canThrow = false;
    public GameObject throwPrefab;
    public float throwForce = 20f;

    public float explosionRadius = 5f;
    public int explosionDamage = 50;
    public GameObject worldPickupPrefab;
}
}