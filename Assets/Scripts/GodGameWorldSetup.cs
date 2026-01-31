using System.Collections;
using UnityEngine;

/// <summary>
/// One-click setup: attach to an empty GameObject, hit Play, and the entire
/// God Game world is constructed at runtime.
///
/// Creates:
///   1. VR environment (3D): screen frame, table, pillars, floor
///   2. VR Screen (Quad) displaying the 2D game via RenderTexture
///   3. 2D game world (offset far below VR space)
///   4. SpawnAndDrop test system (press Space)
///   5. GodScreenShooter (right trigger shoots at screen)
///   6. GodGameManager wiring everything together
///   7. EnemySpawner + VRCharacterController
///
/// Disables XRI interactors on the right hand so the trigger
/// is exclusively used for our custom shooting system.
/// </summary>
public class GodGameWorldSetup : MonoBehaviour
{
    [Header("VR Screen")]
    public float screenDistance = 3f;
    public float screenWidth = 4f;
    public float screenHeight = 2.25f;   // 16:9
    public float screenY = 1.5f;         // Height of screen center in VR

    [Header("2D World")]
    public float worldOffsetY = -50f;     // Push 2D world far below VR
    public float orthoSize = 5f;

    void Start()
    {
        Build();
    }

    void Build()
    {
        // Debug: check which shaders are available
        string[] testShaders = { "Universal Render Pipeline/Lit", "Universal Render Pipeline/Simple Lit", "Standard", "Unlit/Color" };
        foreach (string s in testShaders)
            Debug.Log($"[GodGame] Shader.Find(\"{s}\") = {(Shader.Find(s) != null ? "FOUND" : "NULL")}");

        // ================================================================
        // 1. VR ENVIRONMENT (3D objects around the player)
        // ================================================================
        GameObject vrEnv = new GameObject("VR_Environment");

        // Floor
        Geo3D.Cube(new Vector3(0f, 0f, 0f), new Vector3(10f, 0.05f, 10f),
            new Color(0.2f, 0.2f, 0.25f), vrEnv.transform);

        // Screen frame
        Geo3D.Frame(
            new Vector3(0f, screenY, screenDistance),
            screenWidth, screenHeight,
            0.08f, 0.1f,
            new Color(0.15f, 0.15f, 0.15f),
            vrEnv.transform
        );

        // Two pillars flanking the screen
        Geo3D.Pillar(new Vector3(-screenWidth * 0.5f - 0.5f, 1f, screenDistance),
            0.15f, 2f, new Color(0.35f, 0.3f, 0.25f), vrEnv.transform);
        Geo3D.Pillar(new Vector3(screenWidth * 0.5f + 0.5f, 1f, screenDistance),
            0.15f, 2f, new Color(0.35f, 0.3f, 0.25f), vrEnv.transform);

        // Small table in front of player (to place controller reference)
        Geo3D.Table(new Vector3(0f, 0f, 1f), 0.8f, 0.5f, 0.7f,
            new Color(0.45f, 0.3f, 0.2f), new Color(0.3f, 0.2f, 0.15f), vrEnv.transform);

        // Decorative spheres on sides
        Geo3D.Sphere(new Vector3(-2f, 0.3f, 2f), 0.3f, new Color(0.8f, 0.2f, 0.2f), vrEnv.transform);
        Geo3D.Sphere(new Vector3(2f, 0.3f, 2f), 0.3f, new Color(0.2f, 0.4f, 0.9f), vrEnv.transform);

        // Ramp for VR flavor
        Geo3D.Ramp(new Vector3(-3f, 0f, 3f), 1f, 0.5f, 1.5f,
            new Color(0.5f, 0.5f, 0.55f), vrEnv.transform);

        // ================================================================
        // 2. GAME SCREEN (VR Quad)
        // ================================================================
        GameObject screen = GameObject.CreatePrimitive(PrimitiveType.Quad);
        screen.name = "GameScreen";
        screen.transform.position = new Vector3(0f, screenY, screenDistance);
        screen.transform.localScale = new Vector3(screenWidth, screenHeight, 1f);

        // ================================================================
        // 3. 2D GAME WORLD
        // ================================================================
        GameObject world = new GameObject("GameWorld2D");
        world.transform.position = new Vector3(0f, worldOffsetY, 0f);

        // --- Ortho Camera ---
        GameObject camObj = new GameObject("GameCamera2D");
        camObj.transform.SetParent(world.transform);
        camObj.transform.localPosition = new Vector3(0f, 3f, -10f);
        Camera cam = camObj.AddComponent<Camera>();
        cam.orthographic = true;
        cam.orthographicSize = orthoSize;
        cam.nearClipPlane = 0.1f;
        cam.farClipPlane = 50f;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.08f, 0.08f, 0.18f);
        cam.depth = -10;

