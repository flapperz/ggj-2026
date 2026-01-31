using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.XR;

/// <summary>
/// Right trigger fires a cube projectile from the VR controller toward the game screen.
/// The cube flies from controller to screen, then spawns a 2D projectile in the game world.
/// </summary>
public class GodScreenShooter : MonoBehaviour
{
    [Header("VR Input")]
    public Transform vrRayOrigin;

    [Header("Aim")]
    [Tooltip("Degrees to tilt aim down from grip forward. Positive = down. Meta Quest 3 ~= 60")]
    [Range(0f, 90f)]
    public float aimTiltX = 60f;

    [Header("Shooting")]
    public float cooldown = 0.25f;

    [Header("Screen")]
    public Transform gameScreen;

    [Header("2D World")]
    public Transform gameWorld;

    [HideInInspector] public GodPlatformDragger platformDragger;

    float lastShotTime;
    bool triggerWasDown;

    InputAction runtimeTrigger;

    // Visuals
    LineRenderer laserLine;
    GameObject aimDot;
    bool visualsReady;

    /// <summary>
    /// Returns the corrected aim direction from the controller.
    /// Meta Quest grip forward is angled up from actual pointing direction,
    /// so we rotate around the controller's local right axis to compensate.
    /// </summary>
    Vector3 GetAimDirection()
    {
        return Quaternion.AngleAxis(aimTiltX, vrRayOrigin.right) * vrRayOrigin.forward;
    }

    /// <summary>
    /// Simple direction from controller to screen center (always valid).
    /// </summary>
    Vector3 GetDirectionToScreen()
    {
        return (gameScreen.position - vrRayOrigin.position).normalized;
    }

    void SetupVisuals()
    {
        if (visualsReady) return;
        visualsReady = true;

        GameObject laserObj = new GameObject("ShootLaser");
        laserLine = laserObj.AddComponent<LineRenderer>();
        laserLine.positionCount = 2;
        laserLine.startWidth = 0.01f;
        laserLine.endWidth = 0.004f;
        laserLine.material = MakeUnlitMaterial(new Color(1f, 0.3f, 0.1f, 1f));
        laserLine.startColor = new Color(1f, 0.3f, 0.1f, 1f);
        laserLine.endColor = new Color(1f, 0.8f, 0.2f, 1f);
        laserLine.useWorldSpace = true;
        laserLine.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        laserLine.receiveShadows = false;

        aimDot = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        aimDot.name = "AimDot";
        Object.Destroy(aimDot.GetComponent<Collider>());
        aimDot.transform.localScale = Vector3.one * 0.06f;
        aimDot.GetComponent<Renderer>().material = MakeUnlitMaterial(new Color(1f, 0f, 0f, 1f));
    }

    void OnEnable()
    {
        runtimeTrigger = new InputAction("RightTrigger", InputActionType.Value);
        runtimeTrigger.AddBinding("<XRController>{RightHand}/trigger");
        runtimeTrigger.AddBinding("<XRController>{RightHand}/triggerPressed");
        runtimeTrigger.AddBinding("<OculusTouchController>{RightHand}/trigger");
        runtimeTrigger.AddBinding("<MetaQuestTouchProController>{RightHand}/trigger");
        runtimeTrigger.Enable();
    }

    void OnDisable()
    {
        if (runtimeTrigger != null)
        {
            runtimeTrigger.Disable();
            runtimeTrigger.Dispose();
            runtimeTrigger = null;
        }
    }

    /// <summary>
    /// Tries multiple methods to find where the controller points on the screen.
    /// 1. Raw controller forward (Physics.Raycast against screen collider)
    /// 2. Aim-corrected direction (tilted from grip forward)
    /// 3. Direction from controller to screen (always works, uses controller angle for offset)
    /// </summary>
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

        // Method 3: Use controller's angular offset from screen center direction
        // This always works regardless of grip pose offset
        Vector3 toScreen = GetDirectionToScreen();
        Vector3 ctrlFwd = vrRayOrigin.forward;

        // Project angular difference onto screen's local axes
        float rightOffset = Vector3.Dot(ctrlFwd - toScreen, gameScreen.right);
        float upOffset = Vector3.Dot(ctrlFwd - toScreen, gameScreen.up);

        // Sensitivity: how much controller angle affects screen position
        float sens = 3f;
        u = Mathf.Clamp01(0.5f + rightOffset * sens);
        v = Mathf.Clamp01(0.5f + upOffset * sens);

