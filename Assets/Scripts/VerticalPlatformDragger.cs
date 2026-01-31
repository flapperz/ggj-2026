using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR;

/// <summary>
/// Attach to any platform group object to make it vertically draggable (up/down)
/// using the right VR controller grip button.
/// - Player standing on it rides along (won't fall through).
/// - Respects collision with other draggable platforms.
/// - Drag range is relative to the platform's starting position.
/// </summary>
public class VerticalPlatformDragger : MonoBehaviour
{
    [Header("VR Input")]
    [Tooltip("Right hand controller transform. Auto-found if left empty.")]
    public Transform vrRightHand;

    [Header("Drag Settings")]
    [Tooltip("How far up/down from the starting position the platform can travel.")]
    public float dragRange = 5f;
    public float dragSmoothing = 15f;
    [Tooltip("Max raycast distance from controller to platform.")]
    public float maxRayDistance = 50f;
    [Tooltip("Padding between platforms to prevent overlap.")]
    public float collisionPadding = 0.05f;

    [Header("Visual Feedback")]
    public Color blinkColor = new Color(0f, 1f, 1f, 1f); // bright cyan
    [Tooltip("How fast the platform blinks when idle (cycles per second).")]
    public float blinkSpeed = 1.5f;
    [Tooltip("How much the blink color blends in (0 = subtle, 1 = full).")]
    [Range(0f, 1f)]
    public float blinkIntensity = 0.6f;

    // Track all active instances for inter-platform collision
    public static readonly List<VerticalPlatformDragger> allInstances = new List<VerticalPlatformDragger>();

    // Shared input action across all instances
    static InputAction sharedGrip;
    static int instanceCount;

    bool isDragging;
    bool isHovered;
    float dragOffsetY;
    float targetY;
    bool gripWasDown;
    float originY;

    Collider[] childColliders;
    Renderer[] renderers;
    Color[] originalColors;

    void Awake()
    {
        originY = transform.position.y;
        childColliders = GetComponentsInChildren<Collider>();
    }

    void OnEnable()
    {
        allInstances.Add(this);
        instanceCount++;
        if (sharedGrip == null)
        {
            sharedGrip = new InputAction("RightGrip", InputActionType.Value);
            sharedGrip.AddBinding("<XRController>{RightHand}/grip");
            sharedGrip.AddBinding("<OculusTouchController>{RightHand}/grip");
            sharedGrip.AddBinding("<MetaQuestTouchProController>{RightHand}/grip");
            sharedGrip.Enable();
        }

        targetY = transform.position.y;

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

    Bounds GetBoundsAtY(float y)
    {
        float deltaY = y - transform.position.y;
        Bounds b = GetWorldBounds();
        b.center += new Vector3(0f, deltaY, 0f);
        return b;
    }

    // ── Collision between platforms ──

    float ClampAgainstOtherPlatforms(float desiredY)
    {
        Bounds myBounds = GetBoundsAtY(desiredY);
        myBounds.Expand(collisionPadding);

        foreach (var other in allInstances)
        {
            if (other == this) continue;
            Bounds otherBounds = other.GetWorldBounds();

            if (myBounds.Intersects(otherBounds))
            {
                // Moving up and hit something above
                if (desiredY > transform.position.y)
                {
                    float maxAllowed = otherBounds.min.y - (GetWorldBounds().size.y * 0.5f) - collisionPadding;
                    desiredY = Mathf.Min(desiredY, maxAllowed);
                }
                // Moving down and hit something below
                else
                {
                    float minAllowed = otherBounds.max.y + (GetWorldBounds().size.y * 0.5f) + collisionPadding;
                    desiredY = Mathf.Max(desiredY, minAllowed);
                }
            }
        }

        // Also check HorizontalPlatformDragger instances
        foreach (var other in HorizontalPlatformDragger.allInstances)
        {
            Bounds otherBounds = other.GetWorldBounds();
            if (myBounds.Intersects(otherBounds))
            {
                if (desiredY > transform.position.y)
                {
                    float maxAllowed = otherBounds.min.y - (GetWorldBounds().size.y * 0.5f) - collisionPadding;
                    desiredY = Mathf.Min(desiredY, maxAllowed);
                }
                else
                {
                    float minAllowed = otherBounds.max.y + (GetWorldBounds().size.y * 0.5f) + collisionPadding;
                    desiredY = Mathf.Max(desiredY, minAllowed);
                }
            }
        }

        return desiredY;
    }

    // ── Player riding ──

    void MoveWithRiders(float deltaY)
    {
        if (Mathf.Approximately(deltaY, 0f)) return;

        Bounds b = GetWorldBounds();
        // Check a thin box just above the platform top surface
        Vector3 checkCenter = new Vector3(b.center.x, b.max.y + 0.15f, b.center.z);
        Vector3 checkHalf = new Vector3(b.extents.x, 0.2f, b.extents.z);

        Collider[] hits = Physics.OverlapBox(checkCenter, checkHalf);
        foreach (var col in hits)
        {
            if (col.transform.IsChildOf(transform)) continue;

            Rigidbody rb = col.attachedRigidbody;
            if (rb != null && !rb.isKinematic)
            {
                rb.MovePosition(rb.position + new Vector3(0f, deltaY, 0f));
            }
            else if (rb == null)
            {
                // Non-rigidbody object sitting on top
                col.transform.position += new Vector3(0f, deltaY, 0f);
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

    float GetPointerWorldY()
    {
        if (vrRightHand != null)
        {
            Ray ray = new Ray(vrRightHand.position, vrRightHand.forward);
            Plane vertPlane = new Plane(Vector3.forward, transform.position);
            if (vertPlane.Raycast(ray, out float dist))
                return ray.GetPoint(dist).y;
            return vrRightHand.position.y;
        }

        if (Camera.main != null && Mouse.current != null)
        {
            Vector2 mousePos = Mouse.current.position.ReadValue();
            Ray ray = Camera.main.ScreenPointToRay(new Vector3(mousePos.x, mousePos.y, 0f));
            Plane vertPlane = new Plane(Vector3.forward, transform.position);
            if (vertPlane.Raycast(ray, out float dist))
                return ray.GetPoint(dist).y;
        }
        return transform.position.y;
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
                Debug.Log($"[VertDragger] Released {name}");
            }
            else
            {
                float pointerY = GetPointerWorldY();
                float desiredY = pointerY - dragOffsetY;

                // Clamp to origin-based range
                desiredY = Mathf.Clamp(desiredY, originY - dragRange, originY + dragRange);

                // Clamp against other platforms
                desiredY = ClampAgainstOtherPlatforms(desiredY);

                targetY = Mathf.Lerp(targetY, desiredY, Time.deltaTime * dragSmoothing);

                float oldY = transform.position.y;
                Vector3 pos = transform.position;
                pos.y = targetY;
                transform.position = pos;

                // Carry anything standing on top
                MoveWithRiders(targetY - oldY);

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
                float pointerY = GetPointerWorldY();
                dragOffsetY = pointerY - transform.position.y;
                targetY = transform.position.y;
                SetVisualColor(blinkColor);
                Debug.Log($"[VertDragger] Grabbed {name}");
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
