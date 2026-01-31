using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.XR;

/// <summary>
/// Right-hand grip input drives horizontal dragging of DraggablePlatform2D objects.
/// Same dual-source input pattern as GodScreenShooter (InputAction + XR subsystem fallback).
/// Desktop fallback: right-click drag.
/// </summary>
public class GodPlatformDragger : MonoBehaviour
{
    [Header("VR Input")]
    public Transform vrRayOrigin;

    [Header("Aim")]
    [Range(0f, 90f)]
    public float aimTiltX = 60f;

    [Header("Screen")]
    public Transform gameScreen;

    [Header("2D World")]
    public Transform gameWorld;

    [Header("Dragging")]
    public float dragSmoothing = 15f;
    public float pickRadius = 0.5f;

    InputAction runtimeGrip;
    DraggablePlatform2D currentTarget;
    DraggablePlatform2D hoveredTarget;
    float dragOffsetX;
    bool gripWasDown;

    // Visuals (found at runtime from GodScreenShooter's objects)
    LineRenderer laserLine;
    Renderer aimDotRenderer;
    Color laserStartOrig;
    Color laserEndOrig;
    Color aimDotColorOrig;
    bool visualsCached;

    public bool IsDragging => currentTarget != null;

    void OnEnable()
    {
        runtimeGrip = new InputAction("RightGrip", InputActionType.Value);
        runtimeGrip.AddBinding("<XRController>{RightHand}/grip");
        runtimeGrip.AddBinding("<OculusTouchController>{RightHand}/grip");
        runtimeGrip.AddBinding("<MetaQuestTouchProController>{RightHand}/grip");
        runtimeGrip.Enable();
        Debug.Log("[PlatformDragger] Enabled - grip input bound");
    }

    void OnDisable()
    {
        if (runtimeGrip != null)
        {
            runtimeGrip.Disable();
            runtimeGrip.Dispose();
            runtimeGrip = null;
        }
    }

    Vector3 GetAimDirection()
    {
        return Quaternion.AngleAxis(aimTiltX, vrRayOrigin.right) * vrRayOrigin.forward;
    }

    Vector3 GetDirectionToScreen()
    {
        return (gameScreen.position - vrRayOrigin.position).normalized;
    }

    bool GetScreenPoint(out Vector3 screenWorldPoint, out float u, out float v)
    {
        screenWorldPoint = Vector3.zero;
        u = v = 0.5f;

        if (vrRayOrigin == null || gameScreen == null) return false;

        Vector3 pos = vrRayOrigin.position;

        // Method 1: Physics raycast with raw controller forward
        if (Physics.Raycast(new Ray(pos, vrRayOrigin.forward), out RaycastHit hit, 50f))
        {
            if (hit.transform == gameScreen)
            {
                screenWorldPoint = hit.point;
                Vector3 local = gameScreen.InverseTransformPoint(hit.point);
                u = Mathf.Clamp01(local.x + 0.5f);
                v = Mathf.Clamp01(local.y + 0.5f);
                return true;
            }
        }

        // Method 2: Physics raycast with aim-corrected direction
        Vector3 aimDir = GetAimDirection();
        if (Physics.Raycast(new Ray(pos, aimDir), out hit, 50f))
        {
            if (hit.transform == gameScreen)
            {
                screenWorldPoint = hit.point;
                Vector3 local = gameScreen.InverseTransformPoint(hit.point);
                u = Mathf.Clamp01(local.x + 0.5f);
                v = Mathf.Clamp01(local.y + 0.5f);
                return true;
            }
        }

        // Method 3: angular offset fallback
        Vector3 toScreen = GetDirectionToScreen();
        Vector3 ctrlFwd = vrRayOrigin.forward;
        float rightOffset = Vector3.Dot(ctrlFwd - toScreen, gameScreen.right);
        float upOffset = Vector3.Dot(ctrlFwd - toScreen, gameScreen.up);
        float sens = 3f;
        u = Mathf.Clamp01(0.5f + rightOffset * sens);
        v = Mathf.Clamp01(0.5f + upOffset * sens);

        Vector3 localPt = new Vector3(u - 0.5f, v - 0.5f, 0f);
        screenWorldPoint = gameScreen.TransformPoint(localPt);
        return true;
    }

