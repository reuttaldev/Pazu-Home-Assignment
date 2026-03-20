using UnityEngine;

[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(FaceTarget))]
public class Scissors : DraggableTool
{
    [SerializeField] HairManager hairManager;
    [SerializeField] float cutCooldown = 0.08f;
    FaceTarget faceTarget;
    Animator animator;
    float cutTimer;

    void Awake()
    {
        animator = GetComponent<Animator>();
        faceTarget = GetComponent<FaceTarget>();
        faceTarget.enabled = false;

    }

    protected override void OnBegin(Vector2 pos)
    {
        cutTimer = 0f;
        animator.SetBool("active", true);
        faceTarget.enabled = true;

    }

    protected override void OnMove(Vector2 pos)
    {
        cutTimer -= Time.deltaTime;
        if (cutTimer <= 0f)
        {
            hairManager.CutHair(pos);
            cutTimer = cutCooldown;
        }
    }

    protected override void OnEnd()
    {
        animator.SetBool("active", false);
        faceTarget.enabled = false;

    }
}
