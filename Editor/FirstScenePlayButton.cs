using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.Toolbars;
using UnityEngine;

namespace UnitySceneToolbar.Editor
{
    /// <summary>
    /// Main Toolbar button: open the first enabled Build Settings scene and enter Play Mode.
    /// After exiting Play Mode, restores the previously active scene.
    /// </summary>
    [InitializeOnLoad]
    public static class FirstScenePlayButton
    {
        public const string ElementPath = "Unity Scene Toolbar/Play From First Scene";
        private const string SessionKeyPreviousScene = "UnitySceneToolbar.PreviousScenePath";

        static FirstScenePlayButton()
        {
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        [MainToolbarElement(ElementPath, defaultDockPosition = MainToolbarDockPosition.Middle, defaultDockIndex = 0)]
        public static MainToolbarElement CreateToolbarButton()
        {
            var icon = EditorGUIUtility.IconContent("Animation.FirstKey");
            var content = new MainToolbarContent((Texture2D)icon.image, "Start from First Scene (Index 0)");

            return new MainToolbarButton(content, PlayFromFirstScene)
            {
                displayed = true,
                enabled = !EditorApplication.isPlayingOrWillChangePlaymode
            };
        }

        private static void PlayFromFirstScene()
        {
            var firstScene = EditorBuildSettings.scenes.FirstOrDefault(s => s.enabled);
            if (firstScene == null || !File.Exists(firstScene.path))
            {
                Debug.LogWarning("[Unity Scene Toolbar] No enabled scenes in Build Settings, or the scene file is missing.");
                return;
            }

            var activeScene = EditorSceneManager.GetActiveScene();
            var currentPath = activeScene.path;
            SessionState.SetString(
                SessionKeyPreviousScene,
                !string.IsNullOrEmpty(currentPath) && File.Exists(currentPath) ? currentPath : string.Empty);

            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;

            EditorSceneManager.OpenScene(firstScene.path, OpenSceneMode.Single);
            EditorApplication.isPlaying = true;
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.EnteredEditMode)
            {
                var previous = SessionState.GetString(SessionKeyPreviousScene, string.Empty);
                if (!string.IsNullOrEmpty(previous) && File.Exists(previous))
                    EditorSceneManager.OpenScene(previous, OpenSceneMode.Single);

                SessionState.SetString(SessionKeyPreviousScene, string.Empty);
            }

            MainToolbar.Refresh(ElementPath);
        }
    }
}
