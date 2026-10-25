# Legacy Scene Visibility (Unity 5.6.6f2)

Editor-only temporary scene visibility addon for Unity 5.6.6f2.

## Installation (ZIP)
1. Extract the archive.
2. Copy the `LegacySceneVisibility` folder into your project's `Assets/Editor/` directory.
   Final path: `Assets/Editor/LegacySceneVisibility/`
3. Open Unity. The addon compiles automatically.
4. Access via `Tools > Scene Visibility`.

## Updating
Replace the `Assets/Editor/LegacySceneVisibility/` folder with the new version, then let Unity recompile.

## Usage
- Hide Selected: H
- Show Selected: Shift+H
- Hide Unselected: Ctrl+Shift+H
- Isolate Selected: Shift+I
- Show All: Ctrl+Shift+A
- Restore Previous: Ctrl+Shift+R
- Open Visibility Window: Tools > Scene Visibility > Open Visibility Window
- Settings: Tools > Scene Visibility > Settings

Also click the eye icon in the Hierarchy window.

## Compatibility
Uses only Unity 5.6.6f2 editor APIs: SceneView, EditorApplication callbacks, Camera render callbacks, EditorWindow, SceneManager, EditorPrefs.

## Limitations
Unity 5.6.6f2 has no dedicated editor-only "hide in scene view" API. This addon temporarily disables renderers, lights, colliders, cameras, and canvases during Scene view rendering and restores their original enabled states immediately afterward. GameObject.activeSelf/activeInHierarchy, transforms, parents, serialized data, and component values are never changed, and the scene is not dirtied by visibility changes. Hiding is suspended in Play mode so objects behave normally in-game. Hidden objects may still be selectable in some cases. Script-drawn gizmos (OnDrawGizmos) may not be fully suppressed in all cases. State is not persisted across Unity restarts by default.

## Gameplay state preservation
- GameObject.activeSelf/activeInHierarchy are never changed.
- Components are not permanently enabled/disabled.
- Transforms, parents, serialized data unchanged.
- Scene is not marked dirty by visibility changes.
- Play mode: hiding suspended so objects render normally in game.

## Removal
Delete `Assets/Editor/LegacySceneVisibility/`. No leftover modifications.

## Test checklist
Hide/show single and multiple; parent/child; hide unselected; isolate; restore all; create/destroy/rename/reparent; switch scenes; save/reopen; play mode; prefabs; no state changes; recompile; remove addon; performance.
