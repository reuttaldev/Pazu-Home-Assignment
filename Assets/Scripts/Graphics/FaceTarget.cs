using UnityEngine;

public class FaceTarget : MonoBehaviour
{
    [SerializeField] Transform target;
    [SerializeField] float speed = 15f;
    [SerializeField] float spriteAngleOffset = 0f;

    [SerializeField] float minAngle = 0f;
    [SerializeField] float maxAngle = 180f;

    float zRotation;

    void Update()
    {
        zRotation = Mathf.LerpAngle(zRotation, AngleToTarget()-spriteAngleOffset, Time.deltaTime * speed);
        transform.rotation = Quaternion.Euler(0f, 0f, zRotation);
    }

    float AngleToTarget()
    {
        Vector2 dir = target.position - transform.position;
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        float result;
        //if (dir.x >= 0f)
            result = Mathf.Clamp(angle, minAngle, maxAngle);
        //else
        //    result = Mathf.Clamp(angle + 90f, minAngle + 90f, maxAngle + 90f);

        // Normalize to [-180, 180] so LerpAngle never sees a 360° difference and gets stuck
        return Mathf.DeltaAngle(0f, result);
    }
}