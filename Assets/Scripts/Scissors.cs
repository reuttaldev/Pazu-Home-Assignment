using UnityEngine;

[RequireComponent(typeof(Animator))]
public class Scissors : DraggableTool
{
    Animator _animator;

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
        _animator.SetBool("active", true);
    }

    protected override void OnMove(Vector2 pos) { }

    protected override void OnEnd()
    {
        _animator.SetBool("active", false);
    }
}
