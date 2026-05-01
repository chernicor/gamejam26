using System;
using UnityEngine;
namespace Dany
{
public class FirstPersonController : MonoBehaviour
{
    [Header("Movement Settings")]
    public float walkSpeed = 5f;
    public float airSpeedMultiplier = 0.25f;
    public float aimSpeedMultiplier = 0.5f;
    public float jumpHeight = 1.5f;
    public float gravity = -9.81f;

    [Header("Camera Settings")]
    public float mouseSensitivity = 2f; 
    public float minFov = 40f;
    public float maxFov = 60f;
    public float zoomSpeed = 10f;
    public Transform headBobTarget;
    
    [Header("Head Bob")]
    public bool enableHeadBob = true;
    public float bobFrequency = 10f;
    public float bobAmplitude = 0.05f;
    public float bobHorizontalAmplitude = 0.03f;
    public float bobSmooth = 12f;

    private CharacterController controller;
    private Camera playerCamera;
    private Vector3 velocity;
    private bool isGrounded;
    private float currentFov;
    private bool isAiming;
    private Vector3 headBobDefaultLocalPos;
    private float bobTime;
    private float moveInputMagnitude;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        playerCamera = GetComponentInChildren<Camera>();
        currentFov = maxFov;
        playerCamera.fieldOfView = currentFov;
        Cursor.lockState = CursorLockMode.Locked;
        
        if (headBobTarget == null && playerCamera != null) headBobTarget = playerCamera.transform;
        if (headBobTarget != null) headBobDefaultLocalPos = headBobTarget.localPosition;
    }

    void Update()
    {
        HandleMovement();
        HandleJump();
        HandleCameraRotation();
        HandleAiming();
        HandleHeadBob();
    }

    void HandleMovement()
    {
        float moveX = Input.GetAxis("Horizontal"); // A/D
        float moveZ = Input.GetAxis("Vertical");   // W/S

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

    void HandleJump()
    {
        if (isGrounded && Input.GetKeyDown(KeyCode.Space))
        {
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
        }

        velocity.y += gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);
    }

    void HandleCameraRotation()
    {
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

        transform.Rotate(Vector3.up * mouseX);

        float rotationX = playerCamera.transform.localEulerAngles.x - mouseY;
        playerCamera.transform.localRotation = Quaternion.Euler(rotationX, 0f, 0f);
    }

    void HandleAiming()
    {
        isAiming = Input.GetMouseButton(1);

        float targetFov = isAiming ? minFov : maxFov;
        currentFov = Mathf.Lerp(currentFov, targetFov, Time.deltaTime * zoomSpeed);
        playerCamera.fieldOfView = currentFov;

    }
    
    void HandleHeadBob()
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
            headBobTarget.localPosition = Vector3.Lerp(headBobTarget.localPosition, targetPos, Time.deltaTime * bobSmooth);
        }
        else
        {
            bobTime = 0f;
            headBobTarget.localPosition = Vector3.Lerp(headBobTarget.localPosition, headBobDefaultLocalPos, Time.deltaTime * bobSmooth);
        }
    }
}
}