    Vector2 ScreenUVToWorld2D(float u, float v)
    {
        if (GodGameManager.Instance != null && GodGameManager.Instance.gameCamera2D != null)
        {
            Vector3 world = GodGameManager.Instance.gameCamera2D.ViewportToWorldPoint(new Vector3(u, v, 0f));
            if (gameWorld != null)
            {
                Vector3 local = gameWorld.InverseTransformPoint(new Vector3(world.x, world.y, gameWorld.position.z));
                return new Vector2(local.x, local.y);
            }
            return new Vector2(world.x, world.y);
        }
        return Vector2.zero;
    }

    DraggablePlatform2D FindPlatformAt(Vector2 worldPos2D)
    {
        Vector2 checkPos = worldPos2D;
        if (gameWorld != null)
        {
            Vector3 wp = gameWorld.TransformPoint(new Vector3(worldPos2D.x, worldPos2D.y, 0f));
            checkPos = new Vector2(wp.x, wp.y);
        }

        Collider2D[] hits = Physics2D.OverlapCircleAll(checkPos, pickRadius);
        foreach (var col in hits)
        {
            DraggablePlatform2D dp = col.GetComponent<DraggablePlatform2D>();
            if (dp != null) return dp;
        }
        return null;
    }

    void CacheVisuals()
    {
        if (visualsCached) return;

        GameObject laserObj = GameObject.Find("ShootLaser");
        if (laserObj != null)
        {
            laserLine = laserObj.GetComponent<LineRenderer>();
            if (laserLine != null)
            {
                laserStartOrig = laserLine.startColor;
                laserEndOrig = laserLine.endColor;
            }
        }

        GameObject dotObj = GameObject.Find("AimDot");
        if (dotObj != null)
        {
            aimDotRenderer = dotObj.GetComponent<Renderer>();
            if (aimDotRenderer != null)
            {
                aimDotColorOrig = aimDotRenderer.material.color;
            }
        }

        if (laserLine != null && aimDotRenderer != null)
            visualsCached = true;
    }

    void SetVisualState(int state)
    {
        // 0 = normal, 1 = hovered, 2 = dragged
        if (laserLine == null || aimDotRenderer == null) return;

        if (state == 0)
        {
            laserLine.startColor = laserStartOrig;
            laserLine.endColor = laserEndOrig;
            aimDotRenderer.material.color = aimDotColorOrig;
        }
        else if (state == 1)
        {
            Color hoverCol = new Color(0.6f, 1f, 1f, 1f);
            laserLine.startColor = hoverCol;
            laserLine.endColor = hoverCol;
            aimDotRenderer.material.color = hoverCol;
        }
        else if (state == 2)
        {
            Color dragCol = new Color(1f, 0.9f, 0.2f, 1f);
            laserLine.startColor = dragCol;
            laserLine.endColor = dragCol;
            aimDotRenderer.material.color = dragCol;
        }
    }

