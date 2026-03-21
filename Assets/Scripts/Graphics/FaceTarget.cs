using UnityEngine;

public class FaceTarget : MonoBehaviour
{
    [SerializeField] Transform target;
    [SerializeField] float speed = 15f;

    [SerializeField] float minAngle = 80f;
    [SerializeField] float maxAngle = 100f;

    [SerializeField] bool flip = true;
    [SerializeField] bool defaultFacingRight = false;

    float zRotation;

    void Update()
    {
        zRotation = Mathf.LerpAngle(zRotation, AngleToTarget(), Time.deltaTime * speed);
        transform.rotation = Quaternion.Euler(0f, 0f, zRotation);

        float sign;
        if (flip)
            sign = target.position.x > transform.position.x ? -1f : 1f;
        else
            sign = defaultFacingRight ? 1f : -1f;
        Vector3 s = transform.localScale;
        transform.localScale = new Vector3(s.x, sign * Mathf.Abs(s.y), s.z);
    }

float AngleToTarget()
{
    Vector2 dir = target.position - transform.position;
    float targetAngle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
    float sign = targetAngle >= 0f ? 1f : -1f;
    float clamped = Mathf.Clamp(Mathf.Abs(targetAngle), minAngle, maxAngle);
    return sign * clamped;
}
}
