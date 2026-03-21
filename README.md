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

# Tool–Hair Detection Architecture — Approach Comparison

Hair cards carry no `Collider2D` or `RigidBody` to decrease overhead. Below is a comparison of the possible detection approaches, with a focus on identifying the most efficient solution for this project's scale.

#### Physics Queries (OverlapBox / OverlapCircle)

Each `HairCard` would have a `Collider2D`.
Tools would call:

- `Physics2D.OverlapBoxNonAlloc` (Scissors)
- `Physics2D.OverlapCircleNonAlloc` (Dryer / Extension)

Unity's physics engine maintains a **BVH (Bounding Volume Hierarchy)** to accelerate spatial queries. Queries return only colliders overlapping the specified shape.

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
Rotation alters the collider's axis-aligned bounding box (AABB). Fat AABB prediction does not prevent BVH updates when bounds change shape. Therefore:
- Every rotating card still requires BVH maintenance.
- Cost runs every frame.
- Cost runs even when no tool is active.

With 500–1000 cards rotating continuously, this creates unnecessary physics overhead.

---

#### Brute-Force Mathematical Check ✓ (chosen)

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

---
# Architecture

## DraggableTool (abstract base class)
All three tools extend `DraggableTool`. It handles:
- **Snap-back**: captures rest position, rotation and scale in `Awake()`, restores all three on release
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

### Tool–Hair Detection (`IsToolInRadius`)
Hair cards carry no `Collider2D` or `RigidBody`. `HairManager` iterates all cards each frame and tests each one mathematically using an ellipse centered on the tool.

The detection ellipse is oriented along the tool's forward axis. `alongRadius` limits how far in front of the tool a hair can be; `perpRadius` limits how far to the side.

**Ellipse math**
1. Find the closest point on the hair strand: `t = Clamp(Dot(rootToTool, hairDir), 0, currentLength)`, `closestPoint = root + hairDir * t`
2. Measure from tool to that point: `toolToPoint = closestPoint - toolPos`
3. Project onto tool's forward axis: `alongTool = Dot(toolToPoint, toolDir)`
4. Reject if behind the tool (`alongTool < 0`) or out of forward range (`alongTool² > alongRadiusSq`)
5. Compute perpendicular: `perp = toolToPoint - toolDir * alongTool`
6. Compute `forwardNormSq = alongTool²/alongRadiusSq` and `lateralNormSq = perp.sqrMagnitude/perpRadiusSq`
7. Return `forwardNormSq + lateralNormSq <= 1` (inside ellipse); both values and `perp` are output for use by the caller

**Why `Dot(rootToTool, hairDir)` gives the closest point along the hair (step 1):**

**Step 1 — Draw the situation**
```
              rootToTool
                *
               /
              /
             / θ
            /
root  *----->-----------------
         hairDir
```
`hairDir` is the normalized hair direction (`card.transform.up`).
`rootToTool` is the vector from the root to the tool position.
θ is the angle between them.

**Step 2 — Drop a perpendicular**
```
              rootToTool
                *
               /|
              / |
             /  |  ← perpendicular part
            /   |
root  *----X----+-------------
         hairDir
```
X is the **projection point** — where the tool "lands" on the hair axis.
This gives a right triangle: hypotenuse = `|rootToTool|`, adjacent side = root→X.

**Step 3 — Basic trig**
```
cos(θ) = adjacent / hypotenuse
adjacent = |rootToTool| * cos(θ)
```
The adjacent side (root→X) is exactly the **projection length** — how far along the hair the tool sits.

**Step 4 — Where dot product comes in**

The dot product is defined as:
```
a · b = |a| |b| cos(θ)
```
Since `hairDir` is normalized (`|hairDir| = 1`):
```
rootToTool · hairDir = |rootToTool| · 1 · cos(θ)
                     = |rootToTool| cos(θ)
                     = projection length  ✓
```
So `Dot(rootToTool, hairDir)` directly gives the signed distance along the hair where the tool projects — without any trig calls. Clamping it to `[0, currentLength]` snaps X to the nearest point that actually lies on the strand.

**Step 5 — What happens to the perpendicular part?**

The perpendicular distance (how far the tool is from the hair axis) could be computed as:
```
sqrt(|rootToTool|² - projection²)
```
But `sqrt` is expensive. Instead, we reconstruct the perpendicular vector directly:
```
offset = rootToTool - hairDir * projection
```
Geometrically: `hairDir * projection` is the adjacent side (the vector along the hair to point X). Subtracting it from `rootToTool` removes the along-hair component — what remains is the perpendicular vector pointing from X to the tool.
```
              rootToTool
                *
               /|
              / | ← offset = rootToTool - hairDir*proj
             /  |
root  *----X----+-------------
         hairDir*proj
```
Then `offset.sqrMagnitude` gives the squared perpendicular distance — no `sqrt` needed, just a comparison against `radius²`.

**Why `Dot(toolToPoint, toolDir)` is used again in step 2:**

