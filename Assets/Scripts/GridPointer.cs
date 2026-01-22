using UnityEngine;
using UnityEngine.InputSystem;

public class GridPointer : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private GridManager gridManager;
    [SerializeField] private Camera cam;

    [Header("Input")]
    [SerializeField] private InputActionReference pointAction; // Build/Point

    private void Awake()
    {
        if (cam == null) cam = Camera.main;
    }

    private void OnEnable()
    {
        pointAction?.action.Enable();
    }

    private void OnDisable()
    {
        pointAction?.action.Disable();
    }
    public bool TryGetCellUnderPointer(out Vector2Int cell, out Vector3 hitWorldPos)
    {
        cell = default;
        hitWorldPos = default;

        if (gridManager == null || cam == null || pointAction == null) return false;

        Vector2 screenPos = pointAction.action.ReadValue<Vector2>();
        Ray ray = cam.ScreenPointToRay(screenPos);

        Plane ground = new Plane(Vector3.up, new Vector3(0f, gridManager.GridY, 0f));
        if (!ground.Raycast(ray, out float enter)) return false;

        hitWorldPos = ray.GetPoint(enter);
        cell = gridManager.WorldToCell(hitWorldPos);

        // 範囲チェックがあるならここで
        // if (!gridManager.IsInside(cell)) return false;

        return true;
    }
}
