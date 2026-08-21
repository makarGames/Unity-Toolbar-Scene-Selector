using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace UnitySceneToolbar.Editor
{
    /// <summary>
    /// Shared scene actions used by both Unity 6.3+ Main Toolbar API and legacy toolbar injection.
    /// </summary>
    internal static class SceneToolbarActions
    {
        internal const string SessionKeyPreviousScene = "UnitySceneToolbar.PreviousScenePath";

        internal static void PlayFromFirstScene()
        {
            var firstScene = EditorBuildSettings.scenes.FirstOrDefault(s => s.enabled);
            if (firstScene == null || !File.Exists(firstScene.path))
            {
                Debug.LogWarning("[Unity Scene Toolbar] No enabled scenes in Build Settings, or the scene file is missing.");
                return;
            }

            RememberCurrentScene();

            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;

            EditorSceneManager.OpenScene(firstScene.path, OpenSceneMode.Single);
            EditorApplication.isPlaying = true;
        }

        internal static void RememberCurrentScene()
        {
            var currentPath = EditorSceneManager.GetActiveScene().path;
            SessionState.SetString(
                SessionKeyPreviousScene,
                !string.IsNullOrEmpty(currentPath) && File.Exists(currentPath) ? currentPath : string.Empty);
        }

        internal static void RestorePreviousSceneIfNeeded(PlayModeStateChange state)
        {
            if (state != PlayModeStateChange.EnteredEditMode)
                return;

            var previous = SessionState.GetString(SessionKeyPreviousScene, string.Empty);
            if (!string.IsNullOrEmpty(previous) && File.Exists(previous))
                EditorSceneManager.OpenScene(previous, OpenSceneMode.Single);

            SessionState.SetString(SessionKeyPreviousScene, string.Empty);
        }

        internal static void OpenSceneAssetPath(string scenePath)
        {
            if (Application.isPlaying)
                return;

            if (string.IsNullOrEmpty(scenePath))
                return;

            // Legacy UI sometimes stored absolute paths — normalize to Assets/...
            if (Path.IsPathRooted(scenePath))
            {
                var dataPath = Application.dataPath.Replace('\\', '/');
                var normalized = scenePath.Replace('\\', '/');
                if (normalized.StartsWith(dataPath))
                    scenePath = "Assets" + normalized.Substring(dataPath.Length);
            }

            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(scenePath) == null)
            {
                Debug.LogError($"[Unity Scene Toolbar] Scene at path '{scenePath}' does not exist.");
                return;
            }

            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;

            EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
        }

        internal static void CollectScenes(out string[] scenePaths, out string[] sceneNames)
        {
            var paths = new List<string>();
            var names = new List<string>();

            foreach (var scene in EditorBuildSettings.scenes)
            {
                if (string.IsNullOrEmpty(scene.path) || !scene.path.StartsWith("Assets"))
                    continue;

                paths.Add(scene.path);
                names.Add(Path.GetFileNameWithoutExtension(scene.path));
            }

            if (paths.Count == 0)
            {
                foreach (var guid in AssetDatabase.FindAssets("t:scene"))
                {
                    var path = AssetDatabase.GUIDToAssetPath(guid);
                    paths.Add(path);
                    names.Add(Path.GetFileNameWithoutExtension(path));
                }
            }

            scenePaths = paths.ToArray();
            sceneNames = names.ToArray();
        }
    }
}