        Vector3 localPt = new Vector3(u - 0.5f, v - 0.5f, 0f);
        screenWorldPoint = gameScreen.TransformPoint(localPt);
        return true;
    }

    void Update()
    {
        if (vrRayOrigin != null && !visualsReady) SetupVisuals();

        // Laser and aim dot
        if (vrRayOrigin != null && gameScreen != null && laserLine != null)
        {
            // GetScreenPoint always returns true now (method 3 fallback)
            if (GetScreenPoint(out Vector3 screenPt, out float lu, out float lv))
            {
                laserLine.enabled = true;
                laserLine.SetPosition(0, vrRayOrigin.position);
                laserLine.SetPosition(1, screenPt);

                if (aimDot != null)
                {
                    aimDot.SetActive(true);
                    aimDot.transform.position = screenPt - gameScreen.forward * 0.01f;
                }
            }
        }

        // Read trigger from ALL sources
        float inputSystemVal = runtimeTrigger != null ? runtimeTrigger.ReadValue<float>() : 0f;
        float xrSubsystemVal = ReadRightTriggerXR();
        float triggerVal = Mathf.Max(inputSystemVal, xrSubsystemVal);

        // Debug: log trigger values periodically so we can diagnose
        if (Time.frameCount % 180 == 0)
        {
            Debug.Log($"[GodShooter] Trigger: IS={inputSystemVal:F2} XR={xrSubsystemVal:F2} vrRay={vrRayOrigin != null} screen={gameScreen != null}");
        }

        // Block shooting while dragging a platform
        bool dragBlocking = platformDragger != null && platformDragger.IsDragging;

        // Fire on trigger press (rising edge)
        if (triggerVal > 0.3f && !triggerWasDown)
        {
            triggerWasDown = true;
            if (!dragBlocking) TryShoot();
        }
        else if (triggerVal < 0.15f)
        {
            triggerWasDown = false;
        }

        // Desktop mouse fallback
        if (!dragBlocking && Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            TryShootMouse();
        }

        // Desktop keyboard: press F to fire (for testing)
        if (!dragBlocking && Keyboard.current != null && Keyboard.current.fKey.wasPressedThisFrame)
        {
            TryShoot();
        }
    }

    float ReadRightTriggerXR()
    {
        var devices = new List<UnityEngine.XR.InputDevice>();
        InputDevices.GetDevicesAtXRNode(XRNode.RightHand, devices);
        foreach (var dev in devices)
        {
            if (dev.TryGetFeatureValue(UnityEngine.XR.CommonUsages.trigger, out float val))
                return val;
        }
        return 0f;
    }

    void TryShoot()
    {
        if (Time.time - lastShotTime < cooldown) return;
        lastShotTime = Time.time;

        if (vrRayOrigin == null || gameScreen == null)
        {
            Debug.LogWarning($"[GodShooter] TryShoot skipped: vrRayOrigin={vrRayOrigin != null}, gameScreen={gameScreen != null}");
            return;
        }

        Vector3 spawnPos = vrRayOrigin.position;
        Vector3 targetPoint;
        float u = 0.5f, v = 0.5f;

        if (GetScreenPoint(out Vector3 screenPt, out float su, out float sv))
        {
            targetPoint = screenPt;
            u = su;
            v = sv;
        }
        else
        {
            // Ray missed screen - aim at screen center as fallback
            targetPoint = gameScreen.position;
        }

        ShootCubeBullet.Fire(spawnPos, targetPoint, gameScreen, u, v, gameWorld);
        Debug.Log($"[GodShooter] FIRED cube UV=({u:F2},{v:F2}) aimDir={GetAimDirection()}");
    }

    void TryShootMouse()
    {
        if (Time.time - lastShotTime < cooldown) return;
        if (Camera.main == null || gameScreen == null) return;
        lastShotTime = Time.time;

        Vector2 mousePos = Mouse.current.position.ReadValue();
        Ray ray = Camera.main.ScreenPointToRay(new Vector3(mousePos.x, mousePos.y, 0f));

        float denom = Vector3.Dot(ray.direction, gameScreen.forward);
        if (Mathf.Abs(denom) < 0.0001f) return;
        float t = Vector3.Dot(gameScreen.position - ray.origin, gameScreen.forward) / denom;
        if (t < 0f) return;

        Vector3 hitPoint = ray.origin + ray.direction * t;
        Vector3 local = gameScreen.InverseTransformPoint(hitPoint);
        if (Mathf.Abs(local.x) > 0.5f || Mathf.Abs(local.y) > 0.5f) return;

        float u = local.x + 0.5f;
        float v = local.y + 0.5f;

        ShootCubeBullet.Fire(Camera.main.transform.position, hitPoint, gameScreen, u, v, gameWorld);
    }

    static Material MakeUnlitMaterial(Color color)
    {
        string[] names = {
            "Universal Render Pipeline/Unlit",
            "Unlit/Color",
            "Sprites/Default"
        };
        foreach (var n in names)
        {
            Shader s = Shader.Find(n);
            if (s != null)
            {
                Material mat = new Material(s);
                mat.color = color;
                return mat;
            }
        }
        return new Material(Shader.Find("Sprites/Default")) { color = color };
    }
}

