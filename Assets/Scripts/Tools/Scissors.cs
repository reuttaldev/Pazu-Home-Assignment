using UnityEngine;

[RequireComponent(typeof(Animator))]
public class Scissors : DraggableTool
{
    [SerializeField] HairManager hairManager;
    [SerializeField] float cutCooldown = 0.08f;
    [SerializeField] Vector2 offset;
    public Vector2 Offset => offset;

    Animator animator;
    float    cutTimer;

    void Awake()
    {
        animator = GetComponent<Animator>();
    }

    protected override void Start()
    {
        base.Start();
    }

    protected override void OnBegin(Vector2 pos)
    {
        transform.rotation = Quaternion.Euler(0, 0, 90f);
        cutTimer = 0f;
        animator.SetBool("active", true);
    }

    protected override void OnMove(Vector2 pos)
    {
        cutTimer -= Time.deltaTime;
        if (cutTimer <= 0f)
        {
            hairManager.CutHair(pos + offset);
            cutTimer = cutCooldown;
        }
    }

    protected override void OnEnd()
    {
        animator.SetBool("active", false);
    }
}
