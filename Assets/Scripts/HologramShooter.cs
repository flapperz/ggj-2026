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
    [Range(0f, 90f)]
    public float aimTiltX = 60f;

    [Header("Shooting")]
    public float cooldown = 0.25f;
    public float maxRayDist = 50f;

    float lastShotTime;
    bool triggerWasDown;

    InputAction runtimeTrigger;

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
        laserLine.startWidth = 0.005f;
        laserLine.endWidth = 0.002f;
        laserLine.material = MakeUnlitMaterial(new Color(1f, 0.3f, 0.1f, 1f));
        laserLine.startColor = new Color(1f, 0.3f, 0.1f, 1f);
        laserLine.endColor = new Color(1f, 0.8f, 0.2f, 1f);
        laserLine.useWorldSpace = true;
        laserLine.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        laserLine.receiveShadows = false;

        aimDot = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        aimDot.name = "AimDot";
        Object.Destroy(aimDot.GetComponent<Collider>());
        aimDot.transform.localScale = Vector3.one * 0.02f;
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

    void Update()
    {
        if (vrRayOrigin != null && !visualsReady) SetupVisuals();

        // Update laser visual
        if (vrRayOrigin != null && laserLine != null)
        {
            Vector3 origin = vrRayOrigin.position;
            Vector3 aimDir = GetAimDirection();

            if (Physics.Raycast(new Ray(origin, aimDir), out RaycastHit laserHit, maxRayDist, ~0, QueryTriggerInteraction.Collide))
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
        Ray ray = new Ray(origin, aimDir);

        if (Physics.Raycast(ray, out RaycastHit hit, maxRayDist, ~0, QueryTriggerInteraction.Collide))
        {
            FlyingEnemy enemy = hit.collider.GetComponentInParent<FlyingEnemy>();
            if (enemy != null)
            {
                Debug.Log($"[HologramShooter] HIT enemy: {enemy.gameObject.name}");
                SpawnHitVFX(hit.point);
                Destroy(enemy.gameObject);
            }
            else
            {
                Debug.Log($"[HologramShooter] Hit surface: {hit.collider.name}");
            }
        }
    }

    void TryShootMouse()
    {
        if (Time.time - lastShotTime < cooldown) return;
        if (Camera.main == null) return;
        lastShotTime = Time.time;

        Vector2 mousePos = Mouse.current.position.ReadValue();
        Ray ray = Camera.main.ScreenPointToRay(new Vector3(mousePos.x, mousePos.y, 0f));

        if (Physics.Raycast(ray, out RaycastHit hit, maxRayDist, ~0, QueryTriggerInteraction.Collide))
        {
            FlyingEnemy enemy = hit.collider.GetComponentInParent<FlyingEnemy>();
            if (enemy != null)
            {
                Debug.Log($"[HologramShooter] Mouse HIT enemy: {enemy.gameObject.name}");
                SpawnHitVFX(hit.point);
                Destroy(enemy.gameObject);
            }
        }
    }

    void SpawnHitVFX(Vector3 pos)
    {
        GameObject fx = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        fx.name = "HitVFX";
        fx.transform.position = pos;
        fx.transform.localScale = Vector3.one * 0.03f;
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
        transform.localScale = Vector3.one * Mathf.Lerp(0.03f, 0.08f, t);
        if (rend != null)
        {
            Color c = rend.material.color;
            c.a = Mathf.Lerp(1f, 0f, t);
            rend.material.color = c;
        }
    }
}