/// <summary>
/// 3D cube bullet that flies from the controller to the screen.
/// On arrival, spawns a 2D projectile in the game world.
/// </summary>
public class ShootCubeBullet : MonoBehaviour
{
    Vector3 target;
    Transform screen;
    float u, v;
    Transform gameWorld;
    float speed = 20f;

    public static void Fire(Vector3 from, Vector3 to, Transform screen, float u, float v, Transform gameWorld)
    {
        GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cube.name = "CubeBullet";
        cube.transform.position = from;
        cube.transform.localScale = Vector3.one * 0.08f;
        Object.Destroy(cube.GetComponent<Collider>());

        Renderer rend = cube.GetComponent<Renderer>();
        Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
        if (shader == null) shader = Shader.Find("Unlit/Color");
        if (shader == null) shader = Shader.Find("Sprites/Default");
        rend.material = new Material(shader) { color = new Color(1f, 0.6f, 0f, 1f) };

        ShootCubeBullet bullet = cube.AddComponent<ShootCubeBullet>();
        bullet.target = to;
        bullet.screen = screen;
        bullet.u = u;
        bullet.v = v;
        bullet.gameWorld = gameWorld;
    }

    void Update()
    {
        Vector3 dir = (target - transform.position);
        float dist = dir.magnitude;

        if (dist < 0.1f)
        {
            OnHitScreen();
            Destroy(gameObject);
            return;
        }

        transform.position += dir.normalized * speed * Time.deltaTime;
        transform.Rotate(300f * Time.deltaTime, 200f * Time.deltaTime, 0f);
        speed = Mathf.Max(speed, dist * 5f);
    }

    void OnHitScreen()
    {
        if (screen != null)
        {
            GameObject fx = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            fx.name = "HitFX";
            fx.transform.position = target - screen.forward * 0.02f;
            fx.transform.localScale = Vector3.one * 0.1f;
            Object.Destroy(fx.GetComponent<Collider>());
            Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null) shader = Shader.Find("Unlit/Color");
            if (shader == null) shader = Shader.Find("Sprites/Default");
            fx.GetComponent<Renderer>().material = new Material(shader) { color = new Color(1f, 1f, 0.3f, 1f) };
            fx.AddComponent<ScreenHitVFX>();
        }

        if (GodGameManager.Instance != null && GodGameManager.Instance.gameCamera2D != null)
        {
            Vector3 world2D = GodGameManager.Instance.gameCamera2D.ViewportToWorldPoint(
                new Vector3(u, v, 0f));
            Vector3 local2D = gameWorld != null
                ? gameWorld.InverseTransformPoint(new Vector3(world2D.x, world2D.y, gameWorld.position.z))
                : world2D;
            Vector2 dir2D = new Vector2(Random.Range(-0.15f, 0.15f), -1f).normalized;
            Projectile2D.Create(new Vector2(local2D.x, local2D.y), dir2D, gameWorld);
        }
    }
}

public class ScreenHitVFX : MonoBehaviour
{
    float elapsed;
    float duration = 0.3f;
    Renderer rend;

    void Start()
    {
        rend = GetComponent<Renderer>();
        Destroy(gameObject, duration + 0.05f);
    }

    void Update()
    {
        elapsed += Time.deltaTime;
        float t = Mathf.Clamp01(elapsed / duration);
        transform.localScale = Vector3.one * Mathf.Lerp(0.08f, 0.2f, t);
        if (rend != null)
        {
            Color c = rend.material.color;
            c.a = Mathf.Lerp(1f, 0f, t);
            rend.material.color = c;
        }
    }
}
