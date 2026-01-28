using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class BuildController : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private GridManager gridManager;
    [SerializeField] private GridPointer gridPointer; // ★追加

    [Header("Input (Build Map)")]
    [SerializeField] private InputActionReference placeAction;  // Build/Place
    [SerializeField] private InputActionReference removeAction; // Build/Remove

    [Header("Prefab")]
    [SerializeField] private GameObject wallPrefab;
    [SerializeField] private float spawnYOffset;
    private readonly Dictionary<Vector2Int, GameObject> placed = new();
    public bool IsOccupied(Vector2Int cell) => placed.ContainsKey(cell);
    private void OnEnable()
    {
        placeAction?.action.Enable();
        removeAction?.action.Enable();

        if (placeAction != null) placeAction.action.performed += OnPlace;
        if (removeAction != null) removeAction.action.performed += OnRemove;
    }

    private void OnDisable()
    {
        if (placeAction != null) placeAction.action.performed -= OnPlace;
        if (removeAction != null) removeAction.action.performed -= OnRemove;

        placeAction?.action.Disable();
        removeAction?.action.Disable();
    }

    private void OnPlace(InputAction.CallbackContext ctx)
    {
        if (!isActiveAndEnabled) return;
        if (gridManager == null || gridPointer == null) return;

        if (!gridPointer.TryGetCellUnderPointer(out var cell, out _)) return;

        // 範囲外禁止
        if (!gridManager.IsInside(cell)) return;

        // すでに何かあるなら置けない（枠も含む）
        if (IsOccupied(cell)) return;
        PlaceWall(cell);
    }

    private void OnRemove(InputAction.CallbackContext ctx)
    {
        if (!isActiveAndEnabled) return;
        if (gridManager == null || gridPointer == null) return;

        if (!gridPointer.TryGetCellUnderPointer(out var cell, out _)) return;

        if (!gridManager.IsInside(cell)) return;

        Remove(cell);
    }

    private void PlaceWall(Vector2Int cell)
    {
        if (wallPrefab == null || gridManager == null) return;

        Remove(cell); // 上書き

        Vector3 pos = gridManager.CellToWorldCenter(cell);
        pos.y += spawnYOffset;
        GameObject obj = Instantiate(wallPrefab, pos, Quaternion.identity);
        obj.name = $"Wall_{cell.x}_{cell.y}";
        placed[cell] = obj;
    }

    private void Remove(Vector2Int cell)
    {
        if (placed.TryGetValue(cell, out var obj) && obj != null)
            Destroy(obj);

        placed.Remove(cell);
    }
    public void RegisterPlaced(Vector2Int cell, GameObject obj)
    {
        if (obj == null) return;
        if (placed.ContainsKey(cell)) return;

        placed[cell] = obj;
    }

}
