using UnityEngine;

[RequireComponent(typeof(FaceTarget))]
public class HairDryer : DraggableTool
{
    [SerializeField] HairManager hairManager;
    [SerializeField] ParticleSystem fanParticles;
    FaceTarget faceTarget;

    protected override void Awake()
    {
        base.Awake();
        faceTarget = GetComponent<FaceTarget>();
        faceTarget.enabled = false;
    }
    protected override void OnBegin(Vector2 pos)
    {
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
        faceTarget.enabled = false;
        if (fanParticles != null) fanParticles.Stop();
    }
}
