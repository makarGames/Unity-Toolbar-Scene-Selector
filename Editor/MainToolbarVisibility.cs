using System;
using System.Reflection;
using UnityEditor;
using UnityEditor.Overlays;
using UnityEditor.Toolbars;
using UnityEngine;

namespace UnitySceneToolbar.Editor
{
    /// <summary>
    /// In Unity 6.3+, custom MainToolbarElement entries are hidden until enabled via the toolbar overflow menu.
    /// This helper forces the package controls visible after import (with a few retries while the toolbar boots).
    /// </summary>
    [InitializeOnLoad]
    internal static class MainToolbarVisibility
    {
        private const string MigratedKey = "UnitySceneToolbar.MainToolbar.ForceShown.v1";
        private static int _attempts;

        static MainToolbarVisibility()
        {
            EditorApplication.delayCall += TryShow;
        }

        public static void EnsureVisible(string path) => ShowPath(path);

        private static void TryShow()
        {
            ShowPath(FirstScenePlayButton.ElementPath);
            ShowPath(SceneSwitcherToolbar.ElementPath);

            if (!EditorPrefs.GetBool(MigratedKey, false) && _attempts < 10)
            {
                _attempts++;
                EditorApplication.delayCall += TryShow;
                return;
            }

            EditorPrefs.SetBool(MigratedKey, true);
        }

        private static void ShowPath(string path)
        {
            try
            {
                var flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static;
                var type = typeof(MainToolbar);

                type.GetMethod("ShowAll", flags, null, new[] { typeof(string) }, null)
                    ?.Invoke(null, new object[] { path });

                type.GetMethod("SetDisplayedAll", flags, null, new[] { typeof(string), typeof(bool) }, null)
                    ?.Invoke(null, new object[] { path, true });

                var tryGet = type.GetMethod("TryGetOverlay", flags);
                if (tryGet != null)
                {
                    var args = new object[] { path, null };
                    if ((bool)tryGet.Invoke(null, args) && args[1] is Overlay overlay)
                        overlay.displayed = true;
                }

                MainToolbar.Refresh(path);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[Unity Scene Toolbar] MainToolbarVisibility ({path}): {e.Message}");
            }
        }
    }
}
