using UnityEngine;

// Lightweight data component attached to each hair card prefab.
// Stores per-card static setup data and runtime state.
// No logic lives here — HairManager owns all iteration and mutation.
[RequireComponent(typeof(SpriteRenderer))]
public class HairCard : MonoBehaviour
{
    // ── References (cached at spawn) ────────────────────────────────────────
    [HideInInspector] public SpriteRenderer SR;
    [HideInInspector] public Animator       Anim;   // null when prefab has no Animator

    // ── Static data — set once by HairManager.SpawnCards(), never changes ───
    [HideInInspector] public Vector3    RestPosition;   // world-space root (pivot on scalp)
    [HideInInspector] public Quaternion BaseRotation;   // outward-facing rotation
    [HideInInspector] public float      PhaseOffset;    // random 0..2π — desynchronises sine wave per card

    // ── Runtime state — mutated only by HairManager ─────────────────────────
    [HideInInspector] public float Amplitude;       // signed wind strength (negative = bends left)
    [HideInInspector] public float HeightFraction;  // current tip height as fraction of maxHeight (0..1)

    // Called by HairManager right after Instantiate to cache component refs
    public void Init()
    {
        SR   = GetComponent<SpriteRenderer>();
        Anim = GetComponent<Animator>();    // returns null when component is absent
    }
}
