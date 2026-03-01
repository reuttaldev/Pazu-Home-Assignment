using UnityEngine;

// Abstract base class for all draggable tools (HairDryer, Scissors, HairExtension)
// Subclasses implement the mechanic-specific methods
public abstract class DragLogic : MonoBehaviour
{
    protected Vector2 CurrentWorldPos { get; private set; }
    protected bool IsDragging { get; private set; }

    private Vector3 _restPosition;

    protected virtual void Start()
    {
        _restPosition = transform.position;
    }

    public void OnDragBegin(Vector2 pos)
    {
        IsDragging = true;
        CurrentWorldPos = pos;
        OnBegin(pos);
    }

    public void OnDragMove(Vector2 pos)
    {
        CurrentWorldPos = pos;
        transform.position = new Vector3(pos.x, pos.y, transform.position.z);
        OnMove(pos);

    }

    public void OnDragEnd()
    {
        IsDragging = false;
        transform.position = _restPosition;
        OnEnd();
    }

    protected abstract void OnBegin(Vector2 pos);
    protected abstract void OnMove(Vector2 pos);
    protected abstract void OnEnd();
}
