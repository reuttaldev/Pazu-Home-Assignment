using UnityEngine;

public class HairExtension : DraggableTool
{
    [SerializeField] HairManager hairManager;
    [SerializeField] float growCooldown = 0.08f;
    [SerializeField] ParticleSystem growParticles;
    WobbleComponent wobble;
    float growTimer;

    protected override void Awake()
    {
        base.Awake();
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
            bool hit = hairManager.GrowHair(pos);
            growTimer = growCooldown;
            if (growParticles != null)
            {
                if (hit && !growParticles.isPlaying) growParticles.Play();
                else if (!hit && growParticles.isPlaying) growParticles.Stop();
            }
        }
    }

    protected override void OnEnd()
    {
        wobble.enabled = false;
        if (growParticles != null) growParticles.Stop();  // ensure stopped on release
    }
}
