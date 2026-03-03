using UnityEngine;

[RequireComponent(typeof(WobbleComponent))]
[RequireComponent(typeof(FaceTarget))]
public class HairDryer : DraggableTool
{
    [SerializeField] HairManager hairManager;
    [SerializeField] Vector2 offset;

    WobbleComponent wobble;
    FaceTarget faceTarget;

    protected override void Start()
    {
        base.Start();
        wobble = GetComponent<WobbleComponent>();
        faceTarget = GetComponent<FaceTarget>();
    }

    protected override void OnBegin(Vector2 pos)
    {
        wobble.enabled = true;
        faceTarget.enabled = true;
    }

    protected override void OnMove(Vector2 pos)
    {
        hairManager.ApplyWind(pos + offset);
    }

    protected override void OnEnd()
    {
        wobble.enabled = false;
        faceTarget.enabled = false;
    }
}
