using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class BuildController : MonoBehaviour
{
    public enum BuildInputMode { Single, Drag }

    [Header("Mode")]
    [SerializeField] private BuildInputMode mode = BuildInputMode.Drag;

    [Header("Refs")]
    [SerializeField] private GridManager gridManager;
    [SerializeField] private GridPointer gridPointer;

    [Header("Input (Build Map)")]
    [SerializeField] private InputActionReference placeAction;   // LMB
    [SerializeField] private InputActionReference removeAction;  // RMB
    [SerializeField] private InputActionReference toggleModeAction; // 例: Tab

    [Header("Prefab")]
    [SerializeField] private GameObject wallPrefab;
    [SerializeField] private float spawnYOffset;

    private readonly Dictionary<Vector2Int, GameObject> placed = new();
    public bool IsOccupied(Vector2Int cell) => placed.ContainsKey(cell);

    // --- drag state ---
    private bool isPlacing;
    private bool isRemoving;
    private bool hasLastCell;
    private Vector2Int lastCell;

    private void OnEnable()
    {
        placeAction?.action.Enable();
        removeAction?.action.Enable();
        toggleModeAction?.action.Enable();

        // Single用
        if (placeAction != null) placeAction.action.performed += OnPlaceSingle;
        if (removeAction != null) removeAction.action.performed += OnRemoveSingle;

        // Drag用
        if (placeAction != null)
        {
            placeAction.action.started += OnPlaceStarted;
            placeAction.action.canceled += OnPlaceCanceled;
        }
        if (removeAction != null)
        {
            removeAction.action.started += OnRemoveStarted;
            removeAction.action.canceled += OnRemoveCanceled;
        }

        // モード切替
        if (toggleModeAction != null) toggleModeAction.action.performed += OnToggleMode;
    }

    private void OnDisable()
    {
        if (placeAction != null) placeAction.action.performed -= OnPlaceSingle;
        if (removeAction != null) removeAction.action.performed -= OnRemoveSingle;

        if (placeAction != null)
        {
            placeAction.action.started -= OnPlaceStarted;
            placeAction.action.canceled -= OnPlaceCanceled;
        }
        if (removeAction != null)
        {
            removeAction.action.started -= OnRemoveStarted;
            removeAction.action.canceled -= OnRemoveCanceled;
        }

        if (toggleModeAction != null) toggleModeAction.action.performed -= OnToggleMode;

        placeAction?.action.Disable();
        removeAction?.action.Disable();
        toggleModeAction?.action.Disable();
    }

    private void Update()
    {
        if (mode != BuildInputMode.Drag) return; // ← Dragモードの時だけ連続処理

        if (!isActiveAndEnabled) return;
        if (gridManager == null || gridPointer == null) return;

        if (!isPlacing && !isRemoving)
        {
            hasLastCell = false;
            return;
        }

        if (!gridPointer.TryGetCellUnderPointer(out var cell, out _))
        {
            hasLastCell = false;
            return;
        }
        if (!gridManager.IsInside(cell)) return;

        if (hasLastCell && cell == lastCell) return;

        lastCell = cell;
        hasLastCell = true;

        if (isRemoving)
        {
            Remove(cell);
        }
        else if (isPlacing)
        {
            if (!IsOccupied(cell))
                PlaceWall(cell);
        }
    }

    // -------------------------
    // Single (performed) handlers
    // -------------------------
    private void OnPlaceSingle(InputAction.CallbackContext ctx)
    {
        if (mode != BuildInputMode.Single) return;

        if (!isActiveAndEnabled) return;
        if (gridManager == null || gridPointer == null) return;

        if (!gridPointer.TryGetCellUnderPointer(out var cell, out _)) return;
        if (!gridManager.IsInside(cell)) return;
        if (IsOccupied(cell)) return;

        PlaceWall(cell);
    }

    private void OnRemoveSingle(InputAction.CallbackContext ctx)
    {
        if (mode != BuildInputMode.Single) return;

        if (!isActiveAndEnabled) return;
        if (gridManager == null || gridPointer == null) return;

        if (!gridPointer.TryGetCellUnderPointer(out var cell, out _)) return;
        if (!gridManager.IsInside(cell)) return;

        Remove(cell);
    }

    // -------------------------
    // Drag (started/canceled) handlers
    // -------------------------
    private void OnPlaceStarted(InputAction.CallbackContext ctx)
    {
        if (mode != BuildInputMode.Drag) return;
        isPlacing = true;
    }

    private void OnPlaceCanceled(InputAction.CallbackContext ctx)
    {
        isPlacing = false;
        hasLastCell = false;
    }

    private void OnRemoveStarted(InputAction.CallbackContext ctx)
    {
        if (mode != BuildInputMode.Drag) return;
        isRemoving = true;
    }

    private void OnRemoveCanceled(InputAction.CallbackContext ctx)
    {
        isRemoving = false;
        hasLastCell = false;
    }

    // -------------------------
    // Mode toggle
    // -------------------------
    private void OnToggleMode(InputAction.CallbackContext ctx)
    {
        mode = (mode == BuildInputMode.Single) ? BuildInputMode.Drag : BuildInputMode.Single;

        // 切替時にドラッグ状態をリセット（誤爆防止）
        isPlacing = false;
        isRemoving = false;
        hasLastCell = false;

        Debug.Log($"Build Input Mode: {mode}");
    }

    // -------------------------
    // Place / Remove
    // -------------------------
    private void PlaceWall(Vector2Int cell)
    {
        if (wallPrefab == null || gridManager == null) return;

        Remove(cell);

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
