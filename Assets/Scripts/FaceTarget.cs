using UnityEngine;

public class FaceTarget : MonoBehaviour
{
    [SerializeField] Transform target;
    [SerializeField] float speed = 15f;
    [SerializeField] Vector2 spriteForward = Vector2.up;

    float _smoothedAngle;

    void Update()
    {
        _smoothedAngle = Mathf.LerpAngle(_smoothedAngle, AngleToTarget(), Time.deltaTime * speed);
        transform.rotation = Quaternion.Euler(0, 0, _smoothedAngle);
    }

    float AngleToTarget()
    {
        Vector2 targetPos = target != null ? (Vector2)target.position : Vector2.zero;
        Vector2 dir = targetPos - (Vector2)transform.position;
        // target depends on where the sprite is originally pointing to
        float forwardOffset = Mathf.Atan2(spriteForward.y, spriteForward.x) * Mathf.Rad2Deg;
        return Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg - forwardOffset;
    }
}
