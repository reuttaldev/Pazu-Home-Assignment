using UnityEngine;

public class HairExtension : DraggableTool
{
    [SerializeField] HairManager hairManager;
    [SerializeField] float growCooldown = 0.08f;
    [SerializeField] Vector2 offset;
    public Vector2 Offset => offset;

    WobbleComponent wobble;
    float growTimer;

    protected override void Start()
    {
        base.Start();
        wobble = GetComponent<WobbleComponent>();
    }

    protected override void OnBegin(Vector2 pos)
    {
        wobble.enabled = true;
        growTimer = 0f;
    }

    protected override void OnMove(Vector2 pos)
    {
        growTimer -= Time.deltaTime;
        if (growTimer <= 0f)
        {
            hairManager.GrowHair(pos + offset);
            growTimer = growCooldown;
        }
    }

    protected override void OnEnd() => wobble.enabled = false;
}
