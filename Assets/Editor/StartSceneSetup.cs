using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

public class StartSceneSetup
{
    [MenuItem("Tools/Setup Start Scene")]
    public static void Setup()
    {
        // Save current scene without dialog
        EditorSceneManager.SaveOpenScenes();

        // Create new scene
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        // --- XR Origin ---
        string[] xrGuids = AssetDatabase.FindAssets("XR Origin (XR Rig) t:Prefab");
        if (xrGuids.Length == 0)
            xrGuids = AssetDatabase.FindAssets("XR Origin t:Prefab");
        
        GameObject xrOrigin = null;
        if (xrGuids.Length > 0)
        {
            string path = AssetDatabase.GUIDToAssetPath(xrGuids[0]);
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            xrOrigin = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            xrOrigin.transform.position = Vector3.zero;
            xrOrigin.transform.rotation = Quaternion.identity;
            xrOrigin.transform.localScale = Vector3.one;
            Debug.Log("XR Origin added from: " + path);
        }
        else
        {
            Debug.LogWarning("XR Origin prefab not found! Add manually.");
        }

        // --- Directional Light ---
        var lightGO = new GameObject("Directional Light");
        var light = lightGO.AddComponent<Light>();
        light.type = LightType.Directional;
        light.color = new Color(1f, 0.95f, 0.84f);
        light.intensity = 1f;
        lightGO.transform.rotation = Quaternion.Euler(50, -30, 0);

        // --- Invisible Floor ---
        var floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
        floor.name = "InvisibleFloor";
        floor.transform.position = Vector3.zero;
        floor.transform.localScale = new Vector3(10, 1, 10);
        var floorRenderer = floor.GetComponent<MeshRenderer>();
        if (floorRenderer != null) floorRenderer.enabled = false;

        // --- Title Image Quad ---
        var quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
        quad.name = "TitleScreen";
        // Position 3m in front, at eye level (~1.5m)
        quad.transform.position = new Vector3(0, 1.5f, 3f);
        // Make it wide (16:9 aspect) - about 3m wide, 1.7m tall
        quad.transform.localScale = new Vector3(3f, 1.7f, 1f);

        // Load the texture
        var tex = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/UI/Textures/StartScreen.jpg");
        if (tex != null)
        {
            // Create unlit material so it looks bright
            var mat = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
            mat.mainTexture = tex;
            AssetDatabase.CreateAsset(mat, "Assets/UI/Textures/StartScreenMat.mat");
            quad.GetComponent<MeshRenderer>().sharedMaterial = mat;
            Debug.Log("Title screen texture applied.");
        }
        else
        {
            Debug.LogWarning("StartScreen.jpg not found at Assets/UI/Textures/StartScreen.jpg");
        }

        // Remove collider from quad (not needed)
        var quadCollider = quad.GetComponent<Collider>();
        if (quadCollider != null) Object.DestroyImmediate(quadCollider);

        // --- Start Handler (empty GO with script + big trigger collider for VR) ---
        var handler = new GameObject("StartHandler");
        handler.transform.position = new Vector3(0, 1.5f, 3f);
        handler.AddComponent<StartSceneHandler>();

        // Add a box collider as trigger for VR interaction
        var box = handler.AddComponent<BoxCollider>();
        box.size = new Vector3(4f, 3f, 0.5f);
        box.isTrigger = false; // XR needs non-trigger for ray interactable

        // Try adding XRSimpleInteractable
        var xrType = System.Type.GetType("UnityEngine.XR.Interaction.Toolkit.Interactables.XRSimpleInteractable, Unity.XR.Interaction.Toolkit");
        if (xrType != null)
        {
            handler.AddComponent(xrType);
            Debug.Log("XRSimpleInteractable added to StartHandler.");
        }

        // --- Save scene ---
        string scenePath = "Assets/Scenes/StartScene.unity";
        EditorSceneManager.SaveScene(scene, scenePath);
        Debug.Log("StartScene saved to " + scenePath);

        // --- Update Build Settings: StartScene at index 0 ---
        var buildScenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
        
        // Remove any existing StartScene entry
        buildScenes.RemoveAll(s => s.path.Contains("StartScene"));
        
        // Insert at beginning
        buildScenes.Insert(0, new EditorBuildSettingsScene(scenePath, true));
        
        EditorBuildSettings.scenes = buildScenes.ToArray();
        Debug.Log("StartScene added to Build Settings at index 0.");

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }
}
