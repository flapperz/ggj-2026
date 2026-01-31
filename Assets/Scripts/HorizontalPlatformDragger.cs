using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR;

/// <summary>
/// Attach to any platform group object to make it horizontally draggable (left/right)
/// using the right VR controller grip button.
/// Raycasts from the right controller; if it hits this object's collider
/// and grip is pressed, the platform moves left/right following controller movement.
/// Desktop fallback: right-click drag.
/// </summary>
public class HorizontalPlatformDragger : MonoBehaviour
{
    [Header("VR Input")]
    [Tooltip("Right hand controller transform. Auto-found if left empty.")]
    public Transform vrRightHand;

    [Header("Drag Settings")]
    public float minX = -20f;
    public float maxX = 20f;
    public float dragSmoothing = 15f;
    [Tooltip("Max raycast distance from controller to platform.")]
    public float maxRayDistance = 50f;

    [Header("Visual Feedback")]
    public Color blinkColor = new Color(1f, 0.6f, 0.2f, 1f); // orange
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
    float dragOffsetX;
    float targetX;
    bool gripWasDown;

    // Cache renderers for visual feedback
    Renderer[] renderers;
    Color[] originalColors;

    void OnEnable()
    {
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
            {
                Vector3 pt = ray.GetPoint(dist);
                return pt.x;
            }
            return vrRightHand.position.x;
        }

        // Desktop fallback
        if (Camera.main != null && Mouse.current != null)
        {
            Vector2 mousePos = Mouse.current.position.ReadValue();
            Ray ray = Camera.main.ScreenPointToRay(new Vector3(mousePos.x, mousePos.y, 0f));
            Plane plane = new Plane(Vector3.forward, transform.position);
            if (plane.Raycast(ray, out float dist))
            {
                Vector3 pt = ray.GetPoint(dist);
                return pt.x;
            }
        }
        return transform.position.x;
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
                isDragging = false;
                Debug.Log($"[HorizDragger] Released {name}");
            }
            else
            {
                // Continue dragging horizontally
                float pointerX = GetPointerWorldX();
                float desiredX = pointerX - dragOffsetX;
                desiredX = Mathf.Clamp(desiredX, minX, maxX);

                targetX = Mathf.Lerp(targetX, desiredX, Time.deltaTime * dragSmoothing);

                Vector3 pos = transform.position;
                pos.x = targetX;
                transform.position = pos;

                SetVisualColor(blinkColor);
                return; // skip blink while dragging
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

        // Idle blink: pulse between original color and blinkColor
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
