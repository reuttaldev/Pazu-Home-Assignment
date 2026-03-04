# Pazu Home Assignment

This project is a 2D hair styling mobile game, made with Unity 2022.3.62f3 in a few short days.
There are 3 main interactive tools (Hair Dryer, Scissors, Hair Extension) operating on 500 sprite-based hair strands.

Hair is rendered as overlapping sprite cards (`hair.png`). They are distributed across multiple depth layers along an elliptical arc simulating a hairline.

All mechanics and effects are "faked" using math calculation. Usage of colliders and physics components are kept to a minimum to meet the efficiency demands of a mobile game with a large number of objects (hairs).

---

# Tools

| Tool | Sprite | Mechanic | Execution |
|------|--------|----------|-----------|
| Hair Dryer | `hairDrayer.png` | Drag over hair — nearby cards rotate toward the wind direction | Rotates each card in range toward the dryer's facing direction; strength scales with distance (closer = stronger); X-scale flip animation simulates air ruffling |
| Scissors | `scisors.png` | Drag across hair — cards in range become shorter | Reduces Y scale from the tip; pivot is at bottom-center so `transform.position` (scalp root) never moves |
| Hair Extension | `hairGrow.png` | Drag over hair — cards in range grow longer | Increases Y scale toward the tip at a fixed `growRate` per tick |

---

# Architecture

## DraggableTool (abstract base class)
All three tools extend `DraggableTool`. It handles:
- **Snap-back**: captures rest position and rotation in `Start()`, restores both on release via `SetPositionAndRotation`
- **Follow pointer**: moves the sprite to the finger/mouse position each frame
- **Drag lifecycle**: `OnDragBegin` → `OnDragMove` → `OnDragEnd`, routed by `InputManager`
- Subclasses implement `OnBegin`, `OnMove`, `OnEnd` for tool-specific behavior

## InputManager
Uses `Pointer.current` from the new Input System for both mouse (Editor) and touch (mobile). On press, does a `Physics2D.OverlapPoint` hit test to find the tool.

## HairCard
Lightweight `MonoBehaviour` data bag attached to each hair card prefab. Stores per-card references and state. No logic lives here for correct de-coupling.

## HairManager
Owns all hair cards and all operations on them. Cards are spawned once in `Awake` and stored in `HairCard[] cards` for fast indexed iteration.

### Instantiation 
- Cards are distributed along an **elliptical arc** (`arcRadiusX`, `arcRadiusY`) at the top of the head, covering `±arcDeg` degrees
- Multiple **depth layers** (`layerCount`, `layerSpacing`) stack concentric ellipses so inner layers sit closer to the scalp
- Per-card random jitter in angle, scale, and height breaks visual repetition
- Each layer is parented to its own `Layer_N` GameObject for scene hierarchy clarity
- Sorting order per card is the layer index ±1 randomly, so cards within a layer interleave naturally

### Tool–Hair Detection Architecture
Hair cards carry no `Collider2D` or `RigidBody` to decrease overhead. Instead, `HairManager` iterates over all cards and determines whether a tool is within range using optimized mathematical distance checks.

Below is a comparison of the possible detection approaches, with a focus on identifying the most efficient solution for this project’s scale.

#### Physics Queries (OverlapBox / OverlapCircle)

Each `HairCard` would have a `Collider2D`.  
Tools would call:

- `Physics2D.OverlapBoxNonAlloc` (Scissors)
- `Physics2D.OverlapCircleNonAlloc` (Dryer / Extension)

Unity’s physics engine maintains a **BVH (Bounding Volume Hierarchy)** to accelerate spatial queries. Queries return only colliders overlapping the specified shape.

**Cons**

Although queries are efficient, the BVH must remain up-to-date. Hair cards rotate every frame due to wind animation. Any transform change (position, rotation, scale) marks the collider as dirty.
This forces BVH maintenance continuously even when no tool is active.

With 500–1000 rotating cards:
- Continuous BVH updates
- Physics overhead every frame
- Cost exists even when not dragging tools

---

#### Static Colliders

