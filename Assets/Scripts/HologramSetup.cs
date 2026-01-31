using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;

/// <summary>
/// Loads Level 1 additively into a StageRoot/WorldMover hierarchy
/// and wires VR controllers.
/// Scene is in cm (1 unit = 1cm). Level 1 content is scaled by
/// levelScale so it fills the stage (level ~20 units wide → 400cm).
/// Level (0,0,0) maps to StageRoot center.
///
/// Required scene setup:
///   - A GameObject named "StageRoot" (position where the diorama sits)
///     - A child named "WorldMover" (will hold Level 1 content + StageWorldMover)
///   If these don't exist, the script creates them at default positions.
/// </summary>
public class HologramSetup : MonoBehaviour
{
    [Header("Stage Root")]
    [Tooltip("Drag the StageRoot from the scene. If empty, looks for 'StageRoot' by name or creates one.")]
    public Transform stageRoot;

    [Header("Level Scale")]
    [Tooltip("Scale factor for level content. 20 = 1 level unit becomes 20cm. Level ~20 units wide fills a 400cm stage.")]
    public float levelScale = 20f;

    [Header("Stage Layout (cm)")]
    [Tooltip("Where the player appears in stage-local X. 0 = center.")]
    public float stageCenterX = 0f;

    [Tooltip("Y offset to align level ground with stage floor.")]
    public float stageBaseY = -112.5f;

    // Runtime references
    Transform worldMover;
    Player playerRef;

    void Start()
    {
        Build();
    }

    void Build()
    {
        // ================================================================
        // 1. FIND OR CREATE STAGE ROOT
        // ================================================================
        if (stageRoot == null)
        {
            GameObject found = GameObject.Find("StageRoot");
            if (found != null)
            {
                stageRoot = found.transform;
                Debug.Log("[HologramSetup] Found StageRoot in scene");
            }
            else
            {
                GameObject stageObj = new GameObject("StageRoot");
                stageRoot = stageObj.transform;
                stageRoot.position = new Vector3(0f, 150f, 300f);
                Debug.Log("[HologramSetup] Created StageRoot at default position (0, 150, 300)");
            }
        }

        // ================================================================
        // 2. FIND OR CREATE WORLD MOVER (child of StageRoot)
        // ================================================================
        Transform existing = stageRoot.Find("WorldMover");
        if (existing != null)
        {
            worldMover = existing;
            Debug.Log("[HologramSetup] Found existing WorldMover under StageRoot");
        }
        else
        {
            GameObject worldMoverObj = new GameObject("WorldMover");
            worldMover = worldMoverObj.transform;
            worldMover.SetParent(stageRoot);
            worldMover.localPosition = Vector3.zero;
            worldMover.localRotation = Quaternion.identity;
            worldMover.localScale = Vector3.one;
            Debug.Log("[HologramSetup] Created WorldMover under StageRoot");
        }

        // ================================================================
        // 3. LOAD LEVEL 1 ADDITIVELY
        // ================================================================
        Debug.Log("[HologramSetup] Loading Level 1 additively...");
        StartCoroutine(LoadLevel1());
    }

