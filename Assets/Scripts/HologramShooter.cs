using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR;

/// <summary>
/// VR right-trigger shoots directly into the 3D stage diorama.
/// No UV conversion needed — raycasts directly into the miniature world.
/// Uses QueryTriggerInteraction.Collide to hit FlyingEnemy trigger colliders.
/// </summary>
public class HologramShooter : MonoBehaviour
{
    [Header("VR Input")]
    public Transform vrRayOrigin;

    [Header("Aim")]
    [Range(-90f, 90f)]
    public float aimTiltX = 0f;

    [Header("Shooting")]
    public float cooldown = 0.01f;
    public float maxRayDist = 3000f;

    [Header("Bullet")]
    public float bulletSpeed = 150f;
    public float bulletSize = 2.5f;
    public float bulletLifetime = 3f;   // seconds
    public Color bulletColor = new Color(1f, 0.6f, 0.1f, 1f);

    [Header("Polarity (Right Thumbstick)")]
    public float stickDeadZone = 0.5f;

    float lastShotTime;
    bool triggerWasDown;
    bool stickWasRight;

    InputAction runtimeTrigger;
    InputAction runtimeThumbstick;

    // Visuals
    LineRenderer laserLine;
    GameObject aimDot;
    bool visualsReady;

    Vector3 GetAimDirection()
    {
        return Quaternion.AngleAxis(aimTiltX, vrRayOrigin.right) * vrRayOrigin.forward;
    }

    void SetupVisuals()
    {
        if (visualsReady) return;
        visualsReady = true;

        GameObject laserObj = new GameObject("HologramLaser");
        laserLine = laserObj.AddComponent<LineRenderer>();
        laserLine.positionCount = 2;
        laserLine.startWidth = 0.05f;
        laserLine.endWidth = 0.02f;
        laserLine.material = MakeUnlitMaterial(new Color(1f, 0.3f, 0.1f, 1f));
        laserLine.startColor = new Color(1f, 0.3f, 0.1f, 1f);
        laserLine.endColor = new Color(1f, 0.8f, 0.2f, 1f);
        laserLine.useWorldSpace = true;
        laserLine.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        laserLine.receiveShadows = false;

        aimDot = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        aimDot.name = "AimDot";
        Object.Destroy(aimDot.GetComponent<Collider>());
        aimDot.transform.localScale = Vector3.one * 0.3f;
        aimDot.GetComponent<Renderer>().material = MakeUnlitMaterial(new Color(1f, 0f, 0f, 1f));
        aimDot.SetActive(false);
    }

    void OnEnable()
    {
        runtimeTrigger = new InputAction("RightTrigger", InputActionType.Value);
        runtimeTrigger.AddBinding("<XRController>{RightHand}/trigger");
        runtimeTrigger.AddBinding("<XRController>{RightHand}/triggerPressed");
        runtimeTrigger.AddBinding("<OculusTouchController>{RightHand}/trigger");
        runtimeTrigger.AddBinding("<MetaQuestTouchProController>{RightHand}/trigger");
        runtimeTrigger.Enable();

        runtimeThumbstick = new InputAction("RightThumbstick", InputActionType.Value);
        runtimeThumbstick.AddBinding("<XRController>{RightHand}/thumbstick");
        runtimeThumbstick.Enable();
    }

    void OnDisable()
    {
        if (runtimeTrigger != null)
        {
            runtimeTrigger.Disable();
            runtimeTrigger.Dispose();
            runtimeTrigger = null;
        }
        if (runtimeThumbstick != null)
        {
            runtimeThumbstick.Disable();
            runtimeThumbstick.Dispose();
            runtimeThumbstick = null;
        }
    }