        // --- Ground (thick green rectangle) ---
        Geo2D.MakePlatform(new Vector3(0f, -0.5f, 0f), 16f, 1f,
            new Color(0.25f, 0.65f, 0.25f), world.transform, 0);

        // --- Floating Platforms ---
        Geo2D.MakePlatform(new Vector3(-3f, 2f, 0f), 2.5f, 0.35f,
            new Color(0.55f, 0.4f, 0.3f), world.transform, 1);
        Geo2D.MakePlatform(new Vector3(2.5f, 3.5f, 0f), 3f, 0.35f,
            new Color(0.55f, 0.4f, 0.3f), world.transform, 1);
        Geo2D.MakePlatform(new Vector3(-1f, 5.5f, 0f), 2f, 0.35f,
            new Color(0.55f, 0.4f, 0.3f), world.transform, 1);
        Geo2D.MakePlatform(new Vector3(4.5f, 6f, 0f), 2f, 0.35f,
            new Color(0.6f, 0.45f, 0.35f), world.transform, 1);

        // --- Draggable Platforms (god power) ---
        GameObject dragPlat1 = Geo2D.MakePlatform(new Vector3(0f, 1.5f, 0f), 2.5f, 0.35f,
            new Color(0f, 0.9f, 0.9f), world.transform, 1);
        DraggablePlatform2D dp1 = dragPlat1.AddComponent<DraggablePlatform2D>();
        dp1.minX = -7f;
        dp1.maxX = 7f;
        dragPlat1.name = "DragPlatform_Low";

        GameObject dragPlat2 = Geo2D.MakePlatform(new Vector3(3f, 4.5f, 0f), 2f, 0.35f,
            new Color(0f, 0.9f, 0.9f), world.transform, 1);
        DraggablePlatform2D dp2 = dragPlat2.AddComponent<DraggablePlatform2D>();
        dp2.minX = -7f;
        dp2.maxX = 7f;
        dragPlat2.name = "DragPlatform_Mid";

        // --- Walls ---
        Geo2D.MakePlatform(new Vector3(-8.5f, 4f, 0f), 0.5f, 12f,
            new Color(0.35f, 0.35f, 0.35f), world.transform, 0);
        Geo2D.MakePlatform(new Vector3(8.5f, 4f, 0f), 0.5f, 12f,
            new Color(0.35f, 0.35f, 0.35f), world.transform, 0);

        // --- Decorative 2D geometry ---
        Geo2D.Make("Star1", Geo2D.Star(), new Color(1f, 1f, 0.6f),
            new Vector3(-5f, 8f, 0f), Vector2.one * 0.3f, false, false, -1, world.transform);
        Geo2D.Make("Star2", Geo2D.Star(), new Color(1f, 0.9f, 0.5f),
            new Vector3(3f, 9f, 0f), Vector2.one * 0.25f, false, false, -1, world.transform);
        Geo2D.Make("Star3", Geo2D.Star(), new Color(0.9f, 0.95f, 1f),
            new Vector3(6f, 7.5f, 0f), Vector2.one * 0.2f, false, false, -1, world.transform);

        Geo2D.Make("Gem1", Geo2D.Diamond(), new Color(0f, 1f, 0.7f),
            new Vector3(-2f, 3.5f, 0f), Vector2.one * 0.35f, false, false, 2, world.transform);
        Geo2D.Make("Gem2", Geo2D.Diamond(), new Color(1f, 0.3f, 0.8f),
            new Vector3(3.5f, 5f, 0f), Vector2.one * 0.35f, false, false, 2, world.transform);

