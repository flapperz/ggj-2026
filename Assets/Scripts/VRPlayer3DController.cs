using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// VR left-hand input drives the Level 1 Player.cs character.
/// Left thumbstick X-axis → move left/right.
/// Left X button (primaryButton) → jump.
/// Desktop fallback: A/D + Space.
/// Disables XR Locomotion so thumbstick only controls the game character.
/// </summary>
public class VRPlayer3DController : MonoBehaviour
{
    [Header("References")]
    public Player playerCharacter;

    [Header("Settings")]
    public float deadZone = 0.3f;

    InputAction runtimeThumbstick;
    InputAction runtimeJump;

    void Start()
    {
        DisableXRLocomotion();
    }

    void DisableXRLocomotion()
    {
        GameObject locomotion = GameObject.Find("Locomotion");
        if (locomotion != null)
        {
            locomotion.SetActive(false);
            Debug.Log("[VRPlayer3D] Disabled entire Locomotion system");
            return;
        }

        GameObject xrOrigin = GameObject.Find("XR Origin (XR Rig)");
        if (xrOrigin != null)
        {
            Transform loco = xrOrigin.transform.Find("Locomotion");
            if (loco != null)
            {
                loco.gameObject.SetActive(false);
                Debug.Log("[VRPlayer3D] Disabled Locomotion system via hierarchy");
            }
        }
    }

    void OnEnable()
    {
        runtimeThumbstick = new InputAction("LeftThumbstick", InputActionType.Value);
        runtimeThumbstick.AddBinding("<XRController>{LeftHand}/thumbstick");
        runtimeThumbstick.Enable();

        runtimeJump = new InputAction("LeftXButton", InputActionType.Button);
        runtimeJump.AddBinding("<XRController>{LeftHand}/primaryButton");
        runtimeJump.performed += OnJump;
        runtimeJump.Enable();
    }

    void OnDisable()
    {
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
        if (playerCharacter != null)
            playerCharacter.TriggerJump();
    }

    void Update()
    {
        if (playerCharacter == null) return;

        float moveX = 0f;

        // VR thumbstick
        if (runtimeThumbstick != null)
        {
            Vector2 val = runtimeThumbstick.ReadValue<Vector2>();
            if (val.x < -deadZone)
                moveX = -1f;
            else if (val.x > deadZone)
                moveX = 1f;
        }

        // Desktop fallback: A/D
        if (moveX == 0f && Keyboard.current != null)
        {
            if (Keyboard.current.aKey.isPressed)
                moveX = -1f;
            else if (Keyboard.current.dKey.isPressed)
                moveX = 1f;
        }

        // Desktop fallback: Space to jump
        if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            if (playerCharacter != null)
                playerCharacter.TriggerJump();
        }

        playerCharacter.SetMoveInput(new Vector2(moveX, 0f));
    }
}
