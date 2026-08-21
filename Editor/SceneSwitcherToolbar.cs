#if UNITY_6000_3_OR_NEWER
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.Toolbars;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace UnitySceneToolbar.Editor
{
    /// <summary>
    /// Unity 6.3+: official Main Toolbar dropdown (no reflection).
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

                menu.AddItem(new GUIContent(sceneName), isActive, () => SceneToolbarActions.OpenSceneAssetPath(scenePath));
            }

            menu.DropDown(dropDownRect);
        }

        private static void RefreshSceneList(bool refreshToolbar = true)
        {
            SceneToolbarActions.CollectScenes(out _scenePaths, out _sceneNames);

            if (refreshToolbar && _initialized)
                MainToolbar.Refresh(ElementPath);
        }

        private static void OnActiveSceneChanged(Scene _, Scene __) => MainToolbar.Refresh(ElementPath);
    }
}
#else
using System;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnitySceneToolbar.Editor
{
    /// <summary>
    /// Pre-6.3 fallback: inject an IMGUI scene popup into the legacy Editor toolbar via reflection.
    /// Not used on Unity 6.3+.
    /// </summary>
    [InitializeOnLoad]
    public static class SceneSwitcherToolbar
    {
        private static ScriptableObject _toolbar;
        private static string[] _scenePaths = Array.Empty<string>();
        private static string[] _sceneNames = Array.Empty<string>();

        static SceneSwitcherToolbar()
        {
            RefreshSceneList();
            EditorBuildSettings.sceneListChanged += RefreshSceneList;
            EditorApplication.projectChanged += RefreshSceneList;

            EditorApplication.delayCall += () =>
            {
                EditorApplication.update -= Update;
                EditorApplication.update += Update;
            };
        }

        private static void Update()
        {
            if (_toolbar != null)
                return;

            var editorAssembly = typeof(UnityEditor.Editor).Assembly;
            var toolbarType = editorAssembly.GetType("UnityEditor.Toolbar");
            if (toolbarType == null)
                return;

            var toolbars = Resources.FindObjectsOfTypeAll(toolbarType);
            if (toolbars.Length == 0)
                return;

            _toolbar = (ScriptableObject)toolbars[0];

            var rootField = toolbarType.GetField("m_Root", BindingFlags.NonPublic | BindingFlags.Instance);
            if (rootField == null)
                return;

            var root = rootField.GetValue(_toolbar) as VisualElement;
            var toolbarZone = root?.Q("ToolbarZoneRightAlign");
            if (toolbarZone == null)
                return;

            if (toolbarZone.Q("UnitySceneToolbar_SceneSwitcher") != null)
                return;

            var parent = new VisualElement
            {
                name = "UnitySceneToolbar_SceneSwitcher",
                style =
                {
                    flexGrow = 1,
                    flexDirection = FlexDirection.Row,
                    alignItems = Align.Center
                }
            };

            var container = new IMGUIContainer(OnGUI);
            parent.Add(container);
            toolbarZone.Add(parent);
        }

        private static void OnGUI()
        {
            using (new EditorGUI.DisabledScope(Application.isPlaying))
            {
                var sceneName = EditorSceneManager.GetActiveScene().name;
                var sceneIndex = -1;

                for (var i = 0; i < _sceneNames.Length; i++)
                {
                    if (sceneName == _sceneNames[i])
                    {
                        sceneIndex = i;
                        break;
                    }
                }

                var newSceneIndex = EditorGUILayout.Popup(sceneIndex, _sceneNames, GUILayout.Width(200f));
                if (newSceneIndex != sceneIndex && newSceneIndex >= 0 && newSceneIndex < _scenePaths.Length)
                    SceneToolbarActions.OpenSceneAssetPath(_scenePaths[newSceneIndex]);
            }
        }

        private static void RefreshSceneList() =>
            SceneToolbarActions.CollectScenes(out _scenePaths, out _sceneNames);
    }
}
#endif
