using UnityEngine;
using UnityEngine.InputSystem; 

[RequireComponent(typeof(CharacterController))]
public class Player : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 5.0f;
    public float jumpHeight = 2.0f;
    public float gravityValue = -9.81f;
    public float gravityMultiplier = 2.0f; 

    [Header("Jump Settings")]
    public int maxJumps = 2; // Set to 2 for double jump, 3 for triple, etc.
    private int jumpsPerformed = 0; // Track how many jumps we've done

    [Header("Polarity Settings")]
    private CharacterController controller;
    private Vector3 playerVelocity;
    private Vector2 moveInput;
    private bool isGrounded;
    private Renderer rend;
    
    void Start()
    {
        controller = GetComponent<CharacterController>();
        rend = GetComponent<Renderer>();

        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnPolarityChanged += HandlePolarityChange;
            HandlePolarityChange(GameManager.Instance.CurrentPolarity);
        }
        
        UpdateMaterial();
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
    }
    
    void UpdateMaterial()
    {
        if (GameManager.Instance != null) 
        {
            rend.material = GameManager.Instance.GetMaterial(GameManager.Instance.CurrentPolarity);
        }
    }

    private void ApplyGravity()
    {
        isGrounded = controller.isGrounded;

        // FIX: Only reset jumps if grounded AND we are falling/stationary.
        // This prevents resetting the count immediately after we apply upward jump force.
        if (isGrounded && playerVelocity.y <= 0) 
        {
            jumpsPerformed = 0;
            playerVelocity.y = -2f; // Keep the player stuck to the ground
        }

        // Apply Gravity over time
        playerVelocity.y += gravityValue * gravityMultiplier * Time.deltaTime;
    }

    private void ProcessMovement()
    {
        Vector3 move = new Vector3(moveInput.x, 0, 0);
        controller.Move(move * moveSpeed * Time.deltaTime);
        controller.Move(playerVelocity * Time.deltaTime);
    }

    // --- Input System Events ---

    public void OnMove(InputValue value)
    {
        moveInput = value.Get<Vector2>();
    }

    public void OnJump(InputValue value)
    {
        if (!value.isPressed) return;

        // Allow jump if Grounded OR we have jumps left
        if (isGrounded || jumpsPerformed < maxJumps)
        {
            // Execute Jump Physics
            playerVelocity.y = Mathf.Sqrt(jumpHeight * -2f * (gravityValue * gravityMultiplier));
            
            // Increment counter
            // If we were on the ground, this becomes jump #1. 
            // If we were in the air, this becomes jump #2 (or #3 etc).
            jumpsPerformed++;
        }
    }

    public void OnInteract(InputValue value)
    {
        if (value.isPressed && GameManager.Instance != null)
        {
            Polarity newPolarity = GameManager.Instance.CurrentPolarity switch
            {
                Polarity.Neutral => Polarity.Happy,
                Polarity.Happy => Polarity.Angry,
                Polarity.Angry => Polarity.Neutral,
                _ => Polarity.Neutral
            };
            GameManager.Instance.SetPolarity(newPolarity);
        }
    }
}