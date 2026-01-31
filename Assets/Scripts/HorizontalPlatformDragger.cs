using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR;

/// <summary>
/// Attach to any platform group object to make it horizontally draggable (left/right)
/// using the right VR controller grip button.
/// - Player standing on it rides along (won't fall through).
/// - Respects collision with other draggable platforms.
/// - Drag range is relative to the platform's starting position.
/// </summary>
public class HorizontalPlatformDragger : MonoBehaviour
{
    [Header("VR Input")]
    [Tooltip("Right hand controller transform. Auto-found if left empty.")]
    public Transform vrRightHand;

    [Header("Drag Settings")]
    [Tooltip("How far left/right from the starting position the platform can travel.")]
    public float dragRange = 5f;
    public float dragSmoothing = 15f;
    [Tooltip("Max raycast distance from controller to platform.")]
    public float maxRayDistance = 50f;
    [Tooltip("Padding between platforms to prevent overlap.")]
    public float collisionPadding = 0.05f;

    [Header("Visual Feedback")]
    public Color blinkColor = new Color(1f, 0.5f, 0f, 1f); // bright orange
    [Tooltip("How fast the platform blinks when idle (cycles per second).")]
    public float blinkSpeed = 1.5f;
    [Tooltip("How much the blink color blends in (0 = subtle, 1 = full).")]
    [Range(0f, 1f)]
    public float blinkIntensity = 0.6f;

    // Track all active instances for inter-platform collision
    public static readonly List<HorizontalPlatformDragger> allInstances = new List<HorizontalPlatformDragger>();

    // Shared input action across all instances
    static InputAction sharedGrip;
    static int instanceCount;

    bool isDragging;
    bool isHovered;
    float dragOffsetX;
    float targetX;
    bool gripWasDown;
    float originX;

    Collider[] childColliders;
    Renderer[] renderers;
    Color[] originalColors;

    void Awake()
    {
        originX = transform.position.x;
        childColliders = GetComponentsInChildren<Collider>();
    }

    void OnEnable()
    {
        allInstances.Add(this);
        instanceCount++;
        if (sharedGrip == null)
        {
            sharedGrip = new InputAction("RightGripH", InputActionType.Value);
            sharedGrip.AddBinding("<XRController>{RightHand}/grip");
            sharedGrip.AddBinding("<OculusTouchController>{RightHand}/grip");
            sharedGrip.AddBinding("<MetaQuestTouchProController>{RightHand}/grip");
            sharedGrip.Enable();
        }

        targetX = transform.position.x;

        if (vrRightHand == null)
            TryFindRightHand();

        CacheRenderers();
    }

    void OnDisable()
    {
        allInstances.Remove(this);
        instanceCount--;
        if (instanceCount <= 0 && sharedGrip != null)
        {
            sharedGrip.Disable();
            sharedGrip.Dispose();
            sharedGrip = null;
            instanceCount = 0;
        }

        RestoreColors();
    }

    void TryFindRightHand()
    {
        GameObject xrOrigin = GameObject.Find("XR Origin (XR Rig)");
        if (xrOrigin == null)
            xrOrigin = GameObject.Find("XR Origin");

        if (xrOrigin != null)
        {
            Transform cameraOffset = xrOrigin.transform.Find("Camera Offset");
            if (cameraOffset != null)
            {
                Transform rightCtrl = cameraOffset.Find("Right Controller");
                if (rightCtrl == null)
                    rightCtrl = cameraOffset.Find("RightHand Controller");
                if (rightCtrl != null)
                    vrRightHand = rightCtrl;
            }
        }
    }

    // ── Visual helpers ──

