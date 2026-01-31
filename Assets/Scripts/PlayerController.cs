using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 2.5D Platformer PlayerController using CharacterController.
/// Units are in centimeters (1 Unity unit = 1 cm).
/// Controls: W/Space = Jump, A = Move Left, D = Move Right.
/// Movement is locked to the X-Y plane (Z = 0) for 2.5D.
/// Uses New Input System.
/// </summary>
[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement (cm/s)")]
    [SerializeField] private float moveSpeed = 500f;
    [SerializeField] private float jumpForce = 1200f;
    [SerializeField] private float gravity = 2500f;

    private CharacterController controller;
    private Vector3 velocity;

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
    }

    private void Update()
    {
        if (Keyboard.current == null) return;

        bool isGrounded = controller.isGrounded;

        if (isGrounded && velocity.y < 0f)
        {
            velocity.y = -10f;
        }

        float horizontal = 0f;
        if (Keyboard.current.aKey.isPressed) horizontal = -1f;
        if (Keyboard.current.dKey.isPressed) horizontal = 1f;

        Vector3 move = new Vector3(horizontal * moveSpeed, 0f, 0f);

        if (isGrounded && (Keyboard.current.wKey.wasPressedThisFrame || Keyboard.current.spaceKey.wasPressedThisFrame))
        {
            velocity.y = jumpForce;
        }

        velocity.y -= gravity * Time.deltaTime;

        Vector3 finalMove = (move + velocity) * Time.deltaTime;
        controller.Move(finalMove);

        Vector3 pos = transform.position;
        if (pos.z != 0f)
        {
            pos.z = 0f;
            transform.position = pos;
        }

        if (horizontal != 0f)
        {
            Vector3 scale = transform.localScale;
            scale.x = horizontal > 0 ? Mathf.Abs(scale.x) : -Mathf.Abs(scale.x);
            transform.localScale = scale;
        }
    }
}
