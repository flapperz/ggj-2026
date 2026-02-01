using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// VR Controller for Level 1.
/// Left Thumbstick: Move X
/// Left Primary (A/X): Jump
/// Right Thumbstick Flick: Mask Change (Left=Happy, Right=Angry, Down=Neutral)
/// Desktop Fallback: A/D (Move), Space (Jump), A/S/D (Masks)
/// </summary>
public class VRPlayer3DController2 : MonoBehaviour
{
    public Player playerCharacter;

    [Header("Settings")]
    public float deadZone = 0.3f;
    public float flickThreshold = 0.7f;

    // Input Actions
    private InputAction moveAction;
    private InputAction jumpAction;
    private InputAction flickAction;

    // State tracking for flicking logic
    private bool canFlick = true;

    void Start()
    {
        GameObject playerObj = GameObject.FindWithTag("Player");

        if (playerObj != null)
        {
            // Get the Player script component from that object
            playerCharacter = playerObj.GetComponent<Player>();
        }
        DisableXRLocomotion();
    }

    void OnEnable()
    {
        // Left Hand Jump (Primary Button is usually A on Quest Left, or X)
        jumpAction = new InputAction("Jump", InputActionType.Button, "<XRController>{LeftHand}/primaryButton");
        jumpAction.performed += _ => TryJump();
        jumpAction.Enable();

        // Right Hand Flick
        flickAction = new InputAction("Flick", InputActionType.Value, "<XRController>{RightHand}/thumbstick");
        flickAction.Enable();
    }

    void OnDisable()
    {
        moveAction?.Disable();
        flickAction?.Disable();
        if (jumpAction != null)
        {
            jumpAction.performed -= _ => TryJump();
            jumpAction.Disable();
        }
    }

    void Update()
    {
        if (playerCharacter == null) return;

        HandleFlickDetection();
        HandleKeyboardFallback();
    }

    private void HandleFlickDetection()
    {
        Vector2 stick = flickAction.ReadValue<Vector2>();
        float magnitude = stick.magnitude;

        // 1. Reset: If the stick returns to the center, allow the next flick
        if (magnitude < deadZone)
        {
            canFlick = true;
            return;
        }

        // 2. Trigger: If stick pushed past threshold and we haven't registered this flick yet
        if (canFlick && magnitude > flickThreshold)
        {
            // Left -> Happy
            if (stick.x < -0.5f)
            {
                TryMaskChange(Polarity.Happy);
                canFlick = false;
            }
            // Right -> Angry
            else if (stick.x > 0.5f)
            {
                TryMaskChange(Polarity.Angry);
                canFlick = false;
            }
            // Down -> Neutral
            else if (stick.y < -0.5f)
            {
                TryMaskChange(Polarity.Neutral);
                canFlick = false;
            }
        }
    }

    private void HandleKeyboardFallback()
    {
        if (Keyboard.current == null) return;

        // Jump Fallback
        if (Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            TryJump();
        }

        // Mask Fallback (Directional logic)
        if (Keyboard.current.aKey.wasPressedThisFrame) TryMaskChange(Polarity.Happy);   // A for Left/Happy
        if (Keyboard.current.sKey.wasPressedThisFrame) TryMaskChange(Polarity.Neutral); // S for Down/Neutral
        if (Keyboard.current.dKey.wasPressedThisFrame) TryMaskChange(Polarity.Angry);   // D for Right/Angry
    }

    private void TryJump()
    {
        if (playerCharacter != null) playerCharacter.TriggerJump();
    }

    private void TryMaskChange(Polarity p)
    {
        if (playerCharacter != null) playerCharacter.TriggerMaskChange(p);
    }

    private void DisableXRLocomotion()
    {
        // Try various common names for the Locomotion system to disable it
        GameObject locomotion = GameObject.Find("Locomotion");
        if (locomotion == null)
        {
            GameObject xrOrigin = GameObject.Find("XR Origin (XR Rig)");
            if (xrOrigin != null)
            {
                Transform locoTrans = xrOrigin.transform.Find("Locomotion");
                if (locoTrans != null) locomotion = locoTrans.gameObject;
            }
        }

        if (locomotion != null)
        {
            locomotion.SetActive(false);
            Debug.Log("[VRPlayer3D] Locomotion system disabled.");
        }
    }
}