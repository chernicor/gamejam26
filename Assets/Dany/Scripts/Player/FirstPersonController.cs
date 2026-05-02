using SiberianGJ26.YouAreDoing.Antos.Abstraction;
using SiberianGJ26.YouAreDoing.Antos.Singleton;
using SiberianGJ26.YouAreDoing.Antos.Modules;
using UnityEngine;

namespace Dany
{
    public class FirstPersonController : MonoBehaviour, IMonoUpdate
    {
        [SerializeField] private CharacterController controller;

        [field: SerializeField] public Camera PlayerCamera { get; private set; }
        [field: SerializeField] public Transform HandSocket { get; private set; }
        [field: SerializeField] public MonoHealth Health { get; private set; }
        [field: SerializeField] public PlayerAmmo PlayerAmmo { get; private set; }
        [field: SerializeField] public PlayerPickupInteractor PickupInteractor { get; private set; }

        [Header("Movement Settings")] public float walkSpeed = 5f;
        public float airSpeedMultiplier = 0.25f;
        public float aimSpeedMultiplier = 0.5f;
        public float jumpHeight = 1.5f;
        public float gravity = -9.81f;

        [Header("Camera Settings")] public float mouseSensitivity = 2f;
        public float minFov = 40f;
        public float maxFov = 60f;
        public float zoomSpeed = 10f;
        public Transform headBobTarget;

        [Header("Head Bob")] public bool enableHeadBob = true;
        public float bobFrequency = 10f;
        public float bobAmplitude = 0.05f;
        public float bobHorizontalAmplitude = 0.03f;
        public float bobSmooth = 12f;

        private Vector3 velocity;
        private bool isGrounded;
        private float currentFov;
        private bool isAiming;
        private Vector3 headBobDefaultLocalPos;
        private float bobTime;
        private float moveInputMagnitude;

        //Singleton
        private MonoUpdater _monoUpdater;

        private void OnEnable()
        {
            _monoUpdater?.Add(this);
        }

        private void OnDisable()
        {
            _monoUpdater?.Remove(this);
        }

        private void OnDrawGizmosSelected()
        {
            PickupInteractor?.OnDrawGizmosSelected();
        }

        public void Init(InventoryManager manager)
        {
            _monoUpdater = MonoUpdater.Instance;
            currentFov = maxFov;
            PlayerCamera.fieldOfView = currentFov;
            Cursor.lockState = CursorLockMode.Locked;

            if (headBobTarget == null && PlayerCamera != null) headBobTarget = PlayerCamera.transform;
            if (headBobTarget != null) headBobDefaultLocalPos = headBobTarget.localPosition;
            PickupInteractor.Init(transform, manager);
            Health.OnDeadEv += OnDead;
            _monoUpdater.Add(this);
        }

        public void OnUpdate()
        {
            HandleMovement();
            HandleJump();
            HandleCameraRotation();
            HandleAiming();
            HandleHeadBob();
            PickupInteractor.OnUpdate();
        }

        private void HandleMovement()
        {
            float moveX = Input.GetAxis("Horizontal"); // A/D
            float moveZ = Input.GetAxis("Vertical"); // W/S

            Vector3 move = transform.right * moveX + transform.forward * moveZ;
            moveInputMagnitude = Mathf.Clamp01(new Vector2(moveX, moveZ).magnitude);

            isGrounded = controller.isGrounded;
            if (isGrounded && velocity.y < 0)
            {
                velocity.y = -2f;
            }

            float currentSpeed = walkSpeed;
            if (!isGrounded) currentSpeed *= airSpeedMultiplier;
            if (isAiming) currentSpeed *= aimSpeedMultiplier;
            controller.Move(move * currentSpeed * Time.deltaTime);
        }

        private void HandleJump()
        {
            if (isGrounded && Input.GetKeyDown(KeyCode.Space))
            {
                velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
            }

            velocity.y += gravity * Time.deltaTime;
            controller.Move(velocity * Time.deltaTime);
        }

        private void HandleCameraRotation()
        {
            float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
            float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

            transform.Rotate(Vector3.up * mouseX);

            float rotationX = PlayerCamera.transform.localEulerAngles.x - mouseY;
            PlayerCamera.transform.localRotation = Quaternion.Euler(rotationX, 0f, 0f);
        }

        private void HandleAiming()
        {
            isAiming = Input.GetMouseButton(1);

            float targetFov = isAiming ? minFov : maxFov;
            currentFov = Mathf.Lerp(currentFov, targetFov, Time.deltaTime * zoomSpeed);
            PlayerCamera.fieldOfView = currentFov;
        }

        private void HandleHeadBob()
        {
            if (!enableHeadBob || headBobTarget == null) return;

            bool isMoving = moveInputMagnitude > 0.01f;
            float targetWeight = (isGrounded && isMoving) ? moveInputMagnitude : 0f;
            if (isAiming) targetWeight *= 0.35f;

            if (targetWeight > 0f)
            {
                bobTime += Time.deltaTime * bobFrequency * Mathf.Lerp(0.85f, 1.35f, targetWeight);

                float y = Mathf.Sin(bobTime) * bobAmplitude;
                float x = Mathf.Cos(bobTime * 0.5f) * bobHorizontalAmplitude;
                Vector3 offset = new Vector3(x, y, 0f) * targetWeight;

                Vector3 targetPos = headBobDefaultLocalPos + offset;
                headBobTarget.localPosition =
                    Vector3.Lerp(headBobTarget.localPosition, targetPos, Time.deltaTime * bobSmooth);
            }
            else
            {
                bobTime = 0f;
                headBobTarget.localPosition = Vector3.Lerp(headBobTarget.localPosition, headBobDefaultLocalPos,
                    Time.deltaTime * bobSmooth);
            }
        }

        private void OnDead()
        {
            Health.OnDeadEv -= OnDead;
            _monoUpdater?.Remove(this);
            Destroy(gameObject);
        }
    }
}