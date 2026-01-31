using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Left hand VR input controls the PlatformerCharacter2D ONLY.
/// Disables XR locomotion so left thumbstick does NOT move the VR player.
/// Left thumbstick X-axis: move 2D character left/right.
/// Left X button (primaryButton): jump.
/// Desktop fallback: A/D + Space.
/// </summary>
public class VRCharacterController : MonoBehaviour
{
    [Header("VR Input")]
    [Tooltip("Left hand thumbstick (Vector2 action)")]
    public InputActionReference thumbstickAction;

    [Tooltip("Left X button / primaryButton (jump)")]
    public InputActionReference jumpAction;

    [Header("Settings")]
    public float deadZone = 0.3f;

    [Header("References")]
    public PlatformerCharacter2D character;

    // Runtime-created actions when no InputActionReference is assigned
    InputAction runtimeThumbstick;
    InputAction runtimeJump;

    void Start()
    {
        DisableXRLocomotion();
    }

    /// <summary>
    /// Disables ALL XR locomotion so the left thumbstick
    /// only controls the 2D character, not the VR player.
    /// Deactivates the entire Locomotion GameObject which contains:
    /// Turn (SnapTurnProvider, ContinuousTurnProvider),
    /// Move (DynamicMoveProvider), Teleportation, Climb, Jump, Gravity.
    /// </summary>
    void DisableXRLocomotion()
    {
        // Deactivate the entire Locomotion system
        GameObject locomotion = GameObject.Find("Locomotion");
        if (locomotion != null)
        {
            locomotion.SetActive(false);
            Debug.Log("[VRController] Disabled entire Locomotion system (Turn, Move, Teleport, Climb, Jump, Gravity)");
        }
        else
        {
            // Fallback: search within XR Origin hierarchy
            GameObject xrOrigin = GameObject.Find("XR Origin (XR Rig)");
            if (xrOrigin != null)
            {
                Transform loco = xrOrigin.transform.Find("Locomotion");
                if (loco != null)
                {
                    loco.gameObject.SetActive(false);
                    Debug.Log("[VRController] Disabled Locomotion system via hierarchy");
                }
            }
        }
    }

    void OnEnable()
    {
        // Use assigned InputActionReference, or create runtime XR bindings
        if (thumbstickAction != null && thumbstickAction.action != null)
        {
            thumbstickAction.action.Enable();
        }
        else
        {
            runtimeThumbstick = new InputAction("LeftThumbstick", InputActionType.Value);
            runtimeThumbstick.AddBinding("<XRController>{LeftHand}/thumbstick");
            runtimeThumbstick.Enable();
        }

        if (jumpAction != null && jumpAction.action != null)
        {
            jumpAction.action.Enable();
            jumpAction.action.performed += OnJump;
        }
        else
        {
            runtimeJump = new InputAction("LeftXButton", InputActionType.Button);
            runtimeJump.AddBinding("<XRController>{LeftHand}/primaryButton");
            runtimeJump.performed += OnJump;
            runtimeJump.Enable();
        }
    }

    void OnDisable()
    {
        if (jumpAction != null && jumpAction.action != null)
            jumpAction.action.performed -= OnJump;

        if (runtimeJump != null)
        {
            runtimeJump.performed -= OnJump;
            runtimeJump.Disable();
            runtimeJump.Dispose();
            runtimeJump = null;
        }
        if (runtimeThumbstick != null)
        {
            runtimeThumbstick.Disable();
            runtimeThumbstick.Dispose();
            runtimeThumbstick = null;
        }
    }

    void OnJump(InputAction.CallbackContext ctx)
    {
        if (character != null)
            character.GodJump();
    }

    void Update()
    {
        if (character == null) return;

        float moveDir = 0f;

        // VR thumbstick input
        InputAction stick = (thumbstickAction != null && thumbstickAction.action != null)
            ? thumbstickAction.action
            : runtimeThumbstick;

        if (stick != null)
        {
            Vector2 val = stick.ReadValue<Vector2>();
            if (val.x < -deadZone)
                moveDir = -1f;
            else if (val.x > deadZone)
                moveDir = 1f;
        }

        // Desktop fallback: A/D keys
        if (moveDir == 0f && Keyboard.current != null)
        {
            if (Keyboard.current.aKey.isPressed)
                moveDir = -1f;
            else if (Keyboard.current.dKey.isPressed)
                moveDir = 1f;
        }

        // Desktop fallback: Space to jump
        if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            character.GodJump();
        }

        character.SetMoveDirection(moveDir);
    }
}
