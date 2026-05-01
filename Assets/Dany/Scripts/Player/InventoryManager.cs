using UnityEngine;
using FMODUnity;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Collections;
namespace Dany
{
public class InventoryManager : MonoBehaviour
{
    [Header("UI Settings")]
    public Transform inventoryPanel;
    public SlotUI[] slots = new SlotUI[2];
    public TextMeshProUGUI ammoText;

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
    
    private int[] ammoInMagazine = new int[2];
    private int[] reserveAmmo = new int[2];
    private bool[] isReloading = new bool[2];
    private InventoryItem[] ammoInitializedForItem = new InventoryItem[2];

    private bool isShowingHint = false;
    private string currentHintText = "";

    private PickupObject currentPickupObject;
    private AmmoPickup currentAmmoPickup;

    private float lastShotTime = 0f; 

    private GameObject currentMuzzleEffect;
    
    public Dictionary<InventoryItem.WeaponType, GameObject> decalPrefabs = new Dictionary<InventoryItem.WeaponType, GameObject>();

    public enum CameraMode { TopDown, FPS, TPS }
    public CameraMode cameraMode = CameraMode.FPS;
    public Transform playerTransform;
    
    private Vector3 recoilCurrent;
    private Vector3 recoilTarget;

    void Start()
    {
        if (playerCamera == null)
        {
            playerCamera = Camera.main;
        }

        for (int i = 0; i < 9; i++)
        {
            slotItems.Add(null);
            slotCounts.Add(0);
        }
        
        for (int i = 0; i < 2; i++)
        {
            ammoInMagazine[i] = 0;
            reserveAmmo[i] = 0;
            isReloading[i] = false;
        }

        UpdateUI();
        UpdateHand();
        UpdateAmmoUI();

        pickupHintText.SetActive(false);
        
    }

    void Update()
    {
        HandleInput();
        CheckForPickupObject();
        UpdateHint();
        UpdateRecoil();
        
        if (ammoText != null && !ammoText.gameObject.activeSelf)
        {
            ammoText.gameObject.SetActive(true);
        }
        UpdateAmmoUI();
    }
    
    void LateUpdate()
    {
        ApplyRecoilToCamera();
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
                currentAmmoPickup = null;
                currentHintText = $"Нажми E, чтобы подобрать {pickupObj.item.itemName}";
                return;
            }

            AmmoPickup ammoPickup = hit.collider.GetComponent<AmmoPickup>();
            if (ammoPickup != null)
            {
                canPickup = true;
                pickupItem = null;
                currentPickupObject = null;
                currentAmmoPickup = ammoPickup;
                currentHintText = ammoPickup.GetHintText();
                return;
            }
        }

