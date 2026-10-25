using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace LegacySceneVisibility
{
    public class SceneVisibilityWindow : EditorWindow
    {
        public static SceneVisibilityWindow Instance;

        [MenuItem("Tools/Scene Visibility/Open Visibility Window")]
        public static void Open()
        {
            SceneVisibilityWindow w = GetWindow<SceneVisibilityWindow>("Scene Visibility");
            w.Show();
        }

        void OnEnable() { Instance = this; }
        void OnDisable() { Instance = null; }

        private Vector2 scroll;

        void OnGUI()
        {
            GUILayout.Label("Scene Visibility (Temporary)", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Visibility changes are editor-only and do not modify GameObject state.", MessageType.Info);

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Hide Selected")) SceneVisibilityManager.HideSelected();
            if (GUILayout.Button("Show Selected")) SceneVisibilityManager.ShowSelected();
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Toggle Selected")) SceneVisibilityManager.ToggleSelected();
            if (GUILayout.Button("Hide Unselected")) SceneVisibilityManager.HideUnselected();
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Isolate")) SceneVisibilityManager.IsolateSelected();
            if (GUILayout.Button("Show All")) SceneVisibilityManager.ShowAll();
            GUILayout.EndHorizontal();
            if (GUILayout.Button("Restore Previous")) SceneVisibilityManager.RestorePrevious();

            GUILayout.Space(8);
            GUILayout.Label("Scene Objects", EditorStyles.boldLabel);
            scroll = EditorGUILayout.BeginScrollView(scroll);
            Scene scene = SceneManager.GetActiveScene();
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                DrawNode(root);
            }
            EditorGUILayout.EndScrollView();
        }

        private void DrawNode(GameObject go)
        {
            if (go == null) return;
            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(EditorGUI.indentLevel * 15f);
            bool hidden = SceneVisibilityManager.IsEffectivelyHidden(go);
            bool own = SceneVisibilityManager.IsOwnHidden(go);
            Texture2D icon = hidden ? SceneVisibilityManager.EyeClosed : SceneVisibilityManager.EyeOpen;
            if (GUILayout.Button(new GUIContent(icon, hidden ? "Show" : "Hide"), GUIStyle.none, GUILayout.Width(18), GUILayout.Height(18)))
            {
                SceneVisibilityManager.Toggle(go);
            }
            EditorGUILayout.LabelField(go.name);
            EditorGUILayout.EndHorizontal();

            foreach (Transform c in go.transform)
            {
                DrawNode(c.gameObject);
            }
        }
    }
}
