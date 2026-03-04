using UnityEngine;

[RequireComponent(typeof(WobbleComponent))]
[RequireComponent(typeof(FaceTarget))]
public class HairDryer : DraggableTool
{
    [SerializeField] HairManager hairManager;
    [SerializeField] Vector2 offset;
    public Vector2 Offset => (Vector2)(transform.rotation * (Vector3)offset);

    WobbleComponent wobble;
    FaceTarget faceTarget;

    protected void Awake()
    {
        wobble = GetComponent<WobbleComponent>();
        faceTarget = GetComponent<FaceTarget>();
    }
    protected override void OnBegin(Vector2 pos){}
    protected override void OnMove(Vector2 pos)
    {
        wobble.enabled = true;
        faceTarget.enabled = true;
    }

    void Update()
    {
        if(IsDragging)
            hairManager.ApplyWind((Vector2)transform.position + Offset);
    }

    protected override void OnEnd()
    {
        wobble.enabled = false;
        faceTarget.enabled = false;
    }
}
