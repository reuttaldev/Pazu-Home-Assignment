using UnityEngine;
using UnityEngine.InputSystem;

public class InputManager : MonoBehaviour
{
    [SerializeField] Camera mainCamera;
    [SerializeField] LayerMask toolLayerMask;

    DragLogic _activeTool;
    bool _dragging;

    void Update()
    {
        if (Pointer.current == null)
            return;

        var pointer = Pointer.current;

        Vector2 screenPos = pointer.position.ReadValue();
        Vector2 worldPos = ScreenToWorld(screenPos);

        // ===== Press =====
        if (!_dragging && pointer.press.wasPressedThisFrame)
        {
            Collider2D hit = Physics2D.OverlapPoint(worldPos, toolLayerMask);
            Debug.Log($"Pointer down {screenPos} → {worldPos} hit {(hit ? hit.name : "null")}");

            if (hit != null && hit.TryGetComponent(out DragLogic tool))
            {
                _activeTool = tool;
                _dragging = true;
                _activeTool.OnDragBegin(worldPos);
            }
        }

        // ===== Drag =====
        else if (_dragging)
        {
            if (pointer.press.wasReleasedThisFrame)
            {
                _activeTool.OnDragEnd();
                _activeTool = null;
                _dragging = false;
                return;
            }

            _activeTool.OnDragMove(worldPos);
        }
    }

    Vector2 ScreenToWorld(Vector2 screenPos)
    {
        return mainCamera.ScreenToWorldPoint(
            new Vector3(screenPos.x, screenPos.y, -mainCamera.transform.position.z)
        );
    }
}