The same projection logic applies, but now relative to the tool. `toolToPoint = closestPoint - toolPos` points from the tool toward the closest point on the hair:
```
tool  *----Y---------  toolDir
            \
             \
              \
               * closestPoint
```
`Dot(toolToPoint, toolDir)` gives how far the closest point sits along the tool's forward axis — the "depth" into the detection zone. Subtracting it isolates the lateral offset exactly as in step 5 above:
```
              toolToPoint
                *
               /|
              / |
             /  | ← perp = toolToPoint - toolDir * alongTool
            /   |
tool  *----Y----+-------------
         toolDir * alongTool
```
`perp.sqrMagnitude` is the squared sideways distance from the tool's axis to the closest point on the hair — used directly in the ellipse test.

### Wind Spreading (`ApplyWind`)
Rotates each in-range card toward a target Z angle that produces a fan shape — center cards point straight with the wind, side cards angle outward. The logic is fully geometric.

`IsToolInRadius` outputs two squared normalised distances alongside the raw perpendicular vector `perp`:
- `forwardNormSq = alongTool² / wRangeSq` — squared normalised depth into the cone (0 at nozzle, 1 at max range)
- `lateralNormSq = perp.sqrMagnitude / wWidthSq` — squared normalised sideways distance from the wind axis (0 on-axis, 1 at cone edge)
- `perp` — the raw lateral offset vector, used to recover the left/right sign

In `ApplyWind` these are converted to linear [0, 1] values via `sqrt`, then used as follows:

```
forwardNorm    = sqrt(forwardNormSq)
lateralNorm    = sqrt(lateralNormSq)
sign           = Sign(Dot(perp, windPerp))        // left (-) or right (+) of wind axis
forwardFalloff = Pow(1 - forwardNorm, windForwardFalloffPower)   // 1 near nozzle, 0 at max range
lateralSpread  = Pow(lateralNorm, windLateralFalloffPower)       // 0 on-axis, 1 at cone edge
coneAngle      = lateralSpread * windMaxSpread * sign * forwardFalloff
targetZ        = baseTargetZ + coneAngle
```

- `baseTargetZ = Atan2(-windDir.x, windDir.y)` is the Z rotation that aligns the card's local `up` with `windDir` — center cards target this exactly
- `lateralSpread` shapes how the fan opens: `windLateralFalloffPower = 1` is linear, `< 1` opens faster near the center, `> 1` stays narrow until the edge
- `forwardFalloff` scales the overall spread intensity with distance — cards near the nozzle get full spread, cards at max range barely move. `windForwardFalloffPower > 1` makes pressure drop steeper, `< 1` keeps wind strong deeper into the cone
- The sign of `Dot(perp, windPerp)` determines which side of the axis the card is on, so the fan is symmetric

**Why `sqrt` before `Pow`:** both `forwardNormSq` and `lateralNormSq` are squared values — feeding them directly into `Pow(1 - x, power)` produces a non-linear input where the power parameter no longer has an intuitive meaning. Taking `sqrt` first restores linearity so `power = 1` gives a straight falloff, `power = 2` gives a quadratic, etc.

**Why `perp` is output from `IsToolInRadius`:** `perp` is already computed inside the function as part of the ellipse check. Outputting it avoids recomputing `card.position - toolPos` and a second dot product in `ApplyWind`.

### Scissors (`CutHair`)
Reduces `currentLength` of each in-range card by the cut amount. Y scale is recalculated from length so the tip shrinks while the root stays fixed.

### Hair Extension (`GrowHair`)
Increases `currentLength` of each in-range card at `growRate` per second, clamped to `maxLength`.

---

## FaceTarget (`FaceTarget.cs`)
A component that smoothly rotates a tool toward an assigned `target` Transform each frame.

- **Rotation**: `zRotation` is smoothly interpolated toward `AngleToTarget()` using `LerpAngle` at `speed * deltaTime`, then applied as `Quaternion.Euler(0, 0, zRotation)`.
- **Angle clamping**: `AngleToTarget` computes `Atan2(dir.y, dir.x)` and clamps the absolute angle to `[minAngle, maxAngle]`. This keeps the tool within a valid arc rather than freely spinning (e.g. scissors stay pointing roughly upward regardless of drag position).
- **Flip**: When `flip` is enabled, `localScale.y` is set to `sign * Abs(localScale.y)` — positive when the target is to the left, negative when to the right. This mirrors the sprite vertically so it always appears the correct way up regardless of which side it faces. A y-scale flip is used instead of a 180° z rotation because the z axis is already driven by the rotation logic above; a rotation flip would corrupt it.
- **Lifecycle**: Enabled on drag begin, disabled on drag end — so the tool snaps back to its rest transform (restored by `DraggableTool`) without FaceTarget overriding it. Because FaceTarget writes `localScale.y` every frame, `DraggableTool` must capture and restore `restScale` alongside `restPosition` and `restRotation`.

Used by: **Scissors** and **HairDryer**.

## WobbleComponent (`WobbleAnim.cs`)
A component that applies a sine-wave rotation each `LateUpdate`, independent of all other components. Enabled/disabled by the tool during drag.
