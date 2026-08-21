#if UNITY_6000_3_OR_NEWER
using UnityEditor;
using UnityEditor.Toolbars;
using UnityEngine;

namespace UnitySceneToolbar.Editor
{
    /// <summary>
    /// Unity 6.3+: official Main Toolbar button (no reflection).
    /// </summary>
    [InitializeOnLoad]
    public static class FirstScenePlayButton
    {
        public const string ElementPath = "Unity Scene Toolbar/Play From First Scene";

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

            return new MainToolbarButton(content, SceneToolbarActions.PlayFromFirstScene)
            {
                displayed = true,
                enabled = !EditorApplication.isPlayingOrWillChangePlaymode
            };
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            SceneToolbarActions.RestorePreviousSceneIfNeeded(state);
            MainToolbar.Refresh(ElementPath);
        }
    }
}
#else
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnitySceneToolbar.Editor
{
    /// <summary>
    /// Pre-6.3 fallback: inject a UI Toolkit button into the legacy Editor toolbar via reflection.
    /// Not used on Unity 6.3+.
    /// </summary>
    [InitializeOnLoad]
    public static class FirstScenePlayButton
    {
        private static ScriptableObject _toolbar;

        static FirstScenePlayButton()
        {
            EditorApplication.playModeStateChanged -= SceneToolbarActions.RestorePreviousSceneIfNeeded;
            EditorApplication.playModeStateChanged += SceneToolbarActions.RestorePreviousSceneIfNeeded;

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
            var playModeZone = root?.Q("ToolbarZonePlayMode");
            if (playModeZone == null)
                return;

            if (playModeZone.Q("UnitySceneToolbar_FirstScenePlayButton") != null)
                return;

            var button = new Button(SceneToolbarActions.PlayFromFirstScene)
            {
                name = "UnitySceneToolbar_FirstScenePlayButton",
                tooltip = "Start from First Scene (Index 0)",
                text = "↺▶"
            };
            button.AddToClassList("unity-toolbar-button");
            button.AddToClassList("toolbar-button");

            button.style.paddingLeft = 2;
            button.style.paddingRight = 2;
            button.style.marginLeft = 2;
            button.style.marginRight = 2;
            button.style.height = 19;
            button.style.unityFontStyleAndWeight = FontStyle.Normal;
            button.style.fontSize = 12;
            button.style.unityTextAlign = TextAnchor.MiddleCenter;

            var baseBg = EditorGUIUtility.isProSkin
                ? new Color(0.31f, 0.31f, 0.31f, 1f)
                : new Color(0.86f, 0.86f, 0.86f, 1f);
            var hoverBg = EditorGUIUtility.isProSkin
                ? new Color(0.50f, 0.50f, 0.50f, 1f)
                : new Color(0.95f, 0.95f, 0.95f, 1f);
            var activeBg = new Color(0.15f, 0.45f, 0.80f, 1f);

            button.style.backgroundColor = new StyleColor(baseBg);
            button.style.borderTopLeftRadius = 4;
            button.style.borderTopRightRadius = 4;
            button.style.borderBottomLeftRadius = 4;
            button.style.borderBottomRightRadius = 4;

            button.RegisterCallback<MouseEnterEvent>(_ => button.style.backgroundColor = new StyleColor(hoverBg));
            button.RegisterCallback<MouseLeaveEvent>(_ =>
            {
                button.style.backgroundColor = EditorApplication.isPlaying
                    ? new StyleColor(activeBg)
                    : new StyleColor(baseBg);
            });

            EditorApplication.playModeStateChanged += _ =>
            {
                button.style.backgroundColor = EditorApplication.isPlaying
                    ? new StyleColor(activeBg)
                    : new StyleColor(baseBg);
            };

            playModeZone.Insert(0, button);
        }
    }
}
#endif
