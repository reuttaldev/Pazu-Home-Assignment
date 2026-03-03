using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class HairCard : MonoBehaviour
{
    [HideInInspector] public SpriteRenderer SR;
    [HideInInspector] public Animator Anim;

    [HideInInspector] public Vector3 RestPosition;
    [HideInInspector] public Quaternion BaseRotation;
    [HideInInspector] public float PhaseOffset;

    [HideInInspector] public float Amplitude;
    [HideInInspector] public float currentLength; // current length in world units

    public void Init()
    {
        SR = GetComponent<SpriteRenderer>();
        Anim = GetComponent<Animator>();
    }
}