    IEnumerator LoadLevel1()
    {
        AsyncOperation loadOp = SceneManager.LoadSceneAsync("Level 1", LoadSceneMode.Additive);
        if (loadOp == null)
        {
            Debug.LogError("[HologramSetup] Failed to start loading 'Level 1'. Is it in Build Settings?");
            yield break;
        }

        while (!loadOp.isDone)
            yield return null;

        Debug.Log("[HologramSetup] Level 1 loaded. Reparenting...");

        Scene level1Scene = SceneManager.GetSceneByName("Level 1");
        if (!level1Scene.IsValid())
        {
            Debug.LogError("[HologramSetup] Level 1 scene not valid after load!");
            yield break;
        }

        // Scale WorldMover BEFORE reparenting
        worldMover.localScale = Vector3.one * levelScale;

        // Reparent with worldPositionStays=false so level (0,0,0) = WorldMover origin = StageRoot center
        GameObject[] roots = level1Scene.GetRootGameObjects();
        foreach (GameObject root in roots)
        {
            root.transform.SetParent(worldMover, false);
        }

        // Unload the empty scene container
        SceneManager.UnloadSceneAsync(level1Scene);

        // ================================================================
        // CONFIGURE LEVEL 1 OBJECTS
        // ================================================================

        // Disable Level 1's camera
        Camera[] cameras = worldMover.GetComponentsInChildren<Camera>(true);
        foreach (Camera cam in cameras)
        {
            Debug.Log($"[HologramSetup] Disabling camera: {cam.gameObject.name}");
            cam.gameObject.SetActive(false);
        }

        // Disable Level 1's directional lights
        Light[] lights = worldMover.GetComponentsInChildren<Light>(true);
        foreach (Light light in lights)
        {
            if (light.type == LightType.Directional)
            {
                Debug.Log($"[HologramSetup] Disabling light: {light.gameObject.name}");
                light.gameObject.SetActive(false);
            }
        }

        // Find Player
        Player[] players = worldMover.GetComponentsInChildren<Player>(true);
        if (players.Length > 0)
        {
            playerRef = players[0];
            Debug.Log($"[HologramSetup] Found Player: {playerRef.gameObject.name}");

            // Disable PlayerInput so VR controller drives input
            PlayerInput playerInput = playerRef.GetComponent<PlayerInput>();
            if (playerInput != null)
            {
                playerInput.enabled = false;
                Debug.Log("[HologramSetup] Disabled PlayerInput on Player");
            }

            // Scale physics for world-space CharacterController.Move
            // moveSpeed, jumpHeight, gravityValue are in level units — multiply by scale for cm
            playerRef.moveSpeed *= levelScale;
            playerRef.jumpHeight *= levelScale;
            playerRef.gravityValue *= levelScale;
            playerRef.DeathBarrier *= levelScale;
            Debug.Log($"[HologramSetup] Scaled Player physics (×{levelScale}): speed={playerRef.moveSpeed}, jump={playerRef.jumpHeight}, gravity={playerRef.gravityValue}");
        }
        else
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
            {
                playerRef = playerObj.GetComponent<Player>();
                if (playerRef != null)
                {
                    PlayerInput playerInput = playerRef.GetComponent<PlayerInput>();
                    if (playerInput != null) playerInput.enabled = false;

                    playerRef.moveSpeed *= levelScale;
                    playerRef.jumpHeight *= levelScale;
                    playerRef.gravityValue *= levelScale;
                    Debug.Log($"[HologramSetup] Found Player by tag: {playerRef.gameObject.name}");
                }
            }
            else
            {
                Debug.LogWarning("[HologramSetup] No Player found in Level 1!");
            }
        }

        // Scale FlyingEnemy speeds
        FlyingEnemy[] enemies = worldMover.GetComponentsInChildren<FlyingEnemy>(true);
        foreach (FlyingEnemy enemy in enemies)
        {
            enemy.speed *= levelScale;
        }
        Debug.Log($"[HologramSetup] Scaled {enemies.Length} FlyingEnemy speeds (×{levelScale})");

        // ================================================================
        // STAGE WORLD MOVER (scrolls world to keep player centered)
        // ================================================================
        if (playerRef != null)
        {
            StageWorldMover mover = worldMover.gameObject.AddComponent<StageWorldMover>();
            mover.playerTransform = playerRef.transform;
            mover.stageCenterX = stageCenterX;
            mover.stageBaseY = stageBaseY;
            mover.levelScale = levelScale;
        }

        // ================================================================
        // HOLOGRAM SHOOTER (right hand)
        // ================================================================
        HologramShooter shooter = gameObject.AddComponent<HologramShooter>();

        // ================================================================
        // VR PLAYER CONTROLLER (left hand)
        // ================================================================
        VRPlayer3DController vrController = gameObject.AddComponent<VRPlayer3DController>();
        if (playerRef != null)
        {
            vrController.playerCharacter = playerRef;
        }

        Debug.Log("[HologramSetup] === HOLOGRAM OPERA STAGE READY (waiting for VR controllers...) ===");

        // ================================================================
        // WAIT FOR RIGHT CONTROLLER
        // ================================================================
        StartCoroutine(WaitForRightController(shooter));
    }

    IEnumerator WaitForRightController(HologramShooter shooter)
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
                        Debug.Log("[HologramSetup] Disabled XRInputModalityManager");
                    }
                }

                rightCtrl.gameObject.SetActive(true);
                DisableRightHandInteractors(rightCtrl.gameObject);
                shooter.vrRayOrigin = rightCtrl;
                Debug.Log($"[HologramSetup] RIGHT CONTROLLER FOUND after {elapsed:F1}s — shooting enabled!");
                yield break;
            }
            elapsed += 0.5f;
            yield return new WaitForSeconds(0.5f);
        }

        Debug.LogWarning("[HologramSetup] Right Controller never appeared after 15s — mouse fallback only");
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
}
