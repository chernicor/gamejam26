using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
namespace Dany
{
public class InventoryManager : MonoBehaviour
{
    [Header("UI Settings")]
    public Transform inventoryPanel;
    public SlotUI[] slots = new SlotUI[2];

    [Header("Player Hand")]
    public Transform handSocket; 
    private GameObject currentHandItem;

    [Header("Pickup Settings")]
    public InventoryItem pickupItem;
    public bool canPickup = false;

    [Header("Hint Settings")]
    public Camera playerCamera; 
    public GameObject pickupHintText;
    public float pickupRange = 3f;
    public float hintFadeSpeed = 2f;

    private int selectedSlot = 0;
   
    private List<InventoryItem> slotItems = new List<InventoryItem>(9);
    private List<int> slotCounts = new List<int>(9);

    private bool isShowingHint = false;
    private string currentHintText = "";

    private PickupObject currentPickupObject;

    private float lastShotTime = 0f; 

    private AudioSource audioSource;
    private GameObject currentMuzzleEffect;
    
    public Dictionary<InventoryItem.WeaponType, GameObject> decalPrefabs = new Dictionary<InventoryItem.WeaponType, GameObject>();

    public enum CameraMode { TopDown, FPS, TPS }
    public CameraMode cameraMode = CameraMode.FPS;
    public Transform playerTransform;

    void Start()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();

        if (playerCamera == null)
        {
            playerCamera = Camera.main;
        }

        for (int i = 0; i < 9; i++)
        {
            slotItems.Add(null);
            slotCounts.Add(0);
        }

        UpdateUI();
        UpdateHand();

        pickupHintText.SetActive(false);
    }

    void Update()
    {
        HandleInput();
        CheckForPickupObject();
        UpdateHint();
    }

