using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;

public class BuildController : MonoBehaviour
{
    public enum BuildInputMode { Single, Drag }

    [Header("Mode")]
    [SerializeField] private BuildInputMode mode = BuildInputMode.Drag;

    [Header("Refs")]
    [SerializeField] private GridManager gridManager;
    [SerializeField] private GridPointer gridPointer;

    [Header("Raycast (Minecraft placement)")]
    [SerializeField] private Camera cam;
    [SerializeField] private LayerMask blockLayerMask; 
    [SerializeField] private float rayDistance = 200f;

    [Header("Input (Build Map)")]
    [SerializeField] private InputActionReference placeAction;        // LMB
    [SerializeField] private InputActionReference removeAction;       // RMB
    [SerializeField] private InputActionReference toggleModeAction;   // Tab

    [Header("Block Prefabs (different types)")]
    [SerializeField] private List<GameObject> groundPrefabs = new();
    [SerializeField] private int selectedGroundIndex = 0;

    [Header("Stacking / Size")]
    [SerializeField] private float blockHeight = 1f;
    [SerializeField] private float baseYOffset = 0f;

    [Header("Debug")]
    [SerializeField] private bool debugRaycastLogs = false;

    public float BlockHeight => blockHeight;
    public float BaseYOffset => baseYOffset;

    // (x,y,z) -> GameObject
    private readonly Dictionary<Vector3Int, GameObject> placed = new();

    // ✅ (x,z) -> topY キャッシュ（FindTopYループを無くす）
    private readonly Dictionary<Vector2Int, int> topYCache = new();

    // --- drag state ---
    private bool isPlacing;
    private bool isRemoving;
    private bool hasLastCell;
    private Vector2Int lastCell;

    private void Awake()
    {
        if (cam == null) cam = Camera.main;
    }

    private void OnEnable()
    {
        placeAction?.action.Enable();
        removeAction?.action.Enable();
        toggleModeAction?.action.Enable();

        if (placeAction != null) placeAction.action.performed += OnPlaceSingle;
        if (removeAction != null) removeAction.action.performed += OnRemoveSingle;

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
        if (mode != BuildInputMode.Drag) return;

        if (IsPointerOverUI())
        {
            ResetDragState();
            return;
        }

        if (!isActiveAndEnabled) return;

        if (!isPlacing && !isRemoving)
        {
            hasLastCell = false;
            return;
        }

        
        if (TryGetRaycastTarget(out _, out var placeCell, out var removeCell))
        {
            if (isRemoving) Remove(removeCell);
            else if (isPlacing) PlaceBlock(placeCell);
            return;
        }

        // ✅ fallback（床セルを使って柱の上に積む/外す）
        if (gridManager == null || gridPointer == null) return;

        if (!gridPointer.TryGetCellUnderPointer(out var cell2, out _))
        {
            hasLastCell = false;
            return;
        }

        if (!gridManager.IsInside(cell2)) return;
        if (hasLastCell && cell2 == lastCell) return;

        lastCell = cell2;
        hasLastCell = true;

        if (isRemoving) RemoveTop(cell2);
        else if (isPlacing) PlaceOnTop(cell2);
    }

    private void ResetDragState()
    {
        isPlacing = false;
        isRemoving = false;
        hasLastCell = false;
    }

    // -------------------------
    // Public API for Highlighter
    // -------------------------
    public int GetTopY(Vector2Int cell2)
    {
        return topYCache.TryGetValue(cell2, out var y) ? y : -1;
    }

    /// <summary>床セル(cell2)の「次に置く3Dセル」</summary>
    public Vector3Int GetNextPlaceCellFromFloor(Vector2Int cell2)
    {
        int topY = GetTopY(cell2);
        return new Vector3Int(cell2.x, topY + 1, cell2.y);
    }

    public bool CanPlaceAt3D(Vector3Int cell3)
    {
        // 今は placed だけで判定（＝埋まってなければ置ける）
        // もし gridManager に3D判定があるならここで呼ぶ

        return !placed.ContainsKey(cell3);
    }

    // -------------------------
    // Single handlers
    // -------------------------
    private void OnPlaceSingle(InputAction.CallbackContext ctx)
    {
        if (mode != BuildInputMode.Single) return;
        if (!isActiveAndEnabled) return;
        if (IsPointerOverUI()) return;

        if (TryGetRaycastTarget(out _, out var placeCell, out _))
        {
            PlaceBlock(placeCell);
            return;
        }

        if (gridManager == null || gridPointer == null) return;
        if (!gridPointer.TryGetCellUnderPointer(out var cell2, out _)) return;
        if (!gridManager.IsInside(cell2)) return;

        PlaceOnTop(cell2);
    }

    private void OnRemoveSingle(InputAction.CallbackContext ctx)
    {
        if (mode != BuildInputMode.Single) return;
        if (!isActiveAndEnabled) return;
        if (IsPointerOverUI()) return;

        if (TryGetRaycastTarget(out _, out _, out var removeCell))
        {
            Remove(removeCell);
            return;
        }

        if (gridManager == null || gridPointer == null) return;
        if (!gridPointer.TryGetCellUnderPointer(out var cell2, out _)) return;
        if (!gridManager.IsInside(cell2)) return;

        RemoveTop(cell2);
    }

