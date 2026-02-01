using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;

public class PlayerAnimSetup
{
    static readonly string sourceModelPath = "Assets/Character/Player/Models/Tom_Skeleton_Test.fbx";

    static readonly string[] animPaths = new string[]
    {
        "Assets/Character/Player/anim/Tom_Run_Right.fbx",
        "Assets/Character/Player/anim/Tom_Idle_Right.fbx",
        "Assets/Character/Player/anim/Tom_Jump_Right.fbx",
        "Assets/Character/Player/anim/Tom_DoubleJump_Right.fbx"
    };

    [MenuItem("Tools/Setup Player Animations")]
    static void SetupAll()
    {
        FixAnimationImport();
        CreateController();
    }

    static void FixAnimationImport()
    {
        foreach (string path in animPaths)
        {
            ModelImporter importer = AssetImporter.GetAtPath(path) as ModelImporter;
            if (importer == null)
            {
                Debug.LogWarning("[PlayerAnimSetup] Could not get importer for: " + path);
                continue;
            }

            // Set to Humanoid, auto-create avatar from this model's skeleton
            importer.animationType = ModelImporterAnimationType.Human;
            importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;

            // Configure clip settings (loop for Run and Idle)
            ModelImporterClipAnimation[] clips = importer.defaultClipAnimations;
            if (clips.Length == 0)
                clips = importer.clipAnimations;

            for (int i = 0; i < clips.Length; i++)
            {
                bool shouldLoop = path.Contains("Run") || path.Contains("Idle");
                clips[i].loopTime = shouldLoop;
                clips[i].loopPose = shouldLoop;
            }
            importer.clipAnimations = clips;

            importer.SaveAndReimport();
            Debug.Log("[PlayerAnimSetup] Fixed import: " + path + " (loop=" + (path.Contains("Run") || path.Contains("Idle")) + ")");
        }
    }

    [MenuItem("Tools/Create Player Animator Controller")]
    static void CreateController()
    {
        string controllerPath = "Assets/Character/Player/anim/PlayerAnimator.controller";

        // Delete existing to avoid stale references
        if (AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(controllerPath) != null)
            AssetDatabase.DeleteAsset(controllerPath);

        var controller = AnimatorController.CreateAnimatorControllerAtPath(controllerPath);

        // Add parameters
        controller.AddParameter("isGrounded", AnimatorControllerParameterType.Bool);
        controller.AddParameter("jumpCount", AnimatorControllerParameterType.Int);

        // Get the base layer state machine
        var rootStateMachine = controller.layers[0].stateMachine;

        // Load animation clips from FBX files
        AnimationClip runClip = GetClipFromFBX("Assets/Character/Player/anim/Tom_Run_Right.fbx");
        AnimationClip idleClip = GetClipFromFBX("Assets/Character/Player/anim/Tom_Idle_Right.fbx");
        AnimationClip jumpClip = GetClipFromFBX("Assets/Character/Player/anim/Tom_Jump_Right.fbx");
        AnimationClip doubleJumpClip = GetClipFromFBX("Assets/Character/Player/anim/Tom_DoubleJump_Right.fbx");

        // Create states
        var runState = rootStateMachine.AddState("Run_Right", new Vector3(300, 0, 0));
        runState.motion = runClip;

        var idleState = rootStateMachine.AddState("Idle_Right", new Vector3(0, 0, 0));
        idleState.motion = idleClip;

        var jumpState = rootStateMachine.AddState("Jump_Right", new Vector3(300, -200, 0));
        jumpState.motion = jumpClip;

        var doubleJumpState = rootStateMachine.AddState("DoubleJump_Right", new Vector3(600, -200, 0));
        doubleJumpState.motion = doubleJumpClip;

        // Set Run as default state
        rootStateMachine.defaultState = runState;

        // --- Transitions ---

        // Run -> Jump (isGrounded=false, jumpCount=1)
        var runToJump = runState.AddTransition(jumpState);
        runToJump.AddCondition(AnimatorConditionMode.IfNot, 0, "isGrounded");
        runToJump.AddCondition(AnimatorConditionMode.Equals, 1, "jumpCount");
        runToJump.hasExitTime = false;
        runToJump.duration = 0.05f;

        // Run -> DoubleJump (isGrounded=false, jumpCount>=2)
        var runToDouble = runState.AddTransition(doubleJumpState);
        runToDouble.AddCondition(AnimatorConditionMode.IfNot, 0, "isGrounded");
        runToDouble.AddCondition(AnimatorConditionMode.Equals, 2, "jumpCount");
        runToDouble.hasExitTime = false;
        runToDouble.duration = 0.05f;

        // Jump -> DoubleJump (jumpCount=2)
        var jumpToDouble = jumpState.AddTransition(doubleJumpState);
        jumpToDouble.AddCondition(AnimatorConditionMode.Equals, 2, "jumpCount");
        jumpToDouble.hasExitTime = false;
        jumpToDouble.duration = 0.05f;

        // Jump -> Run (isGrounded=true)
        var jumpToRun = jumpState.AddTransition(runState);
        jumpToRun.AddCondition(AnimatorConditionMode.If, 0, "isGrounded");
        jumpToRun.hasExitTime = false;
        jumpToRun.duration = 0.1f;

        // DoubleJump -> Run (isGrounded=true)
        var doubleToRun = doubleJumpState.AddTransition(runState);
        doubleToRun.AddCondition(AnimatorConditionMode.If, 0, "isGrounded");
        doubleToRun.hasExitTime = false;
        doubleToRun.duration = 0.1f;

        EditorUtility.SetDirty(controller);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[PlayerAnimSetup] Controller created at: " + controllerPath);
    }

    static AnimationClip GetClipFromFBX(string fbxPath)
    {
        Object[] assets = AssetDatabase.LoadAllAssetsAtPath(fbxPath);
        Debug.Log($"[PlayerAnimSetup] Sub-assets in {fbxPath}: {assets.Length}");
        foreach (var asset in assets)
        {
            Debug.Log($"[PlayerAnimSetup]   -> {asset.GetType().Name}: '{asset.name}'");
            if (asset is AnimationClip clip && !clip.name.StartsWith("__preview__"))
                return clip;
        }
        Debug.LogWarning("[PlayerAnimSetup] No clip found in: " + fbxPath);
        return null;
    }
}