        canPickup = false;
        pickupItem = null;
        currentPickupObject = null;
        currentAmmoPickup = null;
        currentHintText = "";
    }

    private void UpdateHint()
    {
        if (pickupHintText == null) return;

        bool shouldShow = canPickup && (pickupItem != null || currentAmmoPickup != null);

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

        var tmp = pickupHintText.GetComponent<TextMeshProUGUI>();
        if (tmp == null) tmp = pickupHintText.GetComponentInChildren<TextMeshProUGUI>();
        if (tmp != null)
        {
            tmp.text = currentHintText;
            var c = tmp.color;
            c.a = Mathf.Lerp(c.a, targetAlpha, Time.deltaTime * hintFadeSpeed);
            tmp.color = c;
        }
    }

    // Обработка ввода
    private void HandleInput()
    {
        // Подбор по E
        if (Input.GetKeyDown(KeyCode.E) && canPickup)
        {
            if (pickupItem != null)
            {
                PickupItem(pickupItem);
            }
            else if (currentAmmoPickup != null)
            {
                PickupAmmo(currentAmmoPickup);
            }
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
        
        // Перезарядка по R
        if (Input.GetKeyDown(KeyCode.R) && slotItems[selectedSlot] != null && slotItems[selectedSlot].canShoot)
        {
            Reload(selectedSlot);
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
            UpdateAmmoUI();
            Debug.Log($"Выбран слот {selectedSlot + 1} колесом");
        }
        else if (Input.GetAxis("Mouse ScrollWheel") < 0)
        {
            selectedSlot = (selectedSlot - 1 + 2) % 2;
            UpdateHand();
            UpdateUI();
            UpdateAmmoUI();
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
                UpdateAmmoUI();
                Debug.Log($"Выбран слот {i + 1} клавишей");
                break;
            }
        }
    }

    // Подбор предмета
    public void PickupItem(InventoryItem item)
    {
        if (item == null)
        {
            Debug.LogWarning("PickupItem вызван с null item");
            return;
        }

        // Ограничение: оружие (стреляющее) можно иметь только 2 штуки (2 слота).
        // Поэтому оружие кладём только в слоты 0-1.
        bool isWeapon = item.canShoot;

        bool placed = false;
        int maxSlot = isWeapon ? 2 : 9;
        for (int i = 0; i < maxSlot; i++)
        {
            if (slotItems[i] == null || (slotItems[i] == item && !item.isConsumable && slotCounts[i] < item.maxStack))
            {
                slotItems[i] = item;
                slotCounts[i] = (slotItems[i] == item && slotCounts[i] > 0) ? slotCounts[i] + 1 : 1;
                placed = true;
                
                if (i < 2)
                {
                    InitAmmoForSlot(i, item);
                }

                UpdateUI();
                UpdateHand();
                UpdateAmmoUI();
                Debug.Log($"Подобран {item.itemName}! Слот {i}: {slotCounts[i]} шт.");
                break;
            }
        }

        if (!placed)
        {
            if (isWeapon)
                Debug.Log("У вас уже есть два оружия. Нельзя подобрать третье!");
            else
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

    public void PickupAmmo(AmmoPickup ammoPickup)
    {
        if (ammoPickup == null) return;

        bool applied = false;
        for (int i = 0; i < 2; i++)
        {
            InventoryItem weapon = slotItems[i];
            if (weapon == null) continue;
            if (!weapon.usesAmmo) continue;
            if (weapon.weaponType != ammoPickup.weaponType) continue;

            int max = Mathf.Max(0, weapon.reserveAmmoMax);
            reserveAmmo[i] = Mathf.Clamp(reserveAmmo[i] + Mathf.Max(0, ammoPickup.amount), 0, max);
            applied = true;
        }

        if (!applied)
        {
            Debug.Log($"Нет оружия подходящего типа для патронов ({ammoPickup.weaponType}).");
            return;
        }

        UpdateAmmoUI();
        Destroy(ammoPickup.gameObject);
        currentAmmoPickup = null;
    }

    private void DropItem(int slotIndex)
    {
        if (slotItems[slotIndex] != null)
        {
            string itemName = slotItems[slotIndex].itemName;
            slotItems[slotIndex] = null;
            slotCounts[slotIndex] = 0;
            if (slotIndex < 2)
            {
                ammoInMagazine[slotIndex] = 0;
                reserveAmmo[slotIndex] = 0;
                ammoInitializedForItem[slotIndex] = null;
            }
            UpdateUI();
            UpdateHand();
            UpdateAmmoUI();
            Debug.Log($"Выброшен {itemName} из слота {slotIndex}");
        }
    }
 
    private void Shoot(int slotIndex)
    {
        InventoryItem weapon = slotItems[slotIndex];
        if (weapon == null || !weapon.canShoot) return;
        if (weapon.usesAmmo && isReloading[slotIndex]) return;
        
        if (weapon.usesAmmo)
        {
            if (ammoInMagazine[slotIndex] <= 0)
            {
                Reload(slotIndex);
                return;
            }
            
            ammoInMagazine[slotIndex]--;
            UpdateAmmoUI();
        }

        lastShotTime = Time.time;

        Transform firePoint = GetFirePointForCurrentHandItem(weapon);
        Vector3 muzzlePosition = firePoint != null ? firePoint.position : handSocket.position;
        Quaternion muzzleRotation = firePoint != null ? firePoint.rotation : handSocket.rotation;

        if (weapon.muzzleEffect != null)
        {
            if (currentMuzzleEffect != null) Destroy(currentMuzzleEffect);
            currentMuzzleEffect = Instantiate(weapon.muzzleEffect.gameObject, muzzlePosition, muzzleRotation);
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

        if (!weapon.shootFmodEvent.IsNull)
        {
            RuntimeManager.PlayOneShot(weapon.shootFmodEvent, muzzlePosition);
        }
        
        ApplyRecoil(weapon);

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

        if (weapon.usesAmmo)
        {
            Debug.Log($"Выстрел из {weapon.itemName}! Патроны: {ammoInMagazine[slotIndex]}/{weapon.magazineSize} | Запас: {reserveAmmo[slotIndex]}");
        }
        else
        {
            Debug.Log($"Выстрел из {weapon.itemName}! Урон: {weapon.damage}, Скорострельность: {weapon.fireRate}");
        }
    }

    private void InitAmmoForSlot(int slotIndex, InventoryItem item)
    {
        if (item == null || !item.usesAmmo)
        {
            ammoInMagazine[slotIndex] = 0;
            reserveAmmo[slotIndex] = 0;
            ammoInitializedForItem[slotIndex] = null;
            UpdateAmmoUI();
            return;
        }
        
        ammoInMagazine[slotIndex] = Mathf.Clamp(item.startingAmmoInMagazine, 0, item.magazineSize);
        reserveAmmo[slotIndex] = Mathf.Clamp(item.startingReserveAmmo, 0, item.reserveAmmoMax);
        ammoInitializedForItem[slotIndex] = item;
        UpdateAmmoUI();
    }

    private void Reload(int slotIndex)
    {
        InventoryItem weapon = slotItems[slotIndex];
        if (weapon == null || !weapon.canShoot || !weapon.usesAmmo) return;
        if (isReloading[slotIndex]) return;
        if (ammoInMagazine[slotIndex] >= weapon.magazineSize) return;
        if (reserveAmmo[slotIndex] <= 0)
        {
            Debug.Log("Нет патронов.");
            return;
        }
        
        StartCoroutine(ReloadRoutine(slotIndex, weapon));
    }
    
    private void ApplyRecoil(InventoryItem weapon)
    {
        if (weapon == null || !weapon.useRecoil) return;
        if (playerCamera == null) return;
        
        float side = Random.Range(-weapon.recoilKickSide, weapon.recoilKickSide);
        recoilTarget += new Vector3(-weapon.recoilKickUp, side, 0f);
    }
    
    private void UpdateRecoil()
    {
        InventoryItem weapon = slotItems[selectedSlot];
        if (weapon == null || !weapon.canShoot || !weapon.useRecoil || playerCamera == null)
        {
            recoilTarget = Vector3.Lerp(recoilTarget, Vector3.zero, Time.deltaTime * 20f);
            recoilCurrent = Vector3.Lerp(recoilCurrent, Vector3.zero, Time.deltaTime * 20f);
            return;
        }
        
        recoilTarget = Vector3.Lerp(recoilTarget, Vector3.zero, Time.deltaTime * weapon.recoilReturnSpeed);
        recoilCurrent = Vector3.Slerp(recoilCurrent, recoilTarget, Time.deltaTime * weapon.recoilSnappiness);
    }
    
    private void ApplyRecoilToCamera()
    {
        if (playerCamera == null) return;
        
        InventoryItem weapon = slotItems[selectedSlot];
        if (weapon == null || !weapon.canShoot || !weapon.useRecoil) return;
        
        // Добавляем отдачу поверх текущего поворота камеры (mouse look).
        // Это не ломает управление вверх/вниз, потому что не меняем иерархию.
        playerCamera.transform.localRotation = playerCamera.transform.localRotation * Quaternion.Euler(recoilCurrent);
    }
    
    private IEnumerator ReloadRoutine(int slotIndex, InventoryItem weapon)
    {
        isReloading[slotIndex] = true;
        UpdateAmmoUI();
        
        float t = Mathf.Max(0f, weapon.reloadTime);
        if (t > 0f) yield return new WaitForSeconds(t);
        
        // оружие могло смениться во время ожидания
        if (slotItems[slotIndex] != weapon)
        {
            isReloading[slotIndex] = false;
            UpdateAmmoUI();
            yield break;
        }
        
        int need = weapon.magazineSize - ammoInMagazine[slotIndex];
        if (need <= 0)
        {
            isReloading[slotIndex] = false;
            UpdateAmmoUI();
            yield break;
        }
        if (reserveAmmo[slotIndex] <= 0) { isReloading[slotIndex] = false; UpdateAmmoUI(); yield break; }
        
        int take = Mathf.Min(need, reserveAmmo[slotIndex]);
        reserveAmmo[slotIndex] -= take;
        ammoInMagazine[slotIndex] += take;
        isReloading[slotIndex] = false;
        UpdateAmmoUI();
        
        Debug.Log($"Перезарядка: {ammoInMagazine[slotIndex]}/{weapon.magazineSize} | Запас: {reserveAmmo[slotIndex]}");
    }
    
    private void UpdateAmmoUI()
    {
        if (ammoText == null) return;
        if (!ammoText.gameObject.activeSelf) ammoText.gameObject.SetActive(true);
        
        InventoryItem weapon = slotItems[selectedSlot];
        if (weapon == null || !weapon.canShoot || !weapon.usesAmmo)
        {
            ammoText.text = "";
            return;
        }

        if (isReloading[selectedSlot])
        {
            ammoText.text = $"Перезарядка... {reserveAmmo[selectedSlot]} | {ammoInMagazine[selectedSlot]}";
        }
        else
        {
            ammoText.text = $"{reserveAmmo[selectedSlot]} | {ammoInMagazine[selectedSlot]}";
        }
    }

    private Transform GetFirePointForCurrentHandItem(InventoryItem weapon)
    {
        if (weapon == null || currentHandItem == null) return null;
        if (string.IsNullOrWhiteSpace(weapon.firePointPath)) return null;
        
        Transform t = currentHandItem.transform.Find(weapon.firePointPath);
        return t;
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
            
            // Инициализируем стартовые патроны ТОЛЬКО при первом появлении оружия в слоте
            // (или если предмет в слоте сменился). Не переинициализируем по условию "0/0",
            // иначе при переключении оружия можно случайно вернуть "заводские" значения.
            if (slotItems[selectedSlot].usesAmmo && ammoInitializedForItem[selectedSlot] != slotItems[selectedSlot])
            {
                InitAmmoForSlot(selectedSlot, slotItems[selectedSlot]);
            }
        }
        else
        {
            Debug.Log($"Нет предмета в слоте {selectedSlot} или handModel не задан в ScriptableObject.");
        }
        
        UpdateAmmoUI();
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