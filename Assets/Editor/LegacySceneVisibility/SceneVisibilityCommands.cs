using UnityEditor;

namespace LegacySceneVisibility
{
    public static class SceneVisibilityCommands
    {
        [MenuItem("Tools/Scene Visibility/Hide Selected _h")]
        public static void HideSelected()
        {
            SceneVisibilityManager.HideSelected();
        }

        [MenuItem("Tools/Scene Visibility/Show Selected #h")]
        public static void ShowSelected()
        {
            SceneVisibilityManager.ShowSelected();
        }

        [MenuItem("Tools/Scene Visibility/Toggle Selected Visibility")]
        public static void ToggleSelected()
        {
            SceneVisibilityManager.ToggleSelected();
        }

        [MenuItem("Tools/Scene Visibility/Hide Unselected %#h")]
        public static void HideUnselected()
        {
            SceneVisibilityManager.HideUnselected();
        }

        [MenuItem("Tools/Scene Visibility/Isolate Selected #i")]
        public static void IsolateSelected()
        {
            SceneVisibilityManager.IsolateSelected();
        }

        [MenuItem("Tools/Scene Visibility/Show All %#a")]
        public static void ShowAll()
        {
            SceneVisibilityManager.ShowAll();
        }

        [MenuItem("Tools/Scene Visibility/Restore Previous Visibility %#r")]
        public static void RestorePrevious()
        {
            SceneVisibilityManager.RestorePrevious();
        }

        [MenuItem("Tools/Scene Visibility/Open Visibility Window")]
        public static void OpenWindow()
        {
            SceneVisibilityWindow.Open();
        }

        [MenuItem("Tools/Scene Visibility/Settings")]
        public static void OpenSettings()
        {
            SceneVisibilitySettings.Open();
        }
    }
}