    // Raycast для обнаружения подобранных предметов
    private void CheckForPickupObject()
    {
        if (playerCamera == null) return;

        Ray ray = playerCamera.ScreenPointToRay(new Vector3(Screen.width / 2, Screen.height / 2, 0));
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, pickupRange))
        {
            PickupObject pickupObj = hit.collider.GetComponent<PickupObject>();
            if (pickupObj != null && pickupObj.item != null)
            {
                canPickup = true;
                pickupItem = pickupObj.item;
                currentPickupObject = pickupObj;
                currentHintText = $"Нажми E, чтобы подобрать {pickupObj.item.itemName}";
                return;
            }
        }

        canPickup = false;
        pickupItem = null;
        currentPickupObject = null;
        currentHintText = "";
    }

    private void UpdateHint()
    {
        if (pickupHintText == null) return;

        bool shouldShow = canPickup && pickupItem != null;

        if (shouldShow && !isShowingHint)
        {
            pickupHintText.SetActive(true);
            isShowingHint = true;
        }
        else if (!shouldShow && isShowingHint)
        {
            pickupHintText.SetActive(false);
            isShowingHint = false;
        }

        float targetAlpha = isShowingHint ? 1f : 0f;
    }

    // Обработка ввода
    private void HandleInput()
    {
        // Подбор по E
        if (Input.GetKeyDown(KeyCode.E) && canPickup && pickupItem != null)
        {
            PickupItem(pickupItem);
        }

        // Выброс по X
        if (Input.GetKeyDown(KeyCode.X) && slotItems[selectedSlot] != null)
        {
            DropItem(selectedSlot);
        }

        // Стрельба по LMB
        if (slotItems[selectedSlot] != null && slotItems[selectedSlot].canShoot)
        {
            InventoryItem weapon = slotItems[selectedSlot];
            bool isFiring = Input.GetMouseButton(0);

            if (weapon.fireMode == InventoryItem.FireMode.Single && Input.GetMouseButtonDown(0))
            {
                Shoot(selectedSlot);
            }
            else if (weapon.fireMode == InventoryItem.FireMode.Automatic && isFiring)
            {
                if (Time.time - lastShotTime >= weapon.fireRate)
                {
                    Shoot(selectedSlot);
                }
            }
        }

        // Бросок гранаты по G (только если предмет canThrow)
        if (Input.GetKeyDown(KeyCode.G) && slotItems[selectedSlot] != null && slotItems[selectedSlot].canThrow)
        {
            Throw(selectedSlot);
        }

        // Смена слота: колесо мыши
        if (Input.GetAxis("Mouse ScrollWheel") > 0)
        {
            selectedSlot = (selectedSlot + 1) % 2;
            UpdateHand();
            UpdateUI();
            Debug.Log($"Выбран слот {selectedSlot + 1} колесом");
        }
        else if (Input.GetAxis("Mouse ScrollWheel") < 0)
        {
            selectedSlot = (selectedSlot - 1 + 2) % 2;
            UpdateHand();
            UpdateUI();
            Debug.Log($"Выбран слот {selectedSlot + 1} колесом");
        }

        // Смена слота: клавиши 1-9
        for (int i = 0; i < 2; i++)
        {
            if (Input.GetKeyDown(KeyCode.Alpha1 + i))
            {
                selectedSlot = i;
                UpdateHand();
                UpdateUI();
                Debug.Log($"Выбран слот {i + 1} клавишей");
                break;
            }
        }
    }

    // Подбор предмета
    public void PickupItem(InventoryItem item)
    {
        bool placed = false;
        for (int i = 0; i < 9; i++)
        {
            if (slotItems[i] == null || (slotItems[i] == item && !item.isConsumable && slotCounts[i] < item.maxStack))
            {
                slotItems[i] = item;
                slotCounts[i] = (slotItems[i] == item && slotCounts[i] > 0) ? slotCounts[i] + 1 : 1;
                placed = true;

                UpdateUI();
                UpdateHand();
                Debug.Log($"Подобран {item.itemName}! Слот {i}: {slotCounts[i]} шт.");
                break;
            }
        }

        if (!placed)
        {
            Debug.Log("Инвентарь полон!");
        }
        else
        {
            if (currentPickupObject != null)
            {
                Destroy(currentPickupObject.gameObject);
                currentPickupObject = null;
                Debug.Log("Объект на сцене уничтожен!");
            }
        }
    }

    private void DropItem(int slotIndex)
    {
        if (slotItems[slotIndex] != null)
        {
            string itemName = slotItems[slotIndex].itemName;
            slotItems[slotIndex] = null;
            slotCounts[slotIndex] = 0;
            UpdateUI();
            UpdateHand();
            Debug.Log($"Выброшен {itemName} из слота {slotIndex}");
        }
    }
 
    private void Shoot(int slotIndex)
    {
        InventoryItem weapon = slotItems[slotIndex];
        if (weapon == null || !weapon.canShoot) return;

        lastShotTime = Time.time;

        if (weapon.muzzleEffect != null)
        {
            if (currentMuzzleEffect != null) Destroy(currentMuzzleEffect);
            currentMuzzleEffect = Instantiate(weapon.muzzleEffect.gameObject, handSocket.position, handSocket.rotation);
            ParticleSystem ps = currentMuzzleEffect.GetComponent<ParticleSystem>();
            if (ps != null)
            {
                if (weapon.fireMode == InventoryItem.FireMode.Automatic)
                {
                    ps.Play();
                }
                else
                {
                    ps.Emit(1);
                }
                Destroy(currentMuzzleEffect, ps.main.duration);
            }
        }

        if (weapon.shootSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(weapon.shootSound);
        }

        Vector3 rayOrigin = playerCamera.transform.position;
        Vector3 rayDirection = playerCamera.transform.forward;
        RaycastHit hit;
        if (Physics.Raycast(rayOrigin, rayDirection, out hit, Mathf.Infinity))
        {
            // Урон
            Health health = hit.collider.GetComponent<Health>();
            if (health != null)
            {
                health.TakeDamage(weapon.damage);
            }

            if (weapon.decalPrefab != null)
            {
                GameObject decal = Instantiate(weapon.decalPrefab, hit.point + hit.normal * 0.01f, Quaternion.LookRotation(hit.normal));
                decal.AddComponent<Decal>(); 
                Debug.Log($"Декаль для {weapon.weaponType} создана на {hit.collider.name}!");
            }
            else
            {
                Debug.LogWarning($"Декаль для оружия {weapon.itemName} (тип {weapon.weaponType}) не задана в InventoryItem!");
            }

            Debug.Log($"Попадание в {hit.collider.name} на расстоянии {hit.distance}. Урон: {weapon.damage}");
        }

        Debug.Log($"Выстрел из {weapon.itemName}! Урон: {weapon.damage}, Скорострельность: {weapon.fireRate}");
    }

    public void Throw(int slotIndex)
    {
        InventoryItem item = slotItems[slotIndex];
        if (item == null || !item.canThrow || item.throwPrefab == null) return;

        GameObject grenade = Instantiate(item.throwPrefab, playerTransform.position + Vector3.up * 1f, Quaternion.identity);
        Rigidbody rb = grenade.GetComponent<Rigidbody>();
        if (rb != null)
        {
            Vector3 throwDirection = playerCamera.transform.forward;
            float angle = (cameraMode == CameraMode.TopDown) ? 45f : 15f;
            throwDirection = Quaternion.Euler(angle, 0, 0) * throwDirection;
            rb.velocity = throwDirection.normalized * item.throwForce;
        }
        else
        {
            Debug.LogError("Rigidbody не найден на гранате! Проверь префаб.");
        }

        Grenade grenadeScript = grenade.GetComponent<Grenade>();
        if (grenadeScript != null)
        {
            grenadeScript.StartTimer();
            Debug.Log("StartTimer() вызван на гранате.");
        }
        else
        {
            Debug.LogError("Скрипт Grenade не найден на гранате! Проверь префаб.");
        }

        if (item.isConsumable)
        {
            slotCounts[slotIndex]--;
            if (slotCounts[slotIndex] <= 0)
            {
                slotItems[slotIndex] = null;
            }
            UpdateUI();
            UpdateHand();
        }

        Debug.Log($"Брошена {item.itemName} с запущенным таймером!");
    }

    private void UpdateUI()
    {
        for (int i = 0; i < 2; i++)
        {
            if (slots[i] != null)
            {
                slots[i].UpdateSlot(slotItems[i], slotCounts[i]);
            }
        }
        HighlightSelectedSlot();
    }

    private void HighlightSelectedSlot()
    {
        for (int i = 0; i < 2; i++)
        {
            if (slots[i] != null)
            {
                slots[i].SetSelected(i == selectedSlot);
            }
        }
    }

    private void UpdateHand()
    {
        // Уничтожить предыдущий
        if (currentHandItem != null)
        {
            Destroy(currentHandItem);
        }

        if (handSocket == null)
        {
            Debug.LogError("Hand Socket не присвоен! Присвой Transform в руке игрока в инспекторе InventoryManager.");
            return;
        }

        if (slotItems[selectedSlot] != null && slotCounts[selectedSlot] > 0 && slotItems[selectedSlot].handModel != null)
        {
            currentHandItem = Instantiate(slotItems[selectedSlot].handModel, handSocket.position, handSocket.rotation, handSocket);
            Debug.Log($"Модель {slotItems[selectedSlot].itemName} отображена в руке (слот {selectedSlot})");
        }
        else
        {
            Debug.Log($"Нет предмета в слоте {selectedSlot} или handModel не задан в ScriptableObject.");
        }
    }

    public bool HasFreeSlot()
    {
        for (int i = 0; i < 2; i++)
        {
            if (slotItems[i] == null) return true;
        }
        return false;
    }
}
}