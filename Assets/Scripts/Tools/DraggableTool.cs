using UnityEngine;

// Abstract base class for all draggable tools (HairDryer, Scissors, HairExtension)
// Subclasses implement the mechanic-specific methods
[RequireComponent(typeof(Collider2D))]
[RequireComponent(typeof(SpriteRenderer))]

public abstract class DraggableTool : MonoBehaviour
{
    protected bool IsDragging { get; private set; }
    private Vector3 restPosition;
    private Quaternion restRotation;
    private Vector3 restScale;

    protected virtual void Awake()
    {
        restPosition = transform.position;
        restRotation = transform.rotation;
        restScale = transform.localScale;
    }

    public void OnDragBegin(Vector2 pos)
    {
        IsDragging = true;
        OnBegin(pos);
    }

    public void OnDragMove(Vector2 pos)
    {
        transform.position = new Vector3(pos.x, pos.y, transform.position.z);
        OnMove(pos);
    }

    public void OnDragEnd()
    {
        IsDragging = false;
        OnEnd();
        transform.SetPositionAndRotation(restPosition, restRotation);
        transform.localScale = restScale;
    }
    protected abstract void OnBegin(Vector2 pos);
    protected abstract void OnMove(Vector2 pos);
    protected abstract void OnEnd();
}