    void Update()
    {
        if (vrRayOrigin != null && !visualsReady) SetupVisuals();

        // Update laser visual
        if (vrRayOrigin != null && laserLine != null)
        {
            Vector3 origin = vrRayOrigin.position;
            Vector3 aimDir = GetAimDirection();

            if (Physics.Raycast(new Ray(origin, aimDir), out RaycastHit laserHit, maxRayDist, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Collide))
            {
                laserLine.enabled = true;
                laserLine.SetPosition(0, origin);
                laserLine.SetPosition(1, laserHit.point);

                if (aimDot != null)
                {
                    aimDot.SetActive(true);
                    aimDot.transform.position = laserHit.point;
                }
            }
            else
            {
                laserLine.enabled = true;
                laserLine.SetPosition(0, origin);
                laserLine.SetPosition(1, origin + aimDir * maxRayDist);

                if (aimDot != null)
                    aimDot.SetActive(false);
            }
        }

        // Read trigger
        float inputSystemVal = runtimeTrigger != null ? runtimeTrigger.ReadValue<float>() : 0f;
        float xrSubsystemVal = ReadRightTriggerXR();
        float triggerVal = Mathf.Max(inputSystemVal, xrSubsystemVal);

        // Fire on rising edge
        if (triggerVal > 0.3f && !triggerWasDown)
        {
            triggerWasDown = true;
            TryShootVR();
        }
        else if (triggerVal < 0.15f)
        {
            triggerWasDown = false;
        }

        // Desktop mouse fallback: left click
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            TryShootMouse();
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

    void TryShootVR()
    {
        if (Time.time - lastShotTime < cooldown) return;
        if (vrRayOrigin == null) return;
        lastShotTime = Time.time;

        Vector3 origin = vrRayOrigin.position;
        Vector3 aimDir = GetAimDirection();
        SpawnBullet(origin, aimDir);
    }

    void TryShootMouse()
    {
        if (Time.time - lastShotTime < cooldown) return;
        if (Camera.main == null) return;
        lastShotTime = Time.time;

        Vector2 mousePos = Mouse.current.position.ReadValue();
        Ray ray = Camera.main.ScreenPointToRay(new Vector3(mousePos.x, mousePos.y, 0f));
        SpawnBullet(ray.origin, ray.direction);
    }

    void SpawnBullet(Vector3 origin, Vector3 direction)
    {
        GameObject bullet = GameObject.CreatePrimitive(PrimitiveType.Cube);
        bullet.name = "Bullet";
        bullet.transform.position = origin;
        bullet.transform.localScale = Vector3.one * bulletSize;
        bullet.transform.rotation = Quaternion.LookRotation(direction);
        bullet.layer = LayerMask.NameToLayer("Ignore Raycast");

        // Make collider a trigger for reliable trigger-trigger detection
        bullet.GetComponent<Collider>().isTrigger = true;

        // Material
        bullet.GetComponent<Renderer>().material = MakeUnlitMaterial(bulletColor);

        // Physics — Rigidbody with velocity, no gravity
        Rigidbody rb = bullet.AddComponent<Rigidbody>();
        rb.useGravity = false;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        rb.linearVelocity = direction.normalized * bulletSpeed;

        // Bullet behaviour
        HologramBullet hb = bullet.AddComponent<HologramBullet>();
        hb.lifetime = bulletLifetime;
    }

    void SpawnHitVFX(Vector3 pos)
    {
        GameObject fx = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        fx.name = "HitVFX";
        fx.transform.position = pos;
        fx.transform.localScale = Vector3.one * 3f;
        Object.Destroy(fx.GetComponent<Collider>());
        fx.GetComponent<Renderer>().material = MakeUnlitMaterial(new Color(1f, 1f, 0.3f, 1f));
        fx.AddComponent<HologramHitVFX>();
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

public class HologramBullet : MonoBehaviour
{
    public float lifetime = 3f;

    void Start()
    {
        Destroy(gameObject, lifetime);
    }

    void OnTriggerEnter(Collider other)
    {
        FlyingEnemy enemy = other.GetComponentInParent<FlyingEnemy>();
        if (enemy != null)
        {
            Debug.Log($"[HologramBullet] HIT enemy: {enemy.gameObject.name}");
            // Spawn hit VFX at contact point
            SpawnHitVFX(transform.position);
            Destroy(enemy.gameObject);
            Destroy(gameObject);
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        FlyingEnemy enemy = collision.collider.GetComponentInParent<FlyingEnemy>();
        if (enemy != null)
        {
            Debug.Log($"[HologramBullet] HIT enemy (collision): {enemy.gameObject.name}");
            SpawnHitVFX(transform.position);
            Destroy(enemy.gameObject);
        }
        // Destroy bullet on any solid hit
        Destroy(gameObject);
    }

    void SpawnHitVFX(Vector3 pos)
    {
        GameObject fx = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        fx.name = "HitVFX";
        fx.transform.position = pos;
        fx.transform.localScale = Vector3.one * 3f;
        Object.Destroy(fx.GetComponent<Collider>());
        Shader s = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Color") ?? Shader.Find("Sprites/Default");
        fx.GetComponent<Renderer>().material = new Material(s) { color = new Color(1f, 1f, 0.3f, 1f) };
        fx.AddComponent<HologramHitVFX>();
    }
}

public class HologramHitVFX : MonoBehaviour
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
        transform.localScale = Vector3.one * Mathf.Lerp(3f, 8f, t);
        if (rend != null)
        {
            Color c = rend.material.color;
            c.a = Mathf.Lerp(1f, 0f, t);
            rend.material.color = c;
        }
    }
}
