using UnityEngine;

[RequireComponent(typeof(WobbleComponent))]
[RequireComponent(typeof(FaceTarget))]
public class HairDryer : DraggableTool
{
    [SerializeField] HairManager hairManager;
    [SerializeField] ParticleSystem fanParticles;
    WobbleComponent wobble;
    FaceTarget faceTarget;

    protected void Awake()
    {
        wobble = GetComponent<WobbleComponent>();
        faceTarget = GetComponent<FaceTarget>();
        faceTarget.enabled = false;
    }
    protected override void OnBegin(Vector2 pos)
    {
        wobble.enabled = true;
        faceTarget.enabled = true;
        if (fanParticles != null) fanParticles.Play();
    }
    protected override void OnMove(Vector2 pos) { }

    void Update()
    {
        if(IsDragging)
            hairManager.ApplyWind((Vector2)transform.position);
    }

    protected override void OnEnd()
    {
        wobble.enabled = false;
        faceTarget.enabled = false;
        if (fanParticles != null) fanParticles.Stop();
    }
}
