using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;

public class MonsterSetup
{
    [MenuItem("Tools/Setup Monster Enemy")]
    static void SetupMonster()
    {
        FixMonsterImport();
        CreateMonsterMaterial();
        CreateMonsterAnimController();
        CreateMonsterPrefab();
    }

    static void FixMonsterImport()
    {
        string path = "Assets/Character/Monster/Monster_flying.fbx";
        ModelImporter importer = AssetImporter.GetAtPath(path) as ModelImporter;
        if (importer == null) { Debug.LogError("[MonsterSetup] No importer for FBX"); return; }

        // Keep Generic (not humanoid - it's a monster)
        importer.animationType = ModelImporterAnimationType.Generic;

        // Configure clip with looping
        ModelImporterClipAnimation[] clips = importer.defaultClipAnimations;
        if (clips.Length == 0) clips = importer.clipAnimations;

        for (int i = 0; i < clips.Length; i++)
        {
            clips[i].loopTime = true;
            clips[i].loopPose = true;
        }
        importer.clipAnimations = clips;
        importer.SaveAndReimport();
        Debug.Log("[MonsterSetup] FBX import fixed (" + clips.Length + " clips)");
    }

    static void CreateMonsterMaterial()
    {
        string matPath = "Assets/Character/Monster/MonsterMat.mat";
        if (AssetDatabase.LoadAssetAtPath<Material>(matPath) != null)
        {
            Debug.Log("[MonsterSetup] Material already exists");
            return;
        }

        Texture2D tex = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Character/Monster/Gagorn_Texture.png");
        Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        if (tex != null) mat.mainTexture = tex;
        AssetDatabase.CreateAsset(mat, matPath);
        Debug.Log("[MonsterSetup] Material created");
    }

    static void CreateMonsterAnimController()
    {
        string ctrlPath = "Assets/Character/Monster/MonsterAnimator.controller";
        if (AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(ctrlPath) != null)
            AssetDatabase.DeleteAsset(ctrlPath);

        var controller = AnimatorController.CreateAnimatorControllerAtPath(ctrlPath);
        var rootSM = controller.layers[0].stateMachine;

        // Get clip from FBX
        AnimationClip flyClip = null;
        Object[] assets = AssetDatabase.LoadAllAssetsAtPath("Assets/Character/Monster/Monster_flying.fbx");
        foreach (var a in assets)
        {
            if (a is AnimationClip c && !c.name.StartsWith("__preview__"))
            { flyClip = c; break; }
        }

        var flyState = rootSM.AddState("Flying", new Vector3(300, 0, 0));
        flyState.motion = flyClip;
        rootSM.defaultState = flyState;

        EditorUtility.SetDirty(controller);
        AssetDatabase.SaveAssets();
        Debug.Log("[MonsterSetup] Animator controller created (clip=" + (flyClip != null ? flyClip.name : "NULL") + ")");
    }

    static void CreateMonsterPrefab()
    {
        // Load assets
        GameObject modelPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Character/Monster/Monster_flying.fbx");
        Material mat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Character/Monster/MonsterMat.mat");
        RuntimeAnimatorController ctrl = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>("Assets/Character/Monster/MonsterAnimator.controller");

        if (modelPrefab == null) { Debug.LogError("[MonsterSetup] Could not load Monster FBX"); return; }

        // Create root GameObject
        GameObject root = new GameObject("Enemy");

        // Add the model as child
        GameObject model = Object.Instantiate(modelPrefab, root.transform);
        model.name = "MonsterModel";
        model.transform.localPosition = Vector3.zero;
        model.transform.localRotation = Quaternion.identity;
        model.transform.localScale = Vector3.one;

        // Assign material to all renderers
        if (mat != null)
        {
            foreach (var rend in model.GetComponentsInChildren<Renderer>())
            {
                Material[] mats = new Material[rend.sharedMaterials.Length];
                for (int i = 0; i < mats.Length; i++) mats[i] = mat;
                rend.sharedMaterials = mats;
            }
        }

        // Setup animator
        Animator anim = model.GetComponentInChildren<Animator>();
        if (anim == null) anim = model.AddComponent<Animator>();
        anim.runtimeAnimatorController = ctrl;
        anim.applyRootMotion = false;

        // Add collider to root
        SphereCollider col = root.AddComponent<SphereCollider>();
        col.isTrigger = true;
        col.radius = 0.5f;
        col.center = new Vector3(0, 0.5f, 0);

        // Add CharacterController (needed by FlyingEnemy script)
        // Actually FlyingEnemy doesn't use CharacterController - it uses transform.position directly
        // So skip it.

        // Save as prefab (overwrite existing)
        string prefabPath = "Assets/Prefabs/Enemy.prefab";
        // Delete old prefab first
        if (AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath) != null)
            AssetDatabase.DeleteAsset(prefabPath);

        PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
        Object.DestroyImmediate(root);

        Debug.Log("[MonsterSetup] Enemy prefab created at " + prefabPath);
    }
}
