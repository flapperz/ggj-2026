using UnityEngine;
using UnityEngine.InputSystem; // Required for the New Input System

[RequireComponent(typeof(CharacterController))]
public class Player : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 5.0f;
    public float jumpHeight = 2.0f;
    public float gravityValue = -9.81f;
    public float gravityMultiplier = 2.0f; // Makes falling feel snappier

    [Header("Polarity Settings")]
    public Polarity currentPolarity = Polarity.Neutral;

    private CharacterController controller;
    private Vector3 playerVelocity;
    private Vector2 moveInput;
    private bool isGrounded;

    void Start()
    {
        controller = GetComponent<CharacterController>();
    }

    void Update()
    {
        ApplyGravity();
        ProcessMovement();
    }

    private void ApplyGravity()
    {
        // CharacterController.isGrounded is more reliable than custom raycasts for simple shapes
        isGrounded = controller.isGrounded;

        // Stop applying gravity accumulation if we are on the ground
        if (isGrounded && playerVelocity.y < 0)
        {
            playerVelocity.y = -2f; // Small negative value ensures we stay "stuck" to the ground
        }

        // Apply Gravity over time
        playerVelocity.y += gravityValue * gravityMultiplier * Time.deltaTime;
    }

    private void ProcessMovement()
    {
        // 1. Calculate X-axis movement based on Input
        // We zero out Z to keep it strictly 2.5D
        Vector3 move = new Vector3(moveInput.x, 0, 0);
        Debug.Log("Move Input: " + moveInput);
        
        // Move the controller based on speed
        controller.Move(move * moveSpeed * Time.deltaTime);

        // 2. Apply the vertical velocity (Gravity + Jump)
        controller.Move(playerVelocity * Time.deltaTime);
    }

    // --- Input System Events (Send Messages) ---

    // Linked to the "Move" Action (typically WASD or Left Stick)
    public void OnMove(InputValue value)
    {
        moveInput = value.Get<Vector2>();
    }

    // Linked to the "Jump" Action (typically Space or South Button)
    public void OnJump(InputValue value)
    {
        if (value.isPressed && isGrounded)
        {
            // Physics formula for jump velocity: v = sqrt(h * -2 * g)
            playerVelocity.y = Mathf.Sqrt(jumpHeight * -2f * (gravityValue * gravityMultiplier));
        }
    }
}