using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public sealed class FirstPersonController : MonoBehaviour
{
    [SerializeField] private Transform cameraPivot;
    [SerializeField, Min(0.1f)] private float walkSpeed = 4f;
    [SerializeField, Min(0.1f)] private float sprintSpeed = 6f;
    [SerializeField, Min(0.1f)] private float mouseSensitivity = 0.12f;
    [SerializeField, Min(0f)] private float jumpHeight = 2.2f;
    [SerializeField] private float gravity = -20f;

    private CharacterController controller;
    private float pitch;
    private float verticalSpeed;
    private bool controlsEnabled = true;
    private PlayerCheckpoint playerCheckpoint;

    // Pickups can use this to require a real jump instead of a walk-by.
    public bool IsJumping => controller != null && !controller.isGrounded && verticalSpeed > 0.1f;

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
        playerCheckpoint = GetComponent<PlayerCheckpoint>();
        if (cameraPivot == null && Camera.main != null)
            cameraPivot = Camera.main.transform;
    }

    private void OnEnable()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void Update()
    {
        if (!controlsEnabled || Keyboard.current == null || Mouse.current == null)
            return;

        if (Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        if (Mouse.current.leftButton.wasPressedThisFrame && Cursor.lockState != CursorLockMode.Locked)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        Look();
        Move();
    }

    public void SetControlsEnabled(bool enabled)
    {
        controlsEnabled = enabled;
    }

    public void ResetAfterRespawn()
    {
        verticalSpeed = 0f;
        pitch = 0f;
        if (cameraPivot != null)
            cameraPivot.localRotation = Quaternion.identity;
    }

    private void OnControllerColliderHit(ControllerColliderHit hit)
    {
        TouchHazard hazard = hit.collider.GetComponentInParent<TouchHazard>();
        if (hazard != null && playerCheckpoint != null)
            hazard.Hit(playerCheckpoint);
    }

    private void Look()
    {
        if (cameraPivot == null || Cursor.lockState != CursorLockMode.Locked)
            return;

        Vector2 delta = Mouse.current.delta.ReadValue() * mouseSensitivity;
        transform.Rotate(Vector3.up, delta.x, Space.Self);
        pitch = Mathf.Clamp(pitch - delta.y, -80f, 80f);
        cameraPivot.localRotation = Quaternion.Euler(pitch, 0f, 0f);
    }

    private void Move()
    {
        Vector2 input = Vector2.zero;
        if (Keyboard.current.wKey.isPressed || Keyboard.current.upArrowKey.isPressed) input.y += 1f;
        if (Keyboard.current.sKey.isPressed || Keyboard.current.downArrowKey.isPressed) input.y -= 1f;
        if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed) input.x += 1f;
        if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed) input.x -= 1f;
        input = Vector2.ClampMagnitude(input, 1f);

        if (controller.isGrounded && verticalSpeed < 0f)
            verticalSpeed = -2f;

        if (controller.isGrounded && Keyboard.current.spaceKey.wasPressedThisFrame)
            verticalSpeed = Mathf.Sqrt(jumpHeight * -2f * gravity);

        verticalSpeed += gravity * Time.deltaTime;
        float speed = Keyboard.current.leftShiftKey.isPressed ? sprintSpeed : walkSpeed;
        Vector3 planar = (transform.right * input.x + transform.forward * input.y) * speed;
        controller.Move((planar + Vector3.up * verticalSpeed) * Time.deltaTime);
    }
}
