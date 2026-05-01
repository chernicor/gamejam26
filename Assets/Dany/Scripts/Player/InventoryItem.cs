using UnityEngine;
namespace Dany
{
[CreateAssetMenu(fileName = "InventoryItem", menuName = "Scriptable Objects/InventoryItem")]
public class InventoryItem : ScriptableObject
{
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
   
    public enum WeaponType
    {
        Gan,
        Automat,
        Grende
      
    }
    public WeaponType weaponType;
    public AudioClip shootSound;
 
    public GameObject decalPrefab;


    public bool canThrow = false;
    public GameObject throwPrefab;
    public float throwForce = 20f;

    public float explosionRadius = 5f;
    public int explosionDamage = 50;
    public GameObject worldPickupPrefab;
}
}