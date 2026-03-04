using UnityEngine;

// Abstract base class for all draggable tools (HairDryer, Scissors, HairExtension)
// Subclasses implement the mechanic-specific methods
[RequireComponent(typeof(Collider2D))]
[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(FaceTarget))]

public abstract class DraggableTool : MonoBehaviour
{
    protected bool IsDragging { get; private set; }
    private Vector3 restPosition;
    private Quaternion restRotation;

    protected virtual void Start()
    {
        restPosition = transform.position;
        restRotation = transform.rotation;
        GetComponent<FaceTarget>().enabled = false; // subclasses enable it during drag as needed
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
        transform.SetPositionAndRotation(restPosition, restRotation);
        OnEnd();
    }
    protected abstract void OnBegin(Vector2 pos);
    protected abstract void OnMove(Vector2 pos);
    protected abstract void OnEnd();
}
