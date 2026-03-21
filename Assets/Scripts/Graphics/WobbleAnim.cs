using UnityEngine;

public class WobbleComponent : MonoBehaviour
{
    [SerializeField] 
    float speed = 15f;
    [SerializeField] 
    float angle = 2f;

    float baseZ;

    void OnEnable()
    {
        baseZ = transform.eulerAngles.z;
    }

    void LateUpdate()
    {
        transform.rotation = Quaternion.Euler(0f, 0f, baseZ + Mathf.Sin(Time.time * speed) * angle);
    }
}
