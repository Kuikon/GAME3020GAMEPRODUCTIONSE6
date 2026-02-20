// =========================================================
// BuildController.cs
// - Uses ObjectsDatabaseSO (ID -> Prefab + SizeXYZ)
// - Grid cell size is fixed; objects can occupy multiple cells
// - Place at "outer cell" determined by hit.point + hit.normal * epsilon
// - Occupancy tracked by Dictionary<Vector3Int, GameObject> occupied
// =========================================================
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public class BuildController : MonoBehaviour
{
    public enum BuildInputMode { Single, Drag }

    [Header("Mode")]
    [SerializeField] private BuildInputMode mode = BuildInputMode.Drag;

    [Header("Refs")]
    [SerializeField] private GridManager grid;
    [SerializeField] private Camera cam;

    [Header("Raycast")]
    [SerializeField] private float rayDistance = 200f;
    [SerializeField] private LayerMask placeMask;     // Block + Ground
    [SerializeField] private LayerMask blockOnlyMask; // Block only

    [Header("Input (Build Map)")]
    [SerializeField] private InputActionReference placeAction;        // LMB
    [SerializeField] private InputActionReference removeAction;       // RMB
    [SerializeField] private InputActionReference toggleModeAction;   // Tab

    [Header("Database (SO)")]
    [SerializeField] private ObjectsDatabaseSO database;
    [SerializeField] private int selectedObjectID = 0;

    [Header("Ground rule")]
    [SerializeField] private int groundYCell = 0; // ground placement locks y to this value (optional)

    [Header("Debug")]
    [SerializeField] private bool debugLogs = true;

    // 1セル -> そのセルを占有しているオブジェクトRoot
    private readonly Dictionary<Vector3Int, GameObject> occupied = new();

    // --- drag state ---
    private bool isPlacing;
    private bool isRemoving;
    private bool hasLastCell;
    private Vector3Int lastCell;

    private void Awake()
    {
        EnsureCamera();
    }

    private void OnEnable()
    {
        EnableActions();
        SubscribeActions();
    }

    private void OnDisable()
    {
        UnsubscribeActions();
        DisableActions();
    }

    private void Update()
    {
        if (!IsActiveForBuild()) return;
        if (!IsDragMode()) return;
        if (!IsDraggingAnything()) { ClearLastCell(); return; }

        HandleDragTick();
    }

    // =========================================================
    // Setup helpers
    // =========================================================
    private void EnsureCamera()
    {
        if (cam == null) cam = Camera.main;
    }

    private void EnableActions()
    {
        placeAction?.action.Enable();
        removeAction?.action.Enable();
        toggleModeAction?.action.Enable();
    }

    private void DisableActions()
    {
        placeAction?.action.Disable();
        removeAction?.action.Disable();
        toggleModeAction?.action.Disable();
    }

    private void SubscribeActions()
    {
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

    private void UnsubscribeActions()
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
    }

    // =========================================================
    // Update flow helpers
    // =========================================================
    private bool IsActiveForBuild()
    {
        if (!isActiveAndEnabled) return false;
        if (IsPointerOverUI()) { ResetDragState(); return false; }
        return true;
    }

    private bool IsDragMode()
    {
        return mode == BuildInputMode.Drag;
    }

    private bool IsDraggingAnything()
    {
        return isPlacing || isRemoving;
    }

    private void ClearLastCell()
    {
        hasLastCell = false;
    }

    private void HandleDragTick()
    {
        if (isRemoving) HandleDragRemove();
        else if (isPlacing) HandleDragPlace();
    }

    private void HandleDragPlace()
    {
        if (!TryGetPlaceOriginCell(out var originCell)) return;
        if (IsSameAsLastCell(originCell)) return;

        RememberLastCell(originCell);
        PlaceSelected(originCell);
    }

    private void HandleDragRemove()
    {
        if (!TryGetRemoveCell(out var cell)) return;
        if (IsSameAsLastCell(cell)) return;

        RememberLastCell(cell);
        RemoveAtCell(cell);
    }

    private bool IsSameAsLastCell(Vector3Int cell)
    {
        return hasLastCell && cell == lastCell;
    }

    private void RememberLastCell(Vector3Int cell)
    {
        lastCell = cell;
        hasLastCell = true;
    }

    private void ResetDragState()
    {
        isPlacing = false;
        isRemoving = false;
        hasLastCell = false;
    }

    // =========================================================
    // Input callbacks (Single)
    // =========================================================
    private void OnPlaceSingle(InputAction.CallbackContext _)
    {
        if (!IsSingleClickAllowed()) return;
        if (!TryGetPlaceOriginCell(out var originCell)) return;

        PlaceSelected(originCell);
    }

    private void OnRemoveSingle(InputAction.CallbackContext _)
    {
        if (!IsSingleClickAllowed()) return;
        if (!TryGetRemoveCell(out var cell)) return;

        RemoveAtCell(cell);
    }

    private bool IsSingleClickAllowed()
    {
        if (mode != BuildInputMode.Single) return false;
        if (!isActiveAndEnabled) return false;
        if (IsPointerOverUI()) return false;
        return true;
    }

    // =========================================================
    // Input callbacks (Drag)
    // =========================================================
    private void OnPlaceStarted(InputAction.CallbackContext _)
    {
        if (!IsDragStartAllowed()) return;
        isPlacing = true;
    }

    private void OnPlaceCanceled(InputAction.CallbackContext _)
    {
        isPlacing = false;
        ClearLastCell();
    }

    private void OnRemoveStarted(InputAction.CallbackContext _)
    {
        if (!IsDragStartAllowed()) return;
        isRemoving = true;
    }

    private void OnRemoveCanceled(InputAction.CallbackContext _)
    {
        isRemoving = false;
        ClearLastCell();
    }

    private bool IsDragStartAllowed()
    {
        if (mode != BuildInputMode.Drag) return false;
        // UI判定はUpdate側で行う
        return true;
    }

    // =========================================================
    // Mode toggle
    // =========================================================
    private void OnToggleMode(InputAction.CallbackContext _)
    {
        ToggleMode();
        ResetDragState();
        Debug.Log($"Build Input Mode: {mode}");
    }

    private void ToggleMode()
    {
        mode = (mode == BuildInputMode.Single) ? BuildInputMode.Drag : BuildInputMode.Single;
    }

    // =========================================================
    // Database
    // =========================================================
    private bool TryGetSelectedData(out ObjectData data)
    {
        data = null;
        if (database == null) { LogWarn("database is null"); return false; }
        if (!database.TryGetByID(selectedObjectID, out data)) { LogWarn($"No ObjectData for ID={selectedObjectID}"); return false; }
        if (data.Prefab == null) { LogWarn($"ObjectData(ID={selectedObjectID}) prefab is null"); return false; }
        return true;
    }

    // =========================================================
    // Ray helpers
    // =========================================================
    private Ray MakeMouseRay()
    {
        return cam.ScreenPointToRay(Mouse.current.position.ReadValue());
    }

    private bool RaycastForPlace(out RaycastHit hit)
    {
        Ray ray = MakeMouseRay();
        DebugDrawRay(ray, Color.red);
        return Physics.Raycast(ray, out hit, rayDistance, placeMask, QueryTriggerInteraction.Ignore);
    }

    private bool RaycastForRemove(out RaycastHit hit)
    {
        Ray ray = MakeMouseRay();
        DebugDrawRay(ray, Color.blue);
        return Physics.Raycast(ray, out hit, rayDistance, blockOnlyMask, QueryTriggerInteraction.Ignore);
    }

    private void DebugDrawRay(Ray ray, Color color)
    {
        Debug.DrawRay(ray.origin, ray.direction * rayDistance, color, 0.05f);
    }

    // =========================================================
    // Place origin cell selection
    // - Works for large blocks: choose cell based on hit.point location
    // - "outer cell" = WorldToCell(hit.point + hit.normal * epsilon)
    // =========================================================
    private bool TryGetPlaceOriginCell(out Vector3Int originCell)
    {
        originCell = default;

        if (!CanRaycast()) return false;
        if (!RaycastForPlace(out var hit)) return false;
        if (!TryGetSelectedData(out var data)) return false;

        // ① 面の内側セル
        Vector3 inside = hit.point - hit.normal * 0.01f;
        Vector3Int hitCell = grid.WorldToCell(inside);

        Vector3Int n = NormalToInt(hit.normal);

        // ② サイズ考慮オフセット
        Vector3Int offset = Vector3Int.zero;

        if (n.x > 0) offset.x = 1;
        if (n.x < 0) offset.x = -data.SizeXYZ.x;

        if (n.y > 0) offset.y = 1;
        if (n.y < 0) offset.y = -data.SizeXYZ.y;

        if (n.z > 0) offset.z = 1;
        if (n.z < 0) offset.z = -data.SizeXYZ.z;

        originCell = hitCell + offset;

        // Ground固定
        if (!TryGetBlockInstance(hit, out _))
            originCell.y = groundYCell;

        return true;
    }

    private static Vector3Int NormalToInt(Vector3 n)
    {
        n = n.normalized;

        float ax = Mathf.Abs(n.x);
        float ay = Mathf.Abs(n.y);
        float az = Mathf.Abs(n.z);

        if (ax >= ay && ax >= az)
            return new Vector3Int((int)Mathf.Sign(n.x), 0, 0);

        if (ay >= ax && ay >= az)
            return new Vector3Int(0, (int)Mathf.Sign(n.y), 0);

        return new Vector3Int(0, 0, (int)Mathf.Sign(n.z));
    }


    // =========================================================
    // Remove cell selection
    // - Remove by occupied dict; the cell is derived from hit point
    // =========================================================
    private bool TryGetRemoveCell(out Vector3Int removeCell)
    {
        removeCell = default;

        if (!CanRaycast()) return false;
        if (!RaycastForRemove(out var hit)) { Log("Remove Raycast: hit nothing"); return false; }

        // ブロック表面のどこをクリックしても、その位置のセルを推定
        removeCell = grid.WorldToCell(hit.point);

        Log($"RemoveCell from hit: collider={hit.collider.name} cell={removeCell}");
        return true;
    }

    private bool TryGetBlockInstance(RaycastHit hit, out BlockInstance bi)
    {
        bi = hit.collider.GetComponentInParent<BlockInstance>();
        return bi != null;
    }

    // =========================================================
    // Place / Remove (XYZ occupancy)
    // =========================================================
    private void PlaceSelected(Vector3Int originCell)
    {
        if (!TryGetSelectedData(out var data)) return;

        if (!ValidateBox(originCell, data.SizeXYZ))
        {
            Log($"Place rejected origin={originCell} size={data.SizeXYZ}");
            return;
        }

        PlaceBox(originCell, data);
    }

    private bool ValidateBox(Vector3Int originCell, Vector3Int sizeXYZ)
    {
        if (grid == null) { LogWarn("ValidateBox: grid is null"); return false; }

        foreach (var c in grid.GetCellsInBox(originCell, sizeXYZ))
        {
            if (!grid.IsInside(c))
            {
                LogWarn($"Place rejected: OUTSIDE cell={c} origin={originCell} size={sizeXYZ}");
                return false;
            }

            if (occupied.TryGetValue(c, out var obj) && obj != null)
            {
                var bi = obj.GetComponent<BlockInstance>();
                if (bi != null)
                    LogWarn($"Place rejected: OCCUPIED cell={c} by ID={bi.ObjectID} origin={bi.OriginCell} size={bi.SizeXYZ} name={obj.name}");
                else
                    LogWarn($"Place rejected: OCCUPIED cell={c} by {obj.name}");

                return false;
            }
        }

        return true;
    }

    private void PlaceBox(Vector3Int originCell, ObjectData data)
    {
        Vector3 desiredCenter = grid.BoxToWorldCenter(originCell, data.SizeXYZ);

        GameObject obj = Instantiate(data.Prefab, desiredCenter, Quaternion.identity);
        obj.name = $"{data.Name}_ID{data.ID}_{originCell.x}_{originCell.y}_{originCell.z}";
        ForceBlockLayerOnAllChildren(obj);

        // ✅ Pivotズレ補正：Collider中心を desiredCenter に合わせる
        var col = obj.GetComponentInChildren<Collider>();
        if (col != null)
        {
            Vector3 delta = desiredCenter - col.bounds.center;
            obj.transform.position += delta;
        }
        else
        {
            Debug.LogWarning($"[{obj.name}] No collider found, cannot fix pivot offset.");
        }

        var bi = obj.GetComponent<BlockInstance>();
        if (bi == null) bi = obj.AddComponent<BlockInstance>();
        bi.Setup(data.ID, originCell, data.SizeXYZ);

        foreach (var c in grid.GetCellsInBox(originCell, data.SizeXYZ))
            occupied[c] = obj;
    }


    private void RemoveAtCell(Vector3Int anyCell)
    {
        if (!occupied.TryGetValue(anyCell, out var obj) || obj == null)
        {
            occupied.Remove(anyCell);
            Log($"Removed: nothing at {anyCell}");
            return;
        }

        var bi = obj.GetComponent<BlockInstance>();
        if (bi != null)
        {
            foreach (var c in grid.GetCellsInBox(bi.OriginCell, bi.SizeXYZ))
                occupied.Remove(c);
        }
        else
        {
            // 保険
            occupied.Remove(anyCell);
        }

        Destroy(obj);
        Log($"Removed object at {anyCell}");
    }

    // =========================================================
    // Visual / Layer helpers
    // =========================================================
    private void ForceBlockLayerOnAllChildren(GameObject obj)
    {
        int blockLayer = LayerMask.NameToLayer("Block");
        if (blockLayer < 0) return;

        obj.layer = blockLayer;
        foreach (Transform t in obj.GetComponentsInChildren<Transform>(true))
            t.gameObject.layer = blockLayer;
    }

    // =========================================================
    // Preconditions
    // =========================================================
    private bool CanRaycast()
    {
        if (cam == null) { LogWarn("cam is null"); return false; }
        if (Mouse.current == null) { LogWarn("Mouse.current is null"); return false; }
        return true;
    }

    private static bool IsPointerOverUI()
    {
        return EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
    }

    // =========================================================
    // Public selection helpers
    // =========================================================
    public void SetSelectedObjectID(int id)
    {
        selectedObjectID = id;
    }

    public void NextObject()
    {
        if (database == null || database.objectsData == null || database.objectsData.Count == 0) return;

        int idx = database.objectsData.FindIndex(o => o.ID == selectedObjectID);
        if (idx < 0) idx = 0;

        idx = (idx + 1) % database.objectsData.Count;
        selectedObjectID = database.objectsData[idx].ID;

        Log($"Selected ID: {selectedObjectID}");
    }

    public void PrevObject()
    {
        if (database == null || database.objectsData == null || database.objectsData.Count == 0) return;

        int idx = database.objectsData.FindIndex(o => o.ID == selectedObjectID);
        if (idx < 0) idx = 0;

        idx = (idx - 1 + database.objectsData.Count) % database.objectsData.Count;
        selectedObjectID = database.objectsData[idx].ID;

        Log($"Selected ID: {selectedObjectID}");
    }

    // =========================================================
    // Logging
    // =========================================================
    private void Log(string msg)
    {
        if (debugLogs) Debug.Log(msg);
    }

    private void LogWarn(string msg)
    {
        if (debugLogs) Debug.LogWarning(msg);
    }
}
