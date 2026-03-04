using UnityEngine;

public class WobbleComponent : MonoBehaviour
{
    [SerializeField] 
    float speed = 15f;
    [SerializeField] 
    float angle = 2f;

    void LateUpdate()
    {
        Vector3 euler = transform.localEulerAngles;
        euler.z += Mathf.Sin(Time.time * speed) * angle;
        transform.localEulerAngles = euler;
    }
}
