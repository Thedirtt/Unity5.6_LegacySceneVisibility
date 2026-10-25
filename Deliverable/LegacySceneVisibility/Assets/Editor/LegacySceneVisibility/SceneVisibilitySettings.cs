using UnityEditor;
using UnityEngine;

namespace LegacySceneVisibility
{
    public class SceneVisibilitySettings : EditorWindow
    {
        private const string PrefSuspendPlayMode = "LegacySceneVis.SuspendPlayMode";
        private const string PrefPersist = "LegacySceneVis.Persist";

        public static void Open()
        {
            GetWindow<SceneVisibilitySettings>("Scene Visibility Settings");
        }

        void OnGUI()
        {
            GUILayout.Label("Settings", EditorStyles.boldLabel);

            bool suspend = EditorPrefs.GetBool(PrefSuspendPlayMode, true);
            bool newSuspend = EditorGUILayout.Toggle("Suspend hiding in Play mode", suspend);
            if (newSuspend != suspend) EditorPrefs.SetBool(PrefSuspendPlayMode, newSuspend);

            GUILayout.Space(10);
            if (GUILayout.Button("Show All Hidden Now"))
            {
                SceneVisibilityManager.ShowAll();
            }
            if (GUILayout.Button("Clear Cached Icons"))
            {
                // no-op placeholder
            }

            GUILayout.Space(10);
            EditorGUILayout.HelpBox("Visibility state is temporary and editor-only. Removing this addon leaves GameObjects unchanged.", MessageType.Info);
        }
    }
}
