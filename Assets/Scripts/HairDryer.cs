using UnityEngine;

[RequireComponent(typeof(WobbleComponent))]
[RequireComponent(typeof(FaceTarget))]
public class HairDryer : DraggableTool
{
    [SerializeField] float windStrength = 0.3f;
    [SerializeField] float windRadius = 1.5f;

    WobbleComponent _wobble;
    FaceTarget _faceTarget;

    protected override void Start()
    {
        base.Start();
        _wobble = GetComponent<WobbleComponent>();
        _faceTarget = GetComponent<FaceTarget>();
    }

    protected override void OnBegin(Vector2 pos)
    {
        _wobble.enabled = true;
        _faceTarget.enabled = true;
    }

    protected override void OnMove(Vector2 pos) { }

    protected override void OnEnd()
    {
        _wobble.enabled = false;
        _faceTarget.enabled = false;
    }
}