        Geo2D.Make("Spike1", Geo2D.Triangle(), new Color(0.9f, 0.2f, 0.2f),
            new Vector3(1f, 0.4f, 0f), Vector2.one * 0.5f, false, true, 2, world.transform);
        Geo2D.Make("Spike2", Geo2D.Triangle(), new Color(0.9f, 0.2f, 0.2f),
            new Vector3(1.6f, 0.4f, 0f), Vector2.one * 0.5f, false, true, 2, world.transform);

        Geo2D.Make("HexBlock", Geo2D.Hexagon(), new Color(0.6f, 0.3f, 0.8f),
            new Vector3(5f, 1.5f, 0f), Vector2.one * 0.8f, false, true, 2, world.transform);

        // --- Hero ---
        GameObject hero = MakeHero(world.transform);
        PlatformerCharacter2D heroController = hero.GetComponent<PlatformerCharacter2D>();

        // ================================================================
        // 4. SPAWN & DROP (test)
        // ================================================================
        SpawnAndDrop spawner = world.AddComponent<SpawnAndDrop>();
        spawner.spawnHeight = 8f;
        spawner.spawnRangeX = 6f;

        // ================================================================
        // 5. GOD GAME MANAGER
        // ================================================================
        GodGameManager gm = gameObject.AddComponent<GodGameManager>();
        gm.gameScreen = screen.transform;
        gm.gameCamera2D = cam;

        // ================================================================
        // 6. GOD SCREEN SHOOTER (vrRayOrigin wired later when controller appears)
        // ================================================================
        GodScreenShooter shooter = gameObject.AddComponent<GodScreenShooter>();
        shooter.gameScreen = screen.transform;
        shooter.gameWorld = world.transform;

        // ================================================================
        // 6b. GOD PLATFORM DRAGGER (grip to drag platforms)
        // ================================================================
        GodPlatformDragger dragger = gameObject.AddComponent<GodPlatformDragger>();
        dragger.gameScreen = screen.transform;
        dragger.gameWorld = world.transform;
        shooter.platformDragger = dragger;

        // ================================================================
        // 7. ENEMY SPAWNER
        // ================================================================
        EnemySpawner enemySpawner = world.AddComponent<EnemySpawner>();
        enemySpawner.heroTarget = hero.transform;

        // ================================================================
        // 8. VR CHARACTER CONTROLLER (disables XR locomotion automatically)
        // ================================================================
        VRCharacterController vrController = gameObject.AddComponent<VRCharacterController>();
        vrController.character = heroController;

        Debug.Log("=== GOD GAME WORLD READY (waiting for VR controllers...) ===");

