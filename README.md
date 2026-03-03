# Pazu Home Assignment

## How It Works

Hair is rendered as overlapping sprite cards (`hair.png`) distributed across multiple depth layers along an elliptical arc. All effects are faked:
- **Wind** — sine wave oscillation per card, phase-shifted so they move independently
- **Cut** — reduce Y scale from the tip (pivot at root, so the root stays fixed)
- **Grow** — increase Y scale toward the tip

Bottom-center pivot is the key geometric choice: `transform.position` is always the scalp root and never moves. Scaling Y only moves the tip, so cut and grow require no offset arithmetic.

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

### HairCard
Lightweight `MonoBehaviour` data bag attached to each hair card prefab. Stores per-card references and state — no logic lives here. All fields are written by `HairManager`.

| Field | Type | Description |
|-------|------|-------------|
| `SR` | `SpriteRenderer` | Cached at spawn via `Init()` |
| `Anim` | `Animator` | Cached at spawn, null if prefab has no Animator |
| `RestPosition` | `Vector3` | World-space root on the scalp — never changes |
| `BaseRotation` | `Quaternion` | Outward-facing rotation — never changes |
| `PhaseOffset` | `float` | Random 0–2π, desynchronizes sine wave per card |
| `Amplitude` | `float` | Max sway angle in degrees, written by HairManager each frame |
| `HeightFraction` | `float` | Current tip height as fraction of maxHeight (0–1) |

### HairManager
Owns all hair cards and all operations on them. Cards are spawned once in `Awake` and stored in `HairCard[] _cards` for fast indexed iteration.

**Spawn layout:**
- Cards are distributed along an **elliptical arc** (`arcRadiusX`, `arcRadiusY`) at the top of the head, covering `±arcDeg` degrees
- Multiple **depth layers** (`layerCount`, `layerSpacing`) stack concentric ellipses so inner layers sit closer to the scalp — each layer covers the full arc independently, producing horizontal depth not vertical sections
- Per-card random jitter in angle, scale, and height breaks visual repetition
- Each layer is parented to its own `Layer_N` GameObject for scene hierarchy clarity
- Sorting order per card is the layer index ±1 randomly, so cards within a layer interleave naturally

**Wind animation:**
`Amplitude` is a rotation angle in degrees. Each frame every card rotates around its fixed root (`RestPosition`) by `Sin(t * windFreq + PhaseOffset) * Amplitude`. No world-space X offset is applied — rotation-only works correctly for all card orientations on the arc.

**Tool detection — brute-force math (O(N), no physics):**
All three tools iterate `cards[]` directly. Hair cards carry no `Collider2D`.

Each tool uses a shared `IsToolInRadius` helper that checks whether the tool is within a radius of any point along the root-to-tip segment — so tools respond correctly anywhere along the hair length, not just at the root.

| Tool | Shape | Math |
|------|-------|------|
| Scissors | Circle (`bladeRadius`) | `IsToolInRadius` proximity; cut height via `Dot(toScissors, card.transform.up)` |
| Hair Dryer | Circle | `IsToolInRadius` proximity; wind force via `-Dot(windDir, card.transform.right)` |
| Hair Extension | Circle (`growRadius`) | `IsToolInRadius` proximity; fixed `growRate * dt` per card |

**Why `Dot(toTool, alongHair)` gives the projection along the hair:**

**Step 1 — Draw the situation**
```
              toTool
                *
               /
              /
             / θ
            /
root  *----->-----------------
         alongHair
```
`alongHair` is the normalized hair direction (`card.transform.up`).
`toTool` is the vector from the root to the tool position.
θ is the angle between them.

**Step 2 — Drop a perpendicular**
```
              toTool
                *
               /|
              / |
             /  |  ← perpendicular part
            /   |
root  *----X----+-------------
         alongHair
```
X is the **projection point** — where the tool "lands" on the hair axis.
This gives a right triangle: hypotenuse = `|toTool|`, adjacent side = root→X.

**Step 3 — Basic trig**
```
cos(θ) = adjacent / hypotenuse
adjacent = |toTool| * cos(θ)
```
The adjacent side (root→X) is exactly the **projection length** — how far along the hair the tool sits.

**Step 4 — Where dot product comes in**

The dot product is defined as:
```
a · b = |a| |b| cos(θ)
```
Since `alongHair` is normalized (`|alongHair| = 1`):
```
toTool · alongHair = |toTool| · 1 · cos(θ)
                   = |toTool| cos(θ)
                   = projection length  ✓
```

So `Dot(toTool, alongHair)` directly gives the signed distance along the hair where the tool projects — without any trig calls.

**Step 5 — What happens to the perpendicular part?**

The perpendicular distance (how far the tool is from the hair axis) could be computed as:
```
sqrt(|toTool|² - projection²)
```
But `sqrt` is expensive. Instead, we reconstruct the perpendicular vector directly:
```
offset = toTool - alongHair * projection
```
Geometrically: `alongHair * projection` is the adjacent side (the vector along the hair to the projection point X). Subtracting it from `toTool` removes the horizontal part — what remains is the perpendicular vector pointing from X to the tool.
```
              toTool
                *
               /|
    toTool    / | ← offset = toTool - alongHair*proj
             /  |
root  *----X----+-------------
         alongHair*proj
```
Then `offset.sqrMagnitude` gives the squared perpendicular distance — no `sqrt` needed, just a comparison against `radius²`.

See `Assets/Docs/ToolHairDetectionDecision.md` for the full analysis including why brute-force outperforms `OverlapBoxNonAlloc` / `OverlapCircleNonAlloc` for this layout.

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
    Hair/
      HairCard.cs         — data bag component, one per card prefab
    Managers/
      HairManager.cs      — spawns cards, owns all hair logic
      InputManager.cs     — routes touch/mouse input to the active tool
    Tools/
      DraggableTool.cs    — abstract drag base class
      HairDryer.cs
      Scissors.cs
      HairExtension.cs
    Graphics/
      FaceTarget.cs       — rotates tool toward a target Transform
      WobbleAnim.cs       — sine-wave rotation wobble component
  Sprites/
  Scenes/
    Main.unity
Docs/
  HairTouchDetection_Comparison_And_Decision.txt
```
