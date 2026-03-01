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

## Implementation Progress

### DragLogic (abstract base class)
All three tools extend `DragLogic`. It handles:
- **Snap-back**: captures rest position in `Start()`, restores it on release
- **Follow pointer**: moves the sprite to the finger/mouse position each frame
- **Drag lifecycle**: `OnDragBegin` → `OnDragMove` → `OnDragEnd`, routed by `InputManager`
- Subclasses implement `OnBegin`, `OnMove`, `OnEnd` for tool-specific behavior

### InputManager
Uses `Pointer.current` from the new Input System — a single abstraction that covers both mouse (Editor) and touch (mobile) with no extra configuration. On press, does a `Physics2D.OverlapPoint` hit test against `toolLayerMask` to find the tool. Tracks one active tool at a time until the pointer is released.

### Tool GameObjects
`HairDryer`, `Scissors`, and `HairExtension` live under a `Tools` parent in the scene. Each has its own tool script.

---

## Project Structure

```
Assets/
  Scripts/
    DragLogic.cs     — abstract drag base class
    InputManager.cs  — routes touch/mouse input to the active tool
    HairManager.cs   — hair card system (stub, full impl next)
  Sprites/
  Scenes/
    Main.unity
```
