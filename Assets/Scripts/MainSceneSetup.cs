using System.Collections;
using UnityEngine;

/// <summary>
/// MainScene setup: same god-game VR infrastructure as BasicScene
/// (GodGameWorldSetup) but builds the 2D world using Level 1's
/// platform configuration adapted to 2D.
///
/// Level 1 layout (3D → 2D adaptation):
///   - Wide ground floor
///   - Platform at (5.92, 5) — 3×0.5
///   - Platform at (0, 8.74) — 3×0.5
///   - Angry pillar at (-5.29, 4.17) — tall 3.37×7.47 (red, polarity-themed)
///   - Hero at (0, 2.85) with Level 1 movement params
///   - Draggable platforms + enemies + VR controls
/// </summary>
public class MainSceneSetup : MonoBehaviour
{
    [Header("VR Screen")]
    public float screenDistance = 3f;
    public float screenWidth = 4f;
    public float screenHeight = 2.25f;
    public float screenY = 1.5f;

    [Header("2D World")]
    public float worldOffsetY = -50f;
    public float orthoSize = 6f; // Slightly larger than BasicScene to fit Level 1's tall layout

    void Start()
    {
        Build();
    }

    void Build()
    {
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

        // Small table in front of player
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
        // 3. 2D GAME WORLD — Level 1 Layout
        // ================================================================
        GameObject world = new GameObject("GameWorld2D");
        world.transform.position = new Vector3(0f, worldOffsetY, 0f);

        // --- Ortho Camera (raised to y=4 to frame Level 1's taller layout) ---
        GameObject camObj = new GameObject("GameCamera2D");
        camObj.transform.SetParent(world.transform);
        camObj.transform.localPosition = new Vector3(0f, 4f, -10f);
        Camera cam = camObj.AddComponent<Camera>();
        cam.orthographic = true;
        cam.orthographicSize = orthoSize;
        cam.nearClipPlane = 0.1f;
        cam.farClipPlane = 50f;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.08f, 0.08f, 0.18f);
        cam.depth = -10;

        // --- Ground (wide floor, matching Level 1's large ground) ---
        Geo2D.MakePlatform(new Vector3(0f, -0.5f, 0f), 20f, 1f,
            new Color(0.25f, 0.65f, 0.25f), world.transform, 0);

        // --- Level 1 Static Platforms ---
        // Platform at (5.92, 5) — 3 wide, 0.5 tall (right, mid-height)
        Geo2D.MakePlatform(new Vector3(5.92f, 5f, 0f), 3f, 0.5f,
            new Color(0.55f, 0.4f, 0.3f), world.transform, 1);

        // Platform at (0, 8.74) — 3 wide, 0.5 tall (center, high)
        Geo2D.MakePlatform(new Vector3(0f, 8.74f, 0f), 3f, 0.5f,
            new Color(0.55f, 0.4f, 0.3f), world.transform, 1);

        // Angry Pillar at (-5.29, 4.17) — tall column (Level 1 PlatformAngry)
        // 3D scale (0.55, 7.47, 3.37) → 2D: width 3.37 (Z→X), height 7.47
        Geo2D.MakePlatform(new Vector3(-5.29f, 4.17f, 0f), 3.37f, 7.47f,
            new Color(0.85f, 0.2f, 0.2f), world.transform, 1);

        // --- Draggable Platforms (god power) ---
        GameObject dragPlat1 = Geo2D.MakePlatform(new Vector3(0f, 2f, 0f), 2.5f, 0.35f,
            new Color(0f, 0.9f, 0.9f), world.transform, 1);
        DraggablePlatform2D dp1 = dragPlat1.AddComponent<DraggablePlatform2D>();
        dp1.minX = -7f;
        dp1.maxX = 7f;
        dragPlat1.name = "DragPlatform_Low";

        GameObject dragPlat2 = Geo2D.MakePlatform(new Vector3(3f, 6.5f, 0f), 2f, 0.35f,
            new Color(0f, 0.9f, 0.9f), world.transform, 1);
        DraggablePlatform2D dp2 = dragPlat2.AddComponent<DraggablePlatform2D>();
        dp2.minX = -7f;
        dp2.maxX = 7f;
        dragPlat2.name = "DragPlatform_Mid";

        // --- Walls ---
        Geo2D.MakePlatform(new Vector3(-10f, 5f, 0f), 0.5f, 14f,
            new Color(0.35f, 0.35f, 0.35f), world.transform, 0);
        Geo2D.MakePlatform(new Vector3(10f, 5f, 0f), 0.5f, 14f,
            new Color(0.35f, 0.35f, 0.35f), world.transform, 0);

        // --- Decorative 2D geometry ---
        Geo2D.Make("Star1", Geo2D.Star(), new Color(1f, 1f, 0.6f),
            new Vector3(-7f, 9f, 0f), Vector2.one * 0.3f, false, false, -1, world.transform);
        Geo2D.Make("Star2", Geo2D.Star(), new Color(1f, 0.9f, 0.5f),
            new Vector3(4f, 10f, 0f), Vector2.one * 0.25f, false, false, -1, world.transform);
        Geo2D.Make("Star3", Geo2D.Star(), new Color(0.9f, 0.95f, 1f),
            new Vector3(8f, 8f, 0f), Vector2.one * 0.2f, false, false, -1, world.transform);

        Geo2D.Make("Gem1", Geo2D.Diamond(), new Color(0f, 1f, 0.7f),
            new Vector3(5.92f, 6f, 0f), Vector2.one * 0.35f, false, false, 2, world.transform);
        Geo2D.Make("Gem2", Geo2D.Diamond(), new Color(1f, 0.3f, 0.8f),
            new Vector3(0f, 9.5f, 0f), Vector2.one * 0.35f, false, false, 2, world.transform);