    void CacheRenderers()
    {
        renderers = GetComponentsInChildren<Renderer>();
        originalColors = new Color[renderers.Length];
        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i].material.HasProperty("_Color"))
                originalColors[i] = renderers[i].material.color;
            else
                originalColors[i] = Color.white;
        }
    }

    void RestoreColors()
    {
        if (renderers == null) return;
        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] != null && renderers[i].material.HasProperty("_Color"))
                renderers[i].material.color = originalColors[i];
        }
    }

    void SetVisualColor(Color color)
    {
        if (renderers == null) return;
        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] != null && renderers[i].material.HasProperty("_Color"))
                renderers[i].material.color = color;
        }
    }

    void BlendColors(float t)
    {
        if (renderers == null) return;
        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] != null && renderers[i].material.HasProperty("_Color"))
                renderers[i].material.color = Color.Lerp(originalColors[i], blinkColor, t);
        }
    }

    // ── Bounds helpers ──

    public Bounds GetWorldBounds()
    {
        if (childColliders == null || childColliders.Length == 0)
            return new Bounds(transform.position, Vector3.one);

        Bounds b = childColliders[0].bounds;
        for (int i = 1; i < childColliders.Length; i++)
            b.Encapsulate(childColliders[i].bounds);
        return b;
    }

    Bounds GetBoundsAtX(float x)
    {
        float deltaX = x - transform.position.x;
        Bounds b = GetWorldBounds();
        b.center += new Vector3(deltaX, 0f, 0f);
        return b;
    }

    // ── Collision between platforms ──

    float ClampAgainstOtherPlatforms(float desiredX)
    {
        Bounds myBounds = GetBoundsAtX(desiredX);
        myBounds.Expand(collisionPadding);

        foreach (var other in allInstances)
        {
            if (other == this) continue;
            Bounds otherBounds = other.GetWorldBounds();

            if (myBounds.Intersects(otherBounds))
            {
                if (desiredX > transform.position.x)
                {
                    float maxAllowed = otherBounds.min.x - (GetWorldBounds().size.x * 0.5f) - collisionPadding;
                    desiredX = Mathf.Min(desiredX, maxAllowed);
                }
                else
                {
                    float minAllowed = otherBounds.max.x + (GetWorldBounds().size.x * 0.5f) + collisionPadding;
                    desiredX = Mathf.Max(desiredX, minAllowed);
                }
            }
        }

        // Also check VerticalPlatformDragger instances
        foreach (var other in VerticalPlatformDragger.allInstances)
        {
            Bounds otherBounds = other.GetWorldBounds();
            if (myBounds.Intersects(otherBounds))
            {
                if (desiredX > transform.position.x)
                {
                    float maxAllowed = otherBounds.min.x - (GetWorldBounds().size.x * 0.5f) - collisionPadding;
                    desiredX = Mathf.Min(desiredX, maxAllowed);
                }
                else
                {
                    float minAllowed = otherBounds.max.x + (GetWorldBounds().size.x * 0.5f) + collisionPadding;
                    desiredX = Mathf.Max(desiredX, minAllowed);
                }
            }
        }

        return desiredX;
    }

    // ── Player riding ──

    void MoveWithRiders(float deltaX)
    {
        if (Mathf.Approximately(deltaX, 0f)) return;

        Bounds b = GetWorldBounds();
        Vector3 checkCenter = new Vector3(b.center.x, b.max.y + 0.15f, b.center.z);
        Vector3 checkHalf = new Vector3(b.extents.x, 0.2f, b.extents.z);

        Collider[] hits = Physics.OverlapBox(checkCenter, checkHalf);
        foreach (var col in hits)
        {
            if (col.transform.IsChildOf(transform)) continue;

            Rigidbody rb = col.attachedRigidbody;
            if (rb != null && !rb.isKinematic)
            {
                rb.MovePosition(rb.position + new Vector3(deltaX, 0f, 0f));
            }
            else if (rb == null)
            {
                col.transform.position += new Vector3(deltaX, 0f, 0f);
            }
        }
    }

    // ── Pointing / Raycast ──

    bool IsPointingAtMe(out Vector3 hitPoint)
    {
        hitPoint = Vector3.zero;
        if (vrRightHand == null) return false;

        Ray ray = new Ray(vrRightHand.position, vrRightHand.forward);
        RaycastHit[] hits = Physics.RaycastAll(ray, maxRayDistance);

        foreach (var hit in hits)
        {
            if (hit.transform == transform || hit.transform.IsChildOf(transform))
            {
                hitPoint = hit.point;
                return true;
            }
        }
        return false;
    }

    bool IsMousePointingAtMe(out Vector3 hitPoint)
    {
        hitPoint = Vector3.zero;
        if (Camera.main == null || Mouse.current == null) return false;

        Vector2 mousePos = Mouse.current.position.ReadValue();
        Ray ray = Camera.main.ScreenPointToRay(new Vector3(mousePos.x, mousePos.y, 0f));
        RaycastHit[] hits = Physics.RaycastAll(ray, maxRayDistance);

        foreach (var hit in hits)
        {
            if (hit.transform == transform || hit.transform.IsChildOf(transform))
            {
                hitPoint = hit.point;
                return true;
            }
        }
        return false;
    }

    float GetPointerWorldX()
    {
        if (vrRightHand != null)
        {
            Ray ray = new Ray(vrRightHand.position, vrRightHand.forward);
            Plane plane = new Plane(Vector3.forward, transform.position);
            if (plane.Raycast(ray, out float dist))
                return ray.GetPoint(dist).x;
            return vrRightHand.position.x;
        }

        if (Camera.main != null && Mouse.current != null)
        {
            Vector2 mousePos = Mouse.current.position.ReadValue();
            Ray ray = Camera.main.ScreenPointToRay(new Vector3(mousePos.x, mousePos.y, 0f));
            Plane plane = new Plane(Vector3.forward, transform.position);
            if (plane.Raycast(ray, out float dist))
                return ray.GetPoint(dist).x;
        }
        return transform.position.x;
    }

    // ── Main loop ──

    void Update()
    {
        float inputSystemVal = sharedGrip != null ? sharedGrip.ReadValue<float>() : 0f;
        float xrVal = ReadRightGripXR();
        float gripVal = Mathf.Max(inputSystemVal, xrVal);

        bool rightMouseDown = Mouse.current != null && Mouse.current.rightButton.isPressed;
        bool rightMousePressed = Mouse.current != null && Mouse.current.rightButton.wasPressedThisFrame;
        bool rightMouseReleased = Mouse.current != null && Mouse.current.rightButton.wasReleasedThisFrame;

        bool gripDown = gripVal > 0.3f;
        bool gripPressed = gripDown && !gripWasDown;
        bool gripReleased = !gripDown && gripWasDown;
        gripWasDown = gripDown;

        bool isPressed = gripPressed || rightMousePressed;
        bool isHeld = gripDown || rightMouseDown;
        bool isReleased = gripReleased || rightMouseReleased;

        if (isDragging)
        {
            if (isReleased || !isHeld)
            {
                isDragging = false;
                Debug.Log($"[HorizDragger] Released {name}");
            }
            else
            {
                float pointerX = GetPointerWorldX();
                float desiredX = pointerX - dragOffsetX;

                // Clamp to origin-based range
                desiredX = Mathf.Clamp(desiredX, originX - dragRange, originX + dragRange);

                // Clamp against other platforms
                desiredX = ClampAgainstOtherPlatforms(desiredX);

                targetX = Mathf.Lerp(targetX, desiredX, Time.deltaTime * dragSmoothing);

                float oldX = transform.position.x;
                Vector3 pos = transform.position;
                pos.x = targetX;
                transform.position = pos;

                // Carry anything standing on top
                MoveWithRiders(targetX - oldX);

                SetVisualColor(blinkColor);
                return;
            }
        }
        else
        {
            bool pointing = IsPointingAtMe(out Vector3 hitPt);
            if (!pointing)
                pointing = IsMousePointingAtMe(out hitPt);

            isHovered = pointing;

            if (isPressed && pointing)
            {
                isDragging = true;
                float pointerX = GetPointerWorldX();
                dragOffsetX = pointerX - transform.position.x;
                targetX = transform.position.x;
                SetVisualColor(blinkColor);
                Debug.Log($"[HorizDragger] Grabbed {name}");
                return;
            }
        }

        // Idle blink
        float speed = isHovered ? blinkSpeed * 2.5f : blinkSpeed;
        float intensity = isHovered ? 1f : blinkIntensity;
        float t = (Mathf.Sin(Time.time * speed * Mathf.PI * 2f) + 1f) * 0.5f * intensity;
        BlendColors(t);
    }

    float ReadRightGripXR()
    {
        var devices = new List<UnityEngine.XR.InputDevice>();
        InputDevices.GetDevicesAtXRNode(XRNode.RightHand, devices);
        foreach (var dev in devices)
        {
            if (dev.TryGetFeatureValue(UnityEngine.XR.CommonUsages.grip, out float val))
                return val;
        }
        return 0f;
    }
}
