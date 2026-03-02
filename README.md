# Pazu Home Assignment

## How It Works

Hair is rendered as 60 overlapping sprite cards (`hair.png`), batched into a single draw call. All effects are faked:
- **Wind** — sine wave oscillation per card, phase-shifted so they move independently
- **Cut** — reduce Y scale from the tip (pivot at root, so the root stays fixed)
- **Grow** — increase Y scale toward the tip

---

## Tools

| Tool | Sprite | Mechanic |
|------|--------|----------|
| Hair Dryer | `hairDrayer.png` | Drag over hair — nearby cards wave side to side |
| Scissors | `scisors.png` | Drag across hair — cards in range become shorter |
| Hair Extension | `hairGrow.png` | Drag over hair — cards in range grow longer |

---

## Architecture

### DraggableTool (abstract base class)
All three tools extend `DraggableTool`. It handles:
- **Snap-back**: captures rest position and rotation in `Start()`, restores both on release via `SetPositionAndRotation`
- **Follow pointer**: moves the sprite to the finger/mouse position each frame
- **Drag lifecycle**: `OnDragBegin` → `OnDragMove` → `OnDragEnd`, routed by `InputManager`
- Subclasses implement `OnBegin`, `OnMove`, `OnEnd` for tool-specific behavior

### InputManager
Uses `Pointer.current` from the new Input System — a single abstraction that covers both mouse (Editor) and touch (mobile) with no extra configuration. On press, does a `Physics2D.OverlapPoint` hit test against `toolLayerMask` to find the tool. Tracks one active tool at a time until the pointer is released.

### FaceTarget
Reusable component that smoothly rotates a tool to always face an assigned Transform. Configurable `spriteForward` vector accounts for sprites that don't naturally point up. Enabled/disabled by the tool during drag so it snaps back with the rest rotation on release.

### WobbleComponent (`WobbleAnim.cs`)
Reusable component that adds a sine-wave rotation offset each `LateUpdate`. Runs after `FaceTarget` (which uses `Update`) so the wobble always layers on top cleanly. Enabled/disabled by the tool during drag.

### Tool behavior
| Tool | Rotation | Animation |
|------|----------|-----------|
| Hair Dryer | `FaceTarget` — smoothly faces target while dragging | `WobbleComponent` — gentle sway |
| Scissors | Snaps to 90° (pointing left) on pickup, holds until release | Animator `"active"` bool |
| Hair Extension | No rotation change | `WobbleComponent` — fast shake |

---

## Project Structure

```
Assets/
  Scripts/
    DraggableTool.cs    — abstract drag base class
    InputManager.cs     — routes touch/mouse input to the active tool
    FaceTarget.cs       — rotates tool toward a target Transform
    WobbleAnim.cs       — sine-wave rotation wobble component
    HairDryer.cs
    Scissors.cs
    HairExtension.cs
  Sprites/
  Scenes/
    Main.unity
```
