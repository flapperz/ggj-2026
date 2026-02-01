using UnityEngine;
using UnityEngine.InputSystem; // New Input System

[RequireComponent(typeof(CharacterController))]
public class Player : MonoBehaviour
{
    public Renderer redmRend;
    public Renderer greenmRend;
    public GameObject greenMaskObject;

    [Header("Movement Settings")]
    public float jumpHeight = 3.0f;
    public float gravityValue = -9.81f;
    public float gravityMultiplier = 5.0f;
    public float DeathBarrier = 30f;

    [Header("Jump Settings")]
    public int maxJumps = 2;                 // 2 = double jump
    public float airJumpMultiplier = 0.85f;  // Second jump is slightly weaker
    private float centeringVelocity; // State variable for SmoothDamp
    public float centeringSmoothTime = 1f; // Adjust for "snappiness"

    private CharacterController controller;
    private Vector3 playerVelocity;
    private bool isGrounded;
    private int jumpsRemaining;
    private Renderer maskRend;
    private Animator anim;
    void Start()
    {
        GameManager.Instance.Score = 0;
        controller = GetComponent<CharacterController>();
        Transform maskTransform = transform.Find("Mask");

        if (maskTransform != null)
        {
            maskRend = maskTransform.GetComponent<Renderer>();
        }
        else
        {
            Debug.LogError("Mask object not found on Player!");
        }

        jumpsRemaining = maxJumps;
        anim = GetComponentInChildren<Animator>();

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
        GameManager.Instance.Score += Time.deltaTime * 10;
        ApplyGravity();
        ProcessMovement();
        UpdateAnimator();

        Rect deathBounds = new Rect(DeathBarrier / 2, DeathBarrier / 2, DeathBarrier, DeathBarrier);
        if (transform.position.x < -DeathBarrier ||
            transform.position.x > DeathBarrier ||
            transform.position.y < -DeathBarrier ||
            transform.position.y > DeathBarrier)
        {
            GameManager.Instance.TriggerGameOver();
        }
    }

    void UpdateMaterial()
    {
        if (GameManager.Instance != null)
        {
            Polarity p = GameManager.Instance.CurrentPolarity;
            if (redmRend != null && greenmRend != null)
            {
                redmRend.enabled = (p == Polarity.Angry);
                greenmRend.enabled = (p == Polarity.Happy);
            }
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
        // // 1. Re-centering Logic (Target X = 0)
        float currentX = transform.position.x;

        // We calculate a new X position that is closer to 0 using SmoothDamp
        float nextX = Mathf.SmoothDamp(currentX, 0f, ref centeringVelocity, centeringSmoothTime);

        // Convert that position change into a displacement (Move delta)
        float centeringDelta = nextX - currentX;


        // 2. Combine with Gravity/Jump
        // We create a move vector: [Centering Displacement] [Gravity/Jump Displacement]
        Vector3 moveDisplacement = new Vector3(centeringDelta, playerVelocity.y * Time.deltaTime, 0f);

        // Apply the movement
        controller.Move(moveDisplacement);
    }

    private void UpdateAnimator()
    {
        if (anim == null) return;
        anim.SetBool("isGrounded", isGrounded);
        int jumpCount = maxJumps - jumpsRemaining;
        if (isGrounded) jumpCount = 0;
        anim.SetInteger("jumpCount", jumpCount);
    }

    // -------- Input System --------


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
        GameManager.Instance.TriggerGameOver();
    }


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
