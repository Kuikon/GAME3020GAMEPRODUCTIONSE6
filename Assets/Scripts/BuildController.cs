using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class BuildController : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private GridManager gridManager;
    [SerializeField] private GridPointer gridPointer; // Åöí«â¡

    [Header("Input (Build Map)")]
    [SerializeField] private InputActionReference placeAction;  // Build/Place
    [SerializeField] private InputActionReference removeAction; // Build/Remove

    [Header("Prefab")]
    [SerializeField] private GameObject wallPrefab;

    private readonly Dictionary<Vector2Int, GameObject> placed = new();

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
        if (gridPointer == null) return;

        if (gridPointer.TryGetCellUnderPointer(out var cell, out _))
            PlaceWall(cell);
    }

    private void OnRemove(InputAction.CallbackContext ctx)
    {
        if (!isActiveAndEnabled) return;
        if (gridPointer == null) return;

        if (gridPointer.TryGetCellUnderPointer(out var cell, out _))
            Remove(cell);
    }

    private void PlaceWall(Vector2Int cell)
    {
        if (wallPrefab == null || gridManager == null) return;

        Remove(cell); // è„èëÇ´

        Vector3 pos = gridManager.CellToWorldCenter(cell);
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
}
