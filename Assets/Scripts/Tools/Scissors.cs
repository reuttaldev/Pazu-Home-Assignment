using UnityEngine;

[RequireComponent(typeof(Animator))]
public class Scissors : DraggableTool
{
    [SerializeField] HairManager hairManager;
    [SerializeField] float cutCooldown = 0.08f;

    Animator _animator;
    float _cutTimer;

    void Awake()
    {
        _animator = GetComponent<Animator>();
    }

    protected override void Start()
    {
        base.Start();
    }

    protected override void OnBegin(Vector2 pos)
    {
        transform.rotation = Quaternion.Euler(0, 0, 90f);
        _cutTimer = 0f;
        _animator.SetBool("active", true);
    }

    protected override void OnMove(Vector2 pos)
    {
    }

    protected override void OnEnd()
    {
        _animator.SetBool("active", false);
    }
}
