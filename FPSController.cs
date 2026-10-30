
using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class FPSController : MonoBehaviour
{
    [Header("Movement")]
    public float walkSpeed = 5f;
    public float runSpeed = 9f;
    public float jumpHeight = 1.5f;
    public float gravity = -20f;

    [Header("Mouse")]
    public Transform playerCamera;
    public float mouseSensitivity = 2f;
    public float maxLookAngle = 85f;

    private CharacterController controller;
    private float verticalVelocity;
    private float cameraRotationX;

    private bool cursorLocked = true;

    void Start()
    {
        controller = GetComponent<CharacterController>();

        if (playerCamera == null)
            playerCamera = GetComponentInChildren<Camera>()?.transform;

        if (playerCamera == null)
        {
            Debug.LogError("Assign Player Camera!");
            enabled = false;
            return;
        }

        LockCursor(true);
    }

    void Update()
    {
        HandleCursor();

        if (!cursorLocked)
            return;

        HandleMouse();
        HandleMovement();
    }

    void HandleMouse()
    {
        float mouseX =
            Input.GetAxis("Mouse X") * mouseSensitivity;

        float mouseY =
            Input.GetAxis("Mouse Y") * mouseSensitivity;

        transform.Rotate(Vector3.up * mouseX);

        cameraRotationX -= mouseY;
        cameraRotationX = Mathf.Clamp(
            cameraRotationX,
            -maxLookAngle,
            maxLookAngle
        );

        playerCamera.localRotation =
            Quaternion.Euler(cameraRotationX, 0f, 0f);
    }

    void HandleMovement()
    {
        float x = Input.GetAxisRaw("Horizontal");
        float z = Input.GetAxisRaw("Vertical");

        Vector3 direction =
            transform.right * x +
            transform.forward * z;

        direction = Vector3.ClampMagnitude(direction, 1f);

        bool running =
            Input.GetKey(KeyCode.LeftShift);

        float speed = running ? runSpeed : walkSpeed;

        if (controller.isGrounded &&
            verticalVelocity < 0f)
        {
            verticalVelocity = -2f;
        }

        if (controller.isGrounded &&
            Input.GetKeyDown(KeyCode.Space))
        {
            verticalVelocity =
                Mathf.Sqrt(jumpHeight * -2f * gravity);
        }

        verticalVelocity += gravity * Time.deltaTime;

        Vector3 velocity =
            direction * speed +
            Vector3.up * verticalVelocity;

        controller.Move(velocity * Time.deltaTime);
    }

    void HandleCursor()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
            LockCursor(false);

        if (Input.GetMouseButtonDown(0))
            LockCursor(true);
    }

    void LockCursor(bool locked)
    {
        cursorLocked = locked;

        Cursor.lockState = locked
            ? CursorLockMode.Locked
            : CursorLockMode.None;

        Cursor.visible = !locked;
    }

    void OnDisable()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }
}
