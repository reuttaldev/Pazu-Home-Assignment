using UnityEngine;

public class HairExtension : DraggableTool
{
    WobbleComponent _wobble;

    protected override void Start()
    {
        base.Start();
        _wobble = GetComponent<WobbleComponent>();
    }

    protected override void OnBegin(Vector2 pos) => _wobble.enabled = true;
    protected override void OnMove(Vector2 pos) { }
    protected override void OnEnd() => _wobble.enabled = false;
}
