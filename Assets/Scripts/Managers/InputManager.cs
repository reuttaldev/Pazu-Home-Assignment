using UnityEngine;
using UnityEngine.InputSystem;

public class InputManager : MonoBehaviour
{
    [SerializeField] Camera mainCamera;
    [SerializeField] LayerMask toolLayerMask;

    DraggableTool activeTool;
    bool dragging;

    void Update()
    {
        if (Pointer.current == null)
            return;

        var pointer = Pointer.current;

        Vector2 screenPos = pointer.position.ReadValue();
        Vector2 worldPos = ScreenToWorld(screenPos);

        // ===== Press =====
        if (!dragging && pointer.press.wasPressedThisFrame)
        {
            Collider2D hit = Physics2D.OverlapPoint(worldPos, toolLayerMask);

            if (hit != null && hit.TryGetComponent(out DraggableTool tool))
            {
                activeTool = tool;
                dragging = true;
                activeTool.OnDragBegin(worldPos);
            }
        }

        // ===== Drag =====
        else if (dragging)
        {
            if (pointer.press.wasReleasedThisFrame)
            {
                activeTool.OnDragEnd();
                activeTool = null;
                dragging = false;
                return;
            }

            activeTool.OnDragMove(worldPos);
        }
    }

    Vector2 ScreenToWorld(Vector2 screenPos)
    {
        return mainCamera.ScreenToWorldPoint(
            new Vector3(screenPos.x, screenPos.y, -mainCamera.transform.position.z)
        );
    }
}