        Geo2D.Make("Spike1", Geo2D.Triangle(), new Color(0.9f, 0.2f, 0.2f),
            new Vector3(2f, 0.4f, 0f), Vector2.one * 0.5f, false, true, 2, world.transform);
        Geo2D.Make("Spike2", Geo2D.Triangle(), new Color(0.9f, 0.2f, 0.2f),
            new Vector3(2.6f, 0.4f, 0f), Vector2.one * 0.5f, false, true, 2, world.transform);
        Geo2D.Make("Spike3", Geo2D.Triangle(), new Color(0.9f, 0.2f, 0.2f),
            new Vector3(3.2f, 0.4f, 0f), Vector2.one * 0.5f, false, true, 2, world.transform);

        // --- Hero (Level 1 spawn: (0, 2.85), adapted movement) ---
        GameObject hero = MakeHero(world.transform);
        PlatformerCharacter2D heroController = hero.GetComponent<PlatformerCharacter2D>();

        // ================================================================
        // 4. SPAWN & DROP (test)
        // ================================================================
        SpawnAndDrop spawner = world.AddComponent<SpawnAndDrop>();
        spawner.spawnHeight = 10f;
        spawner.spawnRangeX = 8f;

        // ================================================================
        // 5. GOD GAME MANAGER
        // ================================================================
        GodGameManager gm = gameObject.AddComponent<GodGameManager>();
        gm.gameScreen = screen.transform;
        gm.gameCamera2D = cam;

        // ================================================================
        // 6. GOD SCREEN SHOOTER
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
        // 8. VR CHARACTER CONTROLLER
        // ================================================================
        VRCharacterController vrController = gameObject.AddComponent<VRCharacterController>();
        vrController.character = heroController;

        Debug.Log("=== MAIN SCENE (Level 1) READY (waiting for VR controllers...) ===");

        // ================================================================
        // 9. WAIT FOR RIGHT CONTROLLER
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
                GameObject xrOrigin = GameObject.Find("XR Origin (XR Rig)");
                if (xrOrigin != null)
                {
                    var modalityManager = xrOrigin.GetComponent<UnityEngine.XR.Interaction.Toolkit.Inputs.XRInputModalityManager>();
                    if (modalityManager != null)
                    {
                        modalityManager.enabled = false;
                        Debug.Log("[MainScene] Disabled XRInputModalityManager to keep controllers active");
                    }
                }

                rightCtrl.gameObject.SetActive(true);

                DisableRightHandInteractors(rightCtrl.gameObject);
                shooter.vrRayOrigin = rightCtrl;
                dragger.vrRayOrigin = rightCtrl;
                Debug.Log($"[MainScene] RIGHT CONTROLLER FOUND after {elapsed:F1}s - shooting + dragging enabled!");
                yield break;
            }
            elapsed += 0.5f;
            yield return new WaitForSeconds(0.5f);
        }

        Debug.LogWarning("[MainScene] Right Controller never appeared after 15s - mouse fallback only");
    }

    Transform FindRightControllerTransform()
    {
        GameObject go = GameObject.Find("Right Controller");
        if (go != null) return go.transform;

        GameObject xrOrigin = GameObject.Find("XR Origin (XR Rig)");
        if (xrOrigin != null)
        {
            Transform found = xrOrigin.transform.Find("Camera Offset/Right Controller");
            if (found != null) return found;
        }

        return null;
    }

    void DisableRightHandInteractors(GameObject rightController)
    {
        if (rightController == null) return;

        var selfBehaviours = rightController.GetComponents<MonoBehaviour>();
        foreach (var b in selfBehaviours)
        {
            string typeName = b.GetType().Name;
            if (typeName != "TrackedPoseDriver")
            {
                b.enabled = false;
            }
        }

        var interactors = rightController.GetComponentsInChildren<UnityEngine.XR.Interaction.Toolkit.Interactors.XRBaseInteractor>(true);
        foreach (var interactor in interactors)
            interactor.enabled = false;

        var lineRenderers = rightController.GetComponentsInChildren<LineRenderer>(true);
        foreach (var lr in lineRenderers)
            lr.enabled = false;

        var lineVisuals = rightController.GetComponentsInChildren<UnityEngine.XR.Interaction.Toolkit.Interactors.Visuals.XRInteractorLineVisual>(true);
        foreach (var lv in lineVisuals)
            lv.enabled = false;
    }

    GameObject MakeHero(Transform parent)
    {
        GameObject hero = new GameObject("Hero");
        hero.transform.SetParent(parent);
        hero.transform.localPosition = new Vector3(0f, 2.85f, 0f); // Level 1 spawn
        hero.transform.localScale = new Vector3(0.7f, 0.7f, 1f);

        SpriteRenderer sr = hero.AddComponent<SpriteRenderer>();
        sr.sprite = Geo2D.Circle();
        sr.color = new Color(0.2f, 0.55f, 1f);
        sr.sortingOrder = 10;

        Rigidbody2D rb = hero.AddComponent<Rigidbody2D>();
        rb.freezeRotation = true;
        rb.gravityScale = 5f; // Level 1 gravityMultiplier
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        CircleCollider2D col = hero.AddComponent<CircleCollider2D>();

        PlatformerCharacter2D pc = hero.AddComponent<PlatformerCharacter2D>();
        pc.moveSpeed = 10f;  // Level 1 moveSpeed
        pc.jumpForce = 17f;  // Scaled for gravityScale 5: sqrt(2*9.81*5*3) ≈ 17

        return hero;
    }
}