        // ================================================================
        // 9. WAIT FOR RIGHT CONTROLLER (XR rig initializes late)
        // ================================================================
        StartCoroutine(WaitForRightController(shooter, dragger));
    }

    IEnumerator WaitForRightController(GodScreenShooter shooter, GodPlatformDragger dragger)
    {
        float timeout = 15f;
        float elapsed = 0f;

        while (elapsed < timeout)
        {
            Transform rightCtrl = FindRightControllerTransform();
            if (rightCtrl != null)
            {
                // Disable XRInputModalityManager so it doesn't deactivate our controller
                GameObject xrOrigin = GameObject.Find("XR Origin (XR Rig)");
                if (xrOrigin != null)
                {
                    var modalityManager = xrOrigin.GetComponent<UnityEngine.XR.Interaction.Toolkit.Inputs.XRInputModalityManager>();
                    if (modalityManager != null)
                    {
                        modalityManager.enabled = false;
                        Debug.Log("[GodGame] Disabled XRInputModalityManager to keep controllers active");
                    }
                }

                // Ensure controller is active (may have been deactivated by modality manager)
                rightCtrl.gameObject.SetActive(true);

                DisableRightHandInteractors(rightCtrl.gameObject);
                shooter.vrRayOrigin = rightCtrl;
                dragger.vrRayOrigin = rightCtrl;
                Debug.Log($"[GodGame] RIGHT CONTROLLER FOUND after {elapsed:F1}s - shooting + dragging enabled! active={rightCtrl.gameObject.activeSelf}");
                yield break;
            }
            elapsed += 0.5f;
            yield return new WaitForSeconds(0.5f);
        }

        Debug.LogWarning("[GodGame] Right Controller never appeared after 15s - mouse fallback only");
    }

    /// <summary>
    /// Finds the Right Controller transform even if it's inactive.
    /// GameObject.Find only works on active objects, so we use Transform.Find via the XR Origin hierarchy.
    /// </summary>
    Transform FindRightControllerTransform()
    {
        // Try active first (fast path)
        GameObject go = GameObject.Find("Right Controller");
        if (go != null) return go.transform;

        // Search via hierarchy (works for inactive objects)
        GameObject xrOrigin = GameObject.Find("XR Origin (XR Rig)");
        if (xrOrigin != null)
        {
            Transform found = xrOrigin.transform.Find("Camera Offset/Right Controller");
            if (found != null) return found;
        }

        return null;
    }

    /// <summary>
    /// Disables all XRI interactors on the right hand controller so the
    /// trigger input is exclusively available for our custom shooting system.
    /// </summary>
    void DisableRightHandInteractors(GameObject rightController)
    {
        if (rightController == null)
        {
            Debug.Log("[GodGame] No Right Controller found - skipping interactor disable");
            return;
        }

        // Disable ALL MonoBehaviour components on the Right Controller itself
        // (ControllerInputActionManager, XRInteractionGroup, etc.)
        // These intercept trigger input before our custom InputAction can read it.
        // Keep only TrackedPoseDriver so the controller still tracks position/rotation.
        var selfBehaviours = rightController.GetComponents<MonoBehaviour>();
        foreach (var b in selfBehaviours)
        {
            string typeName = b.GetType().Name;
            if (typeName != "TrackedPoseDriver")
            {
                b.enabled = false;
                Debug.Log($"[GodGame] Disabled right hand component: {typeName}");
            }
        }

        // Disable all XRI interactors in children (Near-Far, Poke, Teleport, etc.)
        var interactors = rightController.GetComponentsInChildren<UnityEngine.XR.Interaction.Toolkit.Interactors.XRBaseInteractor>(true);
        foreach (var interactor in interactors)
        {
            interactor.enabled = false;
            Debug.Log($"[GodGame] Disabled right hand interactor: {interactor.GetType().Name} on {interactor.gameObject.name}");
        }

        // Also disable line renderers from XRI ray visuals (we draw our own laser)
        var lineRenderers = rightController.GetComponentsInChildren<LineRenderer>(true);
        foreach (var lr in lineRenderers)
        {
            lr.enabled = false;
        }

        // Disable XRInteractorLineVisual components
        var lineVisuals = rightController.GetComponentsInChildren<UnityEngine.XR.Interaction.Toolkit.Interactors.Visuals.XRInteractorLineVisual>(true);
        foreach (var lv in lineVisuals)
        {
            lv.enabled = false;
        }

        // NOTE: Keep InputActionManager enabled - disabling it kills ALL XR input
        // including our custom trigger bindings. XRI interactors are already disabled above.
    }


    GameObject MakeHero(Transform parent)
    {
        GameObject hero = new GameObject("Hero");
        hero.transform.SetParent(parent);
        hero.transform.localPosition = new Vector3(-3f, 2f, 0f);
        hero.transform.localScale = new Vector3(0.7f, 0.7f, 1f);

        SpriteRenderer sr = hero.AddComponent<SpriteRenderer>();
        sr.sprite = Geo2D.Circle();
        sr.color = new Color(0.2f, 0.55f, 1f);
        sr.sortingOrder = 10;

        Rigidbody2D rb = hero.AddComponent<Rigidbody2D>();
        rb.freezeRotation = true;
        rb.gravityScale = 3f;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        CircleCollider2D col = hero.AddComponent<CircleCollider2D>();

        PlatformerCharacter2D pc = hero.AddComponent<PlatformerCharacter2D>();
        pc.moveSpeed = 3f;
        pc.jumpForce = 10f;

        return hero;
    }
}
