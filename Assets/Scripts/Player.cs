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
        if (transform.position.y < DeathBarrier)
        {
            Respawn();
        }
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

    public void OnJump(InputValue value)
    {
        if (!value.isPressed) return;

        if (jumpsRemaining > 0)
        {
            float jumpMultiplier =
                (jumpsRemaining == maxJumps) ? 1f : airJumpMultiplier;

            playerVelocity.y = Mathf.Sqrt(
                jumpHeight * jumpMultiplier * -2f * (gravityValue * gravityMultiplier)
            );

            jumpsRemaining--;
        }
    }

    public void OnInteract(InputValue value)
    {
        if (!value.isPressed) return;

        Polarity newPolarity = GameManager.Instance.CurrentPolarity switch
        {
            Polarity.Neutral => Polarity.Happy,
            Polarity.Happy => Polarity.Angry,
            Polarity.Angry => Polarity.Neutral,
            _ => Polarity.Neutral
        };

        GameManager.Instance.SetPolarity(newPolarity);
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

    private void Respawn()
    {
        // 1. Find all objects with the tag "ResPoint"
        GameObject[] spawnPoints = GameObject.FindGameObjectsWithTag("ResPoint");

        Vector3 targetPosition = Vector3.zero; // Default fallback if no points exist

        if (spawnPoints.Length > 0)
        {
            GameObject nearestPoint = null;
            float minDistance = Mathf.Infinity;

            // 2. Iterate to find the closest one
            foreach (GameObject point in spawnPoints)
            {

                float distance = Mathf.Abs(point.transform.position.x - transform.position.x);
                if (point.transform.position.x > transform.position.x)
                {
                    continue; // Ignore points behind the player
                }
                if (distance < minDistance)
                {
                    minDistance = distance;
                    nearestPoint = point;
                }
            }
            targetPosition = nearestPoint.transform.position;
        }
        else
        {
            Debug.LogWarning("No objects tagged 'ResPoint' found! Respawning at 0,0,0.");
        }

        // 3. Move player (Disable controller briefly to force teleport)
        controller.enabled = false;
        transform.position = targetPosition;
        controller.enabled = true;

        // 4. Reset velocity
        playerVelocity = Vector3.zero;

        Debug.Log($"Player Respawned at {targetPosition}!");
    }
}
