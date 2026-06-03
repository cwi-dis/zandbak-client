using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[InitializeOnLoad]
public static class PlayFromScene
{
    static PlayFromScene()
    {
        const string scenePath = "Assets/Scenes/LoginScene.unity";
        var initialScene = AssetDatabase.LoadAssetAtPath<SceneAsset>(scenePath);

        if (initialScene)
        {
            EditorSceneManager.playModeStartScene = initialScene;
        }
        else
        {
            Debug.LogWarning($"Could not find the scene at: {scenePath}. Make sure the path is correct!");
        }
    }
}