    void Update()
    {
        CacheVisuals();

        // Read grip from all sources
        float inputSystemVal = runtimeGrip != null ? runtimeGrip.ReadValue<float>() : 0f;
        float xrSubsystemVal = ReadRightGripXR();
        float gripVal = Mathf.Max(inputSystemVal, xrSubsystemVal);

        // Desktop fallback: right mouse button
        bool rightMouseDown = Mouse.current != null && Mouse.current.rightButton.isPressed;
        bool rightMousePressed = Mouse.current != null && Mouse.current.rightButton.wasPressedThisFrame;
        bool rightMouseReleased = Mouse.current != null && Mouse.current.rightButton.wasReleasedThisFrame;

        bool gripDown = gripVal > 0.3f;
        bool gripPressed = gripDown && !gripWasDown;
        bool gripReleased = !gripDown && gripWasDown;
        gripWasDown = gripDown;

        // Combine VR grip and desktop right-click
        bool isPressed = gripPressed || rightMousePressed;
        bool isHeld = gripDown || rightMouseDown;
        bool isReleased = gripReleased || rightMouseReleased;

        // Get current aim position in 2D
        Vector2 cursorWorld2D = Vector2.zero;
        bool hasAim = false;

        if (vrRayOrigin != null && gameScreen != null)
        {
            if (GetScreenPoint(out Vector3 screenPt, out float u, out float v))
            {
                cursorWorld2D = ScreenUVToWorld2D(u, v);
                hasAim = true;
            }
        }
        else if (Mouse.current != null && Camera.main != null && gameScreen != null)
        {
            // Desktop mouse fallback
            Vector2 mousePos = Mouse.current.position.ReadValue();
            Ray ray = Camera.main.ScreenPointToRay(new Vector3(mousePos.x, mousePos.y, 0f));
            float denom = Vector3.Dot(ray.direction, gameScreen.forward);
            if (Mathf.Abs(denom) > 0.0001f)
            {
                float t = Vector3.Dot(gameScreen.position - ray.origin, gameScreen.forward) / denom;
                if (t > 0f)
                {
                    Vector3 hitPoint = ray.origin + ray.direction * t;
                    Vector3 local = gameScreen.InverseTransformPoint(hitPoint);
                    float u = Mathf.Clamp01(local.x + 0.5f);
                    float v = Mathf.Clamp01(local.y + 0.5f);
                    cursorWorld2D = ScreenUVToWorld2D(u, v);
                    hasAim = true;
                }
            }
        }

        if (!hasAim)
        {
            if (hoveredTarget != null)
            {
                hoveredTarget.SetHovered(false);
                hoveredTarget = null;
            }
            SetVisualState(0);
            return;
        }

        // Dragging logic
        if (currentTarget != null)
        {
            // Currently dragging
            if (isReleased || !isHeld)
            {
                // Drop
                currentTarget.SetDragged(false);
                Debug.Log($"[PlatformDragger] Dropped {currentTarget.name}");
                currentTarget = null;
                SetVisualState(0);
            }
            else
            {
                // Continue dragging
                float desiredX = cursorWorld2D.x - dragOffsetX;
                float currentX = currentTarget.transform.localPosition.x;
                float smoothedX = Mathf.Lerp(currentX, desiredX, Time.deltaTime * dragSmoothing);
                currentTarget.SetLocalX(smoothedX);
                SetVisualState(2);
            }
        }
        else
        {
            // Not dragging - check for hover/grab
            DraggablePlatform2D found = FindPlatformAt(cursorWorld2D);

            // Update hover state
            if (found != hoveredTarget)
            {
                if (hoveredTarget != null) hoveredTarget.SetHovered(false);
                hoveredTarget = found;
                if (hoveredTarget != null) hoveredTarget.SetHovered(true);
            }

            if (found != null)
            {
                SetVisualState(1); // hover

                if (isPressed)
                {
                    // Grab
                    currentTarget = found;
                    dragOffsetX = cursorWorld2D.x - found.transform.localPosition.x;
                    currentTarget.SetDragged(true);
                    Debug.Log($"[PlatformDragger] Grabbed {currentTarget.name}");
                    SetVisualState(2);
                }
            }
            else
            {
                SetVisualState(0); // normal
            }
        }

        // Debug logging
        if (Time.frameCount % 300 == 0)
        {
            Debug.Log($"[PlatformDragger] Grip: IS={inputSystemVal:F2} XR={xrSubsystemVal:F2} dragging={IsDragging} vrRay={vrRayOrigin != null}");
        }
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
