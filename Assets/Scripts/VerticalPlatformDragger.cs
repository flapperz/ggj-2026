using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR;

/// <summary>
/// Attach to any platform group object to make it vertically draggable
/// using the right VR controller grip button.
/// Raycasts from the right controller; if it hits this object's collider
/// and grip is pressed, the platform moves up/down following controller movement.
/// Desktop fallback: right-click drag.
/// </summary>
public class VerticalPlatformDragger : MonoBehaviour
{
    [Header("VR Input")]
    [Tooltip("Right hand controller transform. Auto-found if left empty.")]
    public Transform vrRightHand;

    [Header("Drag Settings")]
    public float minY = -5f;
    public float maxY = 10f;
    public float dragSmoothing = 15f;
    [Tooltip("Max raycast distance from controller to platform.")]
    public float maxRayDistance = 50f;

    [Header("Visual Feedback")]
    public Color blinkColor = new Color(0.4f, 0.9f, 1f, 1f);
    [Tooltip("How fast the platform blinks when idle (cycles per second).")]
    public float blinkSpeed = 1.5f;
    [Tooltip("How much the blink color blends in (0 = subtle, 1 = full).")]
    [Range(0f, 1f)]
    public float blinkIntensity = 0.5f;

    // Shared input action across all instances
    static InputAction sharedGrip;
    static int instanceCount;

    bool isDragging;
    bool isHovered;
    float dragOffsetY;
    float targetY;
    bool gripWasDown;

    // Cache renderers for visual feedback
    Renderer[] renderers;
    Color[] originalColors;

    void OnEnable()
    {
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
        // Try to find the right hand controller in the XR rig
        GameObject xrOrigin = GameObject.Find("XR Origin (XR Rig)");
        if (xrOrigin == null)
            xrOrigin = GameObject.Find("XR Origin");

        if (xrOrigin != null)
        {
            // Common hierarchy: XR Origin > Camera Offset > Right Controller
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

    /// <summary>
    /// Check if the right controller ray hits any collider on this group object.
    /// Returns the world-space hit point.
    /// </summary>
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

    /// <summary>
    /// Desktop fallback: check if mouse ray hits this object.
    /// </summary>
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

    /// <summary>
    /// Get current Y position from controller ray or mouse in world space.
    /// </summary>
    float GetPointerWorldY()
    {
        if (vrRightHand != null)
        {
            // Project controller ray onto a vertical plane at the platform's X/Z
            Ray ray = new Ray(vrRightHand.position, vrRightHand.forward);
            // Use a plane facing the controller at the platform's position
            Plane vertPlane = new Plane(Vector3.forward, transform.position);
            if (vertPlane.Raycast(ray, out float dist))
            {
                Vector3 pt = ray.GetPoint(dist);
                return pt.y;
            }
            return vrRightHand.position.y;
        }

        // Desktop fallback
        if (Camera.main != null && Mouse.current != null)
        {
            Vector2 mousePos = Mouse.current.position.ReadValue();
            Ray ray = Camera.main.ScreenPointToRay(new Vector3(mousePos.x, mousePos.y, 0f));
            Plane vertPlane = new Plane(Vector3.forward, transform.position);
            if (vertPlane.Raycast(ray, out float dist))
            {
                Vector3 pt = ray.GetPoint(dist);
                return pt.y;
            }
        }
        return transform.position.y;
    }

    void Update()
    {
        // Read grip input
        float inputSystemVal = sharedGrip != null ? sharedGrip.ReadValue<float>() : 0f;
        float xrVal = ReadRightGripXR();
        float gripVal = Mathf.Max(inputSystemVal, xrVal);

        // Desktop fallback: right mouse button
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
                // Release
                isDragging = false;
                Debug.Log($"[VertDragger] Released {name}");
            }
            else
            {
                // Continue dragging vertically
                float pointerY = GetPointerWorldY();
                float desiredY = pointerY - dragOffsetY;
                desiredY = Mathf.Clamp(desiredY, minY, maxY);

                targetY = Mathf.Lerp(targetY, desiredY, Time.deltaTime * dragSmoothing);

                Vector3 pos = transform.position;
                pos.y = targetY;
                transform.position = pos;

                SetVisualColor(blinkColor);
                return; // skip blink while dragging
            }
        }
        else
        {
            // Check if controller is pointing at this object
            bool pointing = IsPointingAtMe(out Vector3 hitPt);
            if (!pointing)
                pointing = IsMousePointingAtMe(out hitPt);

            isHovered = pointing;

            if (isPressed && pointing)
            {
                // Start dragging
                isDragging = true;
                float pointerY = GetPointerWorldY();
                dragOffsetY = pointerY - transform.position.y;
                targetY = transform.position.y;
                SetVisualColor(blinkColor);
                Debug.Log($"[VertDragger] Grabbed {name}");
                return;
            }
        }

        // Idle blink: pulse between original color and blinkColor so player knows it's draggable
        // Blink faster when hovered to signal "ready to grab"
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
