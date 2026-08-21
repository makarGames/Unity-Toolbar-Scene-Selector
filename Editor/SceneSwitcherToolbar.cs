using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.Toolbars;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace UnitySceneToolbar.Editor
{
    /// <summary>
    /// Main Toolbar dropdown: quickly switch between scenes listed in Build Settings.
    /// </summary>
    [InitializeOnLoad]
    public static class SceneSwitcherToolbar
    {
        public const string ElementPath = "Unity Scene Toolbar/Scene Switcher";

        private static string[] _scenePaths = System.Array.Empty<string>();
        private static string[] _sceneNames = System.Array.Empty<string>();
        private static bool _initialized;

        static SceneSwitcherToolbar()
        {
            RefreshSceneList(refreshToolbar: false);
            _initialized = true;

            EditorBuildSettings.sceneListChanged += () => RefreshSceneList();
            EditorApplication.projectChanged += () => RefreshSceneList();
            EditorSceneManager.activeSceneChangedInEditMode += OnActiveSceneChanged;
            SceneManager.activeSceneChanged += OnActiveSceneChanged;
        }

        [MainToolbarElement(ElementPath, defaultDockPosition = MainToolbarDockPosition.Right, defaultDockIndex = 0)]
        public static MainToolbarElement CreateSceneSwitcher()
        {
            var activeName = Application.isPlaying
                ? SceneManager.GetActiveScene().name
                : EditorSceneManager.GetActiveScene().name;

            if (string.IsNullOrEmpty(activeName))
                activeName = "Untitled";

            var icon = EditorGUIUtility.IconContent("SceneAsset Icon").image as Texture2D;
            var content = icon != null
                ? new MainToolbarContent(activeName, icon, "Select active scene")
                : new MainToolbarContent(activeName, "Select active scene");

            return new MainToolbarDropdown(content, ShowDropdownMenu)
            {
                displayed = true,
                enabled = !Application.isPlaying
            };
        }

        private static void ShowDropdownMenu(Rect dropDownRect)
        {
            var menu = new GenericMenu();

            if (_scenePaths.Length == 0)
            {
                menu.AddDisabledItem(new GUIContent("No Scenes"));
                menu.DropDown(dropDownRect);
                return;
            }

            var activeName = EditorSceneManager.GetActiveScene().name;

            for (var i = 0; i < _scenePaths.Length; i++)
            {
                var scenePath = _scenePaths[i];
                var sceneName = _sceneNames[i];
                var isActive = sceneName == activeName;

                menu.AddItem(new GUIContent(sceneName), isActive, () => OpenScene(scenePath));
            }

            menu.DropDown(dropDownRect);
        }

        private static void OpenScene(string scenePath)
        {
            if (Application.isPlaying)
                return;

            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(scenePath) == null)
            {
                Debug.LogError($"[Unity Scene Toolbar] Scene at path '{scenePath}' does not exist.");
                return;
            }

            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;

            EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
        }

        private static void RefreshSceneList(bool refreshToolbar = true)
        {
            var scenePaths = new List<string>();
            var sceneNames = new List<string>();

            foreach (var scene in EditorBuildSettings.scenes)
            {
                if (string.IsNullOrEmpty(scene.path) || !scene.path.StartsWith("Assets"))
                    continue;

                scenePaths.Add(scene.path);
                sceneNames.Add(Path.GetFileNameWithoutExtension(scene.path));
            }

            if (scenePaths.Count == 0)
            {
                foreach (var guid in AssetDatabase.FindAssets("t:scene"))
                {
                    var path = AssetDatabase.GUIDToAssetPath(guid);
                    scenePaths.Add(path);
                    sceneNames.Add(Path.GetFileNameWithoutExtension(path));
                }
            }

            _scenePaths = scenePaths.ToArray();
            _sceneNames = sceneNames.ToArray();

            if (refreshToolbar && _initialized)
                MainToolbar.Refresh(ElementPath);
        }

        private static void OnActiveSceneChanged(Scene _, Scene __) => MainToolbar.Refresh(ElementPath);
    }
}
