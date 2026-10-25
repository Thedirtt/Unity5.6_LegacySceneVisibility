# Legacy Scene Visibility for Unity 5.6.6f2

An editor-only Unity addon that adds temporary Scene view visibility controls to Unity 5.6.6f2, similar to the Scene Visibility feature in newer Unity versions.

## Features

- Temporarily hide/show GameObjects in the Scene view
- Does not change `GameObject.activeSelf` or component states
- Hierarchy window eye icon for quick toggling
- Menu commands and keyboard shortcuts
- Hide unselected, isolate selection, show all, restore previous
- Editor-only, no runtime dependencies

## Installation

**Primary method: import the Unity package**

1. Download `LegacySceneVisibility.unitypackage`.
2. In Unity, go to `Assets > Import Package > Custom Package...`.
3. Select the downloaded file and import everything.
4. Open Unity 5.6.6f2 and let it compile.
5. Access via `Tools > Scene Visibility`.

**Alternative: manual copy**

1. Download `LegacySceneVisibility.zip` (or use the `Deliverable/` folder).
2. Extract and copy `LegacySceneVisibility/Assets/Editor/LegacySceneVisibility/` into your Unity project's `Assets/Editor/` folder.
3. Open Unity 5.6.6f2 and let it compile.

## Usage

| Command | Shortcut |
|---|---|
| Hide Selected | `H` |
| Show Selected | `Shift+H` |
| Hide Unselected | `Ctrl+Shift+H` |
| Isolate Selected | `Shift+I` |
| Show All | `Ctrl+Shift+A` |
| Restore Previous | `Ctrl+Shift+R` |

You can also click the eye icon next to GameObjects in the Hierarchy window.

## Compatibility

- Unity 5.6.6f2 only
- Editor-only scripts (no runtime dependencies)

## Limitations

Unity 5.6.6f2 has no dedicated API for hiding arbitrary objects from the Scene view. This addon temporarily disables renderers, lights, and colliders during Scene view rendering and restores them immediately. Hidden objects may still be selectable in some cases. Canvas/UI elements and cameras are intentionally left untouched.

## Removal

Delete `Assets/Editor/LegacySceneVisibility/`. No GameObjects or project settings are left modified.

## License

MIT

---

*This project was created with AI assistance.*