    // -------------------------
    // Drag handlers
    // -------------------------
    private void OnPlaceStarted(InputAction.CallbackContext ctx)
    {
        if (mode != BuildInputMode.Drag) return;
        if (IsPointerOverUI()) return;
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
        if (IsPointerOverUI()) return;
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
        ResetDragState();
        Debug.Log($"Build Input Mode: {mode}");
    }

    // =========================================================
    // Raycast target (Block only)
    // =========================================================
    private bool TryGetRaycastTarget(out Vector3Int hitCell, out Vector3Int placeCell, out Vector3Int removeCell)
    {
        hitCell = default;
        placeCell = default;
        removeCell = default;

        if (cam == null) return false;

        Ray ray = cam.ScreenPointToRay(Mouse.current.position.ReadValue());
        Debug.DrawRay(ray.origin, ray.direction * rayDistance, Color.red, 0.1f);

        if (!Physics.Raycast(ray, out var hit, rayDistance, blockLayerMask, QueryTriggerInteraction.Ignore))
        {
            if (debugRaycastLogs) Debug.Log("Raycast hit nothing");
            return false;
        }

        var bi = hit.collider.GetComponentInParent<BlockInstance>();
        if (bi == null)
        {
            if (debugRaycastLogs) Debug.LogWarning($"Hit but no BlockInstance: {hit.collider.name}");
            return false;
        }

        hitCell = bi.Cell;

        Vector3Int normalInt = NormalToInt(hit.normal);
        placeCell = hitCell + normalInt;
        removeCell = hitCell;

        return true;
    }

    private static Vector3Int NormalToInt(Vector3 n)
    {
        n = n.normalized;
        float ax = Mathf.Abs(n.x);
        float ay = Mathf.Abs(n.y);
        float az = Mathf.Abs(n.z);

        if (ax >= ay && ax >= az) return new Vector3Int(Mathf.RoundToInt(n.x), 0, 0);
        if (ay >= ax && ay >= az) return new Vector3Int(0, Mathf.RoundToInt(n.y), 0);
        return new Vector3Int(0, 0, Mathf.RoundToInt(n.z));
    }

    // =========================================================
    // fallback（柱の一番上）
    // =========================================================
    private void PlaceOnTop(Vector2Int cell2)
    {
        Vector3Int cell3 = GetNextPlaceCellFromFloor(cell2);
        PlaceBlock(cell3);
    }

    private void RemoveTop(Vector2Int cell2)
    {
        int topY = GetTopY(cell2);
        if (topY < 0) return;

        Vector3Int cell3 = new Vector3Int(cell2.x, topY, cell2.y);
        Remove(cell3);
    }

    // =========================================================
    // Spawn / Remove
    // =========================================================
    private void PlaceBlock(Vector3Int cell3)
    {
        if (gridManager == null) return;
        if (groundPrefabs == null || groundPrefabs.Count == 0) return;

        if (placed.ContainsKey(cell3)) return;

        selectedGroundIndex = Mathf.Clamp(selectedGroundIndex, 0, groundPrefabs.Count - 1);
        var prefab = groundPrefabs[selectedGroundIndex];
        if (prefab == null) return;

        Vector3 pos = gridManager.CellToWorldCenter(new Vector2Int(cell3.x, cell3.z));
        pos.y += baseYOffset + (cell3.y * blockHeight);

        GameObject obj = Instantiate(prefab, pos, Quaternion.identity);
        obj.name = $"Block_{selectedGroundIndex}_{cell3.x}_{cell3.y}_{cell3.z}";

        var bi = obj.GetComponent<BlockInstance>();
        if (bi == null) bi = obj.AddComponent<BlockInstance>();
        bi.SetCell(cell3);

        placed[cell3] = obj;

        // ✅ topYキャッシュ更新
        Vector2Int col = new Vector2Int(cell3.x, cell3.z);
        if (!topYCache.TryGetValue(col, out var curTop) || cell3.y > curTop)
            topYCache[col] = cell3.y;
    }

    private void Remove(Vector3Int cell3)
    {
        if (placed.TryGetValue(cell3, out var obj) && obj != null)
            Destroy(obj);

        placed.Remove(cell3);

        // ✅ topYキャッシュ再計算（その柱のトップを消した時だけ）
        Vector2Int col = new Vector2Int(cell3.x, cell3.z);
        if (topYCache.TryGetValue(col, out var curTop) && curTop == cell3.y)
        {
            int newTop = -1;
            foreach (var key in placed.Keys)
            {
                if (key.x == col.x && key.z == col.y)
                    if (key.y > newTop) newTop = key.y;
            }

            if (newTop < 0) topYCache.Remove(col);
            else topYCache[col] = newTop;
        }
    }

    public void NextGround()
    {
        if (groundPrefabs == null || groundPrefabs.Count == 0) return;
        selectedGroundIndex = (selectedGroundIndex + 1) % groundPrefabs.Count;
    }

    public void PrevGround()
    {
        if (groundPrefabs == null || groundPrefabs.Count == 0) return;
        selectedGroundIndex = (selectedGroundIndex - 1 + groundPrefabs.Count) % groundPrefabs.Count;
    }

    private bool IsPointerOverUI()
    {
        return EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
    }
}
