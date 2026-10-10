using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(PlayerInput))]
public class NetworkPlayerController : NetworkBehaviour
{
    public float moveSpeed = 5f;
    public float mouseSensitivity = 0.1f;
    public Transform cameraTransform;

    private CharacterController characterController;
    private PlayerInput playerInput;
    private Vector2 moveInput;
    private Vector2 lookInput;
    private float xRotation = 0f;

    private void Awake()
    {
        characterController = GetComponent<CharacterController>();
        playerInput = GetComponent<PlayerInput>();
    }

    public override void OnNetworkSpawn()
    {
        if (IsOwner)
        {
            playerInput.enabled = true;
            characterController.enabled = true;
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
        else
        {
            playerInput.enabled = false;
            // Hide remote player's camera so it doesn't override your screen
            if (cameraTransform != null) cameraTransform.gameObject.SetActive(false);
        }
    }

    private void Update()
    {
        if (!IsOwner) return;

        HandleLook();
        HandleMovement();
    }

    public void OnMove(InputValue value) => moveInput = value.Get<Vector2>();
    public void OnLook(InputValue value) => lookInput = value.Get<Vector2>();

    public void OnInteract(InputValue value)
    {
        if (value.isPressed && IsOwner) InteractServerRpc();
    }

    private void HandleLook()
    {
        float mouseX = lookInput.x * mouseSensitivity;
        float mouseY = lookInput.y * mouseSensitivity;

        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -80f, 80f);
        cameraTransform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
        transform.Rotate(Vector3.up * mouseX, Space.World);
    }

    private void HandleMovement()
    {
        Vector3 move = transform.right * moveInput.x + transform.forward * moveInput.y;
        characterController.Move(move.normalized * moveSpeed * Time.deltaTime);
    }

    [ServerRpc]
    private void InteractServerRpc()
    {
        Debug.Log("Server registered an interaction from a player!");
        // Add ticket counter / passenger logic here
    }
}