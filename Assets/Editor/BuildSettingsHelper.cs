using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public class BuildSettingsHelper
{
    [MenuItem("Tools/Add Missing Scenes To Build")]
    static void AddScenes()
    {
        string[] scenesToAdd = new string[]
        {
            "Assets/Scenes/EnlessScene.unity",
            "Assets/UI/Scenes/gameOver.unity"
        };

        List<EditorBuildSettingsScene> scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);

        foreach (string path in scenesToAdd)
        {
            bool exists = false;
            foreach (var s in scenes)
            {
                if (s.path == path) { exists = true; break; }
            }
            if (!exists)
            {
                scenes.Add(new EditorBuildSettingsScene(path, true));
                Debug.Log("[BuildSettings] Added: " + path);
            }
            else
            {
                Debug.Log("[BuildSettings] Already in build: " + path);
            }
        }

        EditorBuildSettings.scenes = scenes.ToArray();
        Debug.Log("[BuildSettings] Done. Total scenes: " + scenes.Count);
    }
}
