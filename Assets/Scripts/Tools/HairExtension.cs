using UnityEngine;

public class HairExtension : DraggableTool
{
    [SerializeField] HairManager hairManager;
    [SerializeField] float growRate = 0.5f;
    [SerializeField] float growRadius = 0.6f;

    WobbleComponent _wobble;

    protected override void Start()
    {
        base.Start();
        _wobble = GetComponent<WobbleComponent>();
    }

    protected override void OnBegin(Vector2 pos) => _wobble.enabled = true;

    protected override void OnMove(Vector2 pos)
    {
        //hairManager.GrowHair(pos, growRadius, growRate);
    }

    protected override void OnEnd() => _wobble.enabled = false;
}
