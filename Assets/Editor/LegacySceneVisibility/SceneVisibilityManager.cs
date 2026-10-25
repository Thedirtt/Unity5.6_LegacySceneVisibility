using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace LegacySceneVisibility
{
    /// <summary>
    /// Editor-only temporary scene visibility manager for Unity 5.6.6f2.
    /// Uses camera render callbacks to hide renderers/lights/colliders during Scene view
    /// rendering without changing GameObject active state or component enabled states persistently.
    /// </summary>
    [InitializeOnLoad]
    public static class SceneVisibilityManager
    {
        // Own hidden state (instance ids). Effective hidden = own or ancestor own.
        private static readonly HashSet<int> ownHidden = new HashSet<int>();

        // Previous snapshot for restore.
        private static HashSet<int> previousSnapshot = null;
        private static bool hasPrevious = false;

        // Play mode suspension.
        public static bool Suspended { get; private set; }

        // Caches for renderers/lights/colliders per hidden root.
        private static readonly Dictionary<int, List<Renderer>> rendererCache = new Dictionary<int, List<Renderer>>();
        private static readonly Dictionary<int, List<Light>> lightCache = new Dictionary<int, List<Light>>();
        private static readonly Dictionary<int, List<Collider>> colliderCache = new Dictionary<int, List<Collider>>();

        // Per-frame disabled components (restored in OnPostRender).
        private static readonly List<Renderer> disabledRenderers = new List<Renderer>();
        private static readonly List<bool> disabledRendererStates = new List<bool>();
        private static readonly List<Light> disabledLights = new List<Light>();
        private static readonly List<bool> disabledLightStates = new List<bool>();
        private static readonly List<Collider> disabledColliders = new List<Collider>();
        private static readonly List<bool> disabledColliderStates = new List<bool>();

        // Icons
        private static Texture2D eyeOpenIcon;
        private static Texture2D eyeClosedIcon;

        static SceneVisibilityManager()
        {
            Camera.onPreCull += OnPreCull;
            Camera.onPostRender += OnPostRender;
            EditorApplication.hierarchyWindowItemOnGUI += OnHierarchyGUI;
            EditorApplication.playmodeStateChanged += OnPlayModeStateChanged;
            SceneManager.sceneLoaded += OnSceneLoaded;
            AppDomain.CurrentDomain.DomainUnload += (s, e) => RestoreDisabled();
        }

        // ---------------- Public API ----------------

        public static bool IsOwnHidden(GameObject go)
        {
            return go != null && ownHidden.Contains(go.GetInstanceID());
        }

        public static bool IsEffectivelyHidden(GameObject go)
        {
            if (go == null) return false;
            Transform t = go.transform;
            while (t != null)
            {
                if (ownHidden.Contains(t.gameObject.GetInstanceID())) return true;
                t = t.parent;
            }
            return false;
        }

        public static void Hide(GameObject go)
        {
            if (go == null) return;
            ownHidden.Add(go.GetInstanceID());
            ClearCaches();
            RepaintAll();
        }

        public static void Show(GameObject go)
        {
            if (go == null) return;
            ownHidden.Remove(go.GetInstanceID());
            ClearCaches();
            RepaintAll();
        }

        public static void Toggle(GameObject go)
        {
            if (go == null) return;
            if (IsOwnHidden(go)) Show(go); else Hide(go);
        }

        public static void HideSelected()
        {
            SavePrevious();
            foreach (GameObject go in Selection.gameObjects)
            {
                if (go != null) HideNoRepaint(go);
            }
            ClearCaches();
            RepaintAll();
        }

        public static void ShowSelected()
        {
            SavePrevious();
            foreach (GameObject go in Selection.gameObjects)
            {
                if (go != null) ShowNoRepaint(go);
            }
            ClearCaches();
            RepaintAll();
        }

        public static void ToggleSelected()
        {
            SavePrevious();
            foreach (GameObject go in Selection.gameObjects)
            {
                if (go == null) continue;
                if (IsOwnHidden(go)) ShowNoRepaint(go); else HideNoRepaint(go);
            }
            ClearCaches();
            RepaintAll();
        }

        public static void HideUnselected()
        {
            SavePrevious();
            HashSet<int> keep = new HashSet<int>();
            foreach (GameObject sel in Selection.gameObjects)
            {
                if (sel == null) continue;
                AddWithAncestorsAndDescendants(sel, keep);
            }
            foreach (int id in keep) ownHidden.Remove(id);
            Scene scene = SceneManager.GetActiveScene();
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                HideUnselectedRecursive(root, keep, false);
            }
            ClearCaches();
            RepaintAll();
        }

        public static void IsolateSelected()
        {
            SavePrevious();
            HashSet<int> keep = new HashSet<int>();
            foreach (GameObject sel in Selection.gameObjects)
            {
                if (sel == null) continue;
                AddWithAncestorsAndDescendants(sel, keep);
            }
            foreach (int id in keep) ownHidden.Remove(id);
            Scene scene = SceneManager.GetActiveScene();
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                IsolateRecursive(root, keep, false);
            }
            ClearCaches();
            RepaintAll();
        }

        public static void ShowAll()
        {
            ownHidden.Clear();
            ClearCaches();
            RepaintAll();
        }

        public static void RestorePrevious()
        {
            if (!hasPrevious || previousSnapshot == null) return;
            ownHidden.Clear();
            foreach (int id in previousSnapshot) ownHidden.Add(id);
            hasPrevious = false;
            previousSnapshot = null;
            ClearCaches();
            RepaintAll();
        }

        public static bool HasPrevious { get { return hasPrevious; } }

        // ---------------- Hierarchy GUI ----------------

        private static void OnHierarchyGUI(int instanceID, Rect selectionRect)
        {
            GameObject go = EditorUtility.InstanceIDToObject(instanceID) as GameObject;
            if (go == null) return;

            // Do not show visibility icon for cameras or canvas/ui elements.
            if (go.GetComponent<Camera>() != null) return;
            if (go.GetComponent<UnityEngine.Canvas>() != null) return;
            if (go.GetComponent<UnityEngine.CanvasRenderer>() != null) return;

            bool hidden = IsEffectivelyHidden(go);

            bool hover = selectionRect.Contains(Event.current.mousePosition);
            if (!hidden && !hover) return;

            float size = 14f;
            Rect iconRect = new Rect(selectionRect.xMax - size - 2f, selectionRect.y + 1f, size, size);

            if (eyeOpenIcon == null) CreateIcons();

            Texture2D icon = hidden ? eyeClosedIcon : eyeOpenIcon;
            string tooltip = hidden ? "Show in Scene view" : "Hide in Scene view";
            GUI.Label(iconRect, new GUIContent(icon, tooltip));

            if (Event.current.type == EventType.MouseDown && iconRect.Contains(Event.current.mousePosition))
            {
                Toggle(go);
                Event.current.Use();
            }
        }

        // ---------------- Camera hooks ----------------

        private static void OnPreCull(Camera cam)
        {
            CleanInvalid();
            RestoreDisabled();
            disabledRenderers.Clear();
            disabledRendererStates.Clear();
            disabledLights.Clear();
            disabledLightStates.Clear();
            disabledColliders.Clear();
            disabledColliderStates.Clear();

            if (Suspended) return;
            if (Application.isPlaying) return;
            if (SceneView.currentDrawingSceneView == null) return;

            foreach (int id in ownHidden.ToList())
            {
                GameObject go = EditorUtility.InstanceIDToObject(id) as GameObject;
                if (go == null) continue;
                if (HasHiddenAncestor(go)) continue;

                List<Renderer> rends;
                if (!rendererCache.TryGetValue(id, out rends))
                {
                    rends = new List<Renderer>(go.GetComponentsInChildren<Renderer>(true));
                    rendererCache[id] = rends;
                }
                foreach (Renderer r in rends)
                {
                    if (r == null) continue;
                    disabledRendererStates.Add(r.enabled);
                    r.enabled = false;
                    disabledRenderers.Add(r);
                }

                List<Light> lights;
                if (!lightCache.TryGetValue(id, out lights))
                {
                    lights = new List<Light>(go.GetComponentsInChildren<Light>(true));
                    lightCache[id] = lights;
                }
                foreach (Light l in lights)
                {
                    if (l == null) continue;
                    disabledLightStates.Add(l.enabled);
                    l.enabled = false;
                    disabledLights.Add(l);
                }

                List<Collider> cols;
                if (!colliderCache.TryGetValue(id, out cols))
                {
                    cols = new List<Collider>(go.GetComponentsInChildren<Collider>(true));
                    colliderCache[id] = cols;
                }
                foreach (Collider c in cols)
                {
                    if (c == null) continue;
                    disabledColliderStates.Add(c.enabled);
                    c.enabled = false;
                    disabledColliders.Add(c);
                }
            }
        }

        private static void OnPostRender(Camera cam)
        {
            RestoreDisabled();
            disabledRenderers.Clear();
            disabledRendererStates.Clear();
            disabledLights.Clear();
            disabledLightStates.Clear();
            disabledColliders.Clear();
            disabledColliderStates.Clear();
        }

        private static void RestoreDisabled()
        {
            for (int i = 0; i < disabledRenderers.Count; i++)
            {
                Renderer r = disabledRenderers[i];
                if (r != null) r.enabled = disabledRendererStates[i];
            }
            for (int i = 0; i < disabledLights.Count; i++)
            {
                Light l = disabledLights[i];
                if (l != null) l.enabled = disabledLightStates[i];
            }
            for (int i = 0; i < disabledColliders.Count; i++)
            {
                Collider c = disabledColliders[i];
                if (c != null) c.enabled = disabledColliderStates[i];
            }
        }

        // ---------------- Helpers ----------------

        private static void HideNoRepaint(GameObject go)
        {
            if (go == null) return;
            ownHidden.Add(go.GetInstanceID());
        }

        private static void ShowNoRepaint(GameObject go)
        {
            if (go == null) return;
            ownHidden.Remove(go.GetInstanceID());
        }

        private static bool HasHiddenAncestor(GameObject go)
        {
            Transform t = go.transform.parent;
            while (t != null)
            {
                if (ownHidden.Contains(t.gameObject.GetInstanceID())) return true;
                t = t.parent;
            }
            return false;
        }

        private static void AddWithAncestorsAndDescendants(GameObject go, HashSet<int> set)
        {
            Transform t = go.transform;
            while (t != null)
            {
                set.Add(t.gameObject.GetInstanceID());
                t = t.parent;
            }
            foreach (Transform child in go.GetComponentsInChildren<Transform>(true))
            {
                set.Add(child.gameObject.GetInstanceID());
            }
        }

        private static void HideUnselectedRecursive(GameObject go, HashSet<int> keep, bool ancestorHidden)
        {
            if (go == null) return;
            bool hideThis = !keep.Contains(go.GetInstanceID()) && !ancestorHidden;
            if (hideThis) ownHidden.Add(go.GetInstanceID());
            bool nowHidden = ancestorHidden || hideThis;
            foreach (Transform c in go.transform)
            {
                HideUnselectedRecursive(c.gameObject, keep, nowHidden);
            }
        }

        private static void IsolateRecursive(GameObject go, HashSet<int> keep, bool ancestorHidden)
        {
            if (go == null) return;
            bool hideThis = !keep.Contains(go.GetInstanceID()) && !ancestorHidden;
            if (hideThis) ownHidden.Add(go.GetInstanceID());
            if (keep.Contains(go.GetInstanceID())) ownHidden.Remove(go.GetInstanceID());
            bool nowHidden = ancestorHidden || hideThis;
            foreach (Transform c in go.transform)
            {
                IsolateRecursive(c.gameObject, keep, nowHidden);
            }
        }

        private static void ClearCaches()
        {
            rendererCache.Clear();
            lightCache.Clear();
            colliderCache.Clear();
        }

        private static void CleanInvalid()
        {
            List<int> remove = null;
            foreach (int id in ownHidden)
            {
                if (EditorUtility.InstanceIDToObject(id) == null)
                {
                    if (remove == null) remove = new List<int>();
                    remove.Add(id);
                }
            }
            if (remove != null)
            {
                foreach (int id in remove) ownHidden.Remove(id);
                ClearCaches();
            }
        }

        private static void OnPlayModeStateChanged()
        {
            Suspended = EditorApplication.isPlaying;
            RepaintAll();
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            CleanInvalid();
        }

        private static void SavePrevious()
        {
            previousSnapshot = new HashSet<int>(ownHidden);
            hasPrevious = true;
        }

        private static void RepaintAll()
        {
            SceneView.RepaintAll();
            EditorApplication.RepaintHierarchyWindow();
            if (SceneVisibilityWindow.Instance != null) SceneVisibilityWindow.Instance.Repaint();
        }

        private static void CreateIcons()
        {
            eyeOpenIcon = CreateEyeTexture(false);
            eyeClosedIcon = CreateEyeTexture(true);
        }

        private static Texture2D CreateEyeTexture(bool closed)
        {
            int s = 16;
            Texture2D t = new Texture2D(s, s, TextureFormat.ARGB32, false);
            Color clear = new Color(0, 0, 0, 0);
            Color fg = EditorGUIUtility.isProSkin ? Color.white : Color.black;
            for (int y = 0; y < s; y++)
                for (int x = 0; x < s; x++)
                    t.SetPixel(x, y, clear);

            float cx = 7.5f, cy = 7.5f;
            float rx = 6f, ry = 3.5f;
            for (int y = 0; y < s; y++)
            {
                for (int x = 0; x < s; x++)
                {
                    float dx = (x - cx) / rx;
                    float dy = (y - cy) / ry;
                    float d = dx * dx + dy * dy;
                    if (d >= 0.7f && d <= 1.15f)
                        t.SetPixel(x, y, fg);
                }
            }
            if (!closed)
            {
                for (int y = 0; y < s; y++)
                    for (int x = 0; x < s; x++)
                    {
                        float dx = x - cx;
                        float dy = y - cy;
                        if (dx * dx + dy * dy <= 2.5f) t.SetPixel(x, y, fg);
                    }
            }
            else
            {
                for (int x = 2; x < s - 2; x++) t.SetPixel(x, 7, fg);
                for (int x = 2; x < s - 2; x++) t.SetPixel(x, 8, fg);
            }
            if (closed)
            {
                for (int i = 0; i < s; i++)
                {
                    int x = i;
                    int y = i;
                    if (x >= 0 && x < s && y >= 0 && y < s) t.SetPixel(x, y, fg);
                    int y2 = s - 1 - i;
                    if (x >= 0 && x < s && y2 >= 0 && y2 < s) t.SetPixel(x, y2, fg);
                }
            }
            t.Apply();
            return t;
        }

        public static Texture2D EyeOpen { get { if (eyeOpenIcon == null) CreateIcons(); return eyeOpenIcon; } }
        public static Texture2D EyeClosed { get { if (eyeClosedIcon == null) CreateIcons(); return eyeClosedIcon; } }
    }
}
