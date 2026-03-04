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
    ParticleSystem fanParticles;

    protected void Awake()
    {
        wobble = GetComponent<WobbleComponent>();
        faceTarget = GetComponent<FaceTarget>();
        Transform ps = transform.Find("FanParticles");
        if (ps != null) fanParticles = ps.GetComponent<ParticleSystem>();
    }
    protected override void OnBegin(Vector2 pos){}
    protected override void OnMove(Vector2 pos)
    {
        wobble.enabled = true;
        faceTarget.enabled = true;
        if (fanParticles != null && !fanParticles.isPlaying)
            fanParticles.Play();
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
        if (fanParticles != null) fanParticles.Stop();
    }
}