Each `HairCard` would have a `Collider2D` without a `Rigidbody2D`, making it static. Static colliders are very cheap in Unity — **as long as they never move**.  They are inserted once into the BVH and remain stable.

**Cons**

Similarly to the previous con, the wind animation rotates every card each frame. Therefore:
- It is marked dirty.
- It must be reprocessed in the BVH.
- This happens every physics step.

Additionally, when using `OnTriggerEnter2D` / `OnTriggerStay2D`, the physics checks run every physics step, therefore detection cost even when no tool is being dragged. This removes control over when detection runs and wastes resources.

---

#### Kinematic Rigidbody2D

To avoid the issue where Unity would mark the colliders as dirty due to the wind, each `HairCard` would use:
- `Collider2D`
- `Rigidbody2D (Kinematic)`

Kinematic bodies are designed for moving objects. Unity uses **fat AABBs with velocity prediction** to reduce frequent tree updates. Small positional movement can be absorbed without full BVH reinsertion.

**Cons**
Rotation alters the collider’s axis-aligned bounding box (AABB). Fat AABB prediction does not prevent BVH updates when bounds change shape. Therefore:
- Every rotating card still requires BVH maintenance.
- Cost runs every frame.
- Cost runs even when no tool is active.

With 500–1000 cards rotating continuously, this creates unnecessary physics overhead.

---

#### Brute-Force Mathematical Check

`HairManager` stores all cards in an array. When a tool is dragged:
- Iterate over `_cards[]`
- Perform optimized math checks:
  - `sqrMagnitude` for circle range
  - Axis-aligned checks for scissors
  - Dot product projections for correct local-axis behavior

No colliders.
No physics queries.
No BVH.

**Pros**

At this scale:
- 500 checks ≈ 2–5 µs
- 1000 checks ≈ 5–10 µs
- 60,000 checks/sec ≈ ~0.3 ms/sec

This cost is negligible on mobile.

Additionally:
- Detection runs only while dragging.
- No per-frame physics maintenance.
- No editor setup required.
- No layer configuration.
- Fully deterministic.
- Consistent architecture across all tools.

This decision should be revisited when the hair card count increases significantly.

**Tool detection: brute-force math (O(N), no physics):**
 Distance is measured as an ellipse: `alongRadius` limits how far along the hair the tool can be, `perpRadius` limits how far off the hair axis it can be.

**Ellipse math** 
1. Project `rootToTool` onto the axis: `along = Dot(rootToTool, dir)`
2. Reject if `applySegmentBounds` and `along` is outside `[0, currentLength]`
3. Compute perpendicular component: `perp = rootToTool - dir * along`
4. Sum normalized squared distances: `normSum = along²/alongRadiusSq + perp.sqrMagnitude/perpRadiusSq`
5. Return `normSum <= 1` (inside ellipse); `normSum` also encodes falloff (0 at center, 1 at edge)

**Why the dryer passes `-windDir` as axis:**
`IsToolInRadius` computes `rootToTool = toolPos - root` (tool's perspective). Wind needs the projection from the dryer's perspective: `dot(toRoot, windDir)`. Since `dot(rootToTool, -windDir) = dot(toRoot, windDir)`, passing `-windDir` corrects the direction with no extra code.

**Wind falloff:**
`ApplyWind` reads wind direction from `hairDryer.transform.right` and rotates each in-range card toward the target Z angle (`Atan2(-windDir.x, windDir.y)`). Falloff is `Mathf.Pow(1 - normSum, windFalloffPower)` — `windFalloffPower` is Inspector-exposed: `1` = linear, `< 1` = stays strong longer, `> 1` = weakens quickly.

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

## FaceTarget
A component that rotates a tool to always face an assigned Transform. `Lerp` is used for a smooth transition.y Enabled/disabled by the tool during drag so it snaps back with the rest rotation on release.

## WobbleComponent (`WobbleAnim.cs`)
A component that adds a sine-wave rotation offset each `LateUpdate`. Runs after `FaceTarget` (which uses `Update`) so the wobble always layers on top cleanly. Enabled/disabled by the tool during drag.
