using UnityEngine;
using UnityEngine.InputSystem; // New Input System

[RequireComponent(typeof(CharacterController))]
public class Player : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 10.0f;
    public float jumpHeight = 3.0f;
    public float gravityValue = -9.81f;
    public float gravityMultiplier = 5.0f;
    public float DeathBarrier = -50f;

    [Header("Jump Settings")]
    public int maxJumps = 2;                 // 2 = double jump
    public float airJumpMultiplier = 0.85f;  // Second jump is slightly weaker

    private CharacterController controller;
    private Vector3 playerVelocity;
    private Vector2 moveInput;
    private bool isGrounded;
    private int jumpsRemaining;
    private Renderer rend;
    void Start()
    {
        controller = GetComponent<CharacterController>();
        rend = GetComponent<Renderer>();

        jumpsRemaining = maxJumps;

        GameManager.Instance.OnPolarityChanged += HandlePolarityChange;
        HandlePolarityChange(GameManager.Instance.CurrentPolarity);
    }

    private void OnDestroy()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnPolarityChanged -= HandlePolarityChange;
        }
    }

    private void HandlePolarityChange(Polarity newPolarity)
    {
        UpdateMaterial();
    }

    void Update()
    {
        ApplyGravity();
        ProcessMovement();

        // Check if Y position drops below DeathBarrier
    }

    void UpdateMaterial()
    {
        if (GameManager.Instance != null)
        {
            rend.material = GameManager.Instance.GetMaterial(
                GameManager.Instance.CurrentPolarity
            );
        }
    }

    private void ApplyGravity()
    {
        isGrounded = controller.isGrounded;

        if (isGrounded && playerVelocity.y < 0)
        {
            playerVelocity.y = -2f;     // Stick to ground
            jumpsRemaining = maxJumps; // Reset jumps
        }

        playerVelocity.y += gravityValue * gravityMultiplier * Time.deltaTime;
    }

    private void ProcessMovement()
    {
        // 2.5D movement: X axis only
        Vector3 move = new Vector3(moveInput.x, 0, 0);
        controller.Move(move * moveSpeed * Time.deltaTime);

        // Vertical movement (gravity + jump)
        controller.Move(playerVelocity * Time.deltaTime);
    }

    // -------- Input System --------

    public void OnMove(InputValue value)
    {
        moveInput = value.Get<Vector2>();
    }

    // public void OnJump(InputValue value)
    // {
    //     if (!value.isPressed) return;

    //     if (jumpsRemaining > 0)
    //     {
    //         float jumpMultiplier =
    //             (jumpsRemaining == maxJumps) ? 1f : airJumpMultiplier;

    //         playerVelocity.y = Mathf.Sqrt(
    //             jumpHeight * jumpMultiplier * -2f * (gravityValue * gravityMultiplier)
    //         );

    //         jumpsRemaining--;
    //     }
    // }

    public void TriggerMaskChange(Polarity targetPolarity)
    {
        // Simply set the polarity based on the input provided by the controller
        GameManager.Instance.SetPolarity(targetPolarity);
        Debug.Log($"[MaskChange] Switched to {targetPolarity}");
    }

    public void OnHit()
    {
        Debug.Log("Player was hit!");
        // Add damage logic here later (e.g., health--, knockback)
    }

    /// <summary>Inject move input from VR controller (bypasses PlayerInput).</summary>
    public void SetMoveInput(Vector2 input) { moveInput = input; }

    /// <summary>Trigger a jump from VR controller (bypasses PlayerInput).</summary>
    public void TriggerJump()
    {
        if (jumpsRemaining > 0)
        {
            float jumpMultiplier = (jumpsRemaining == maxJumps) ? 1f : airJumpMultiplier;
            playerVelocity.y = Mathf.Sqrt(jumpHeight * jumpMultiplier * -2f * (gravityValue * gravityMultiplier));
            jumpsRemaining--;
        }
    }

}
