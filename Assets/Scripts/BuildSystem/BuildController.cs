using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public class BuildController : MonoBehaviour
{
    public enum PlaceToolMode { Single, Line }

    [Header("Tool (initial)")]
    [SerializeField] private PlaceToolMode initialTool = PlaceToolMode.Single;

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
    [SerializeField] private InputActionReference moveAction;         // e.g. MMB or key
    [SerializeField] private InputActionReference toggleToolAction;   // e.g. Tab (Single <-> Line)

    [Header("Undo/Redo")]
    [SerializeField] private InputActionReference undoAction; // Ctrl+Z
    [SerializeField] private InputActionReference redoAction; // Ctrl+Y

    [Header("Database (SO)")]
    [SerializeField] private ObjectsDatabaseSO database;
    [SerializeField] private int initialSelectedObjectID = 0;

    [Header("Placement Root")]
    [SerializeField] private Transform placedRoot;

    [Header("Ground rule")]
    [SerializeField] private int groundYCell = 0;

    [Header("Preview")]
    [SerializeField] private Material previewMaterial;

    [Header("Debug")]
    [SerializeField] private bool debugLogs = true;
    public BuildPlacementRules Rules => rules;
    public BuildSpawner Spawner => spawner;
    // Parts
    private BuildRaycaster raycaster;
    private BuildPlacementSolver solver;
    private BuildSpawner spawner;
    private BuildState state;
    private BuildPlacementRules rules;
    private CommandHistory history;
    private BuildPreview preview;

    // -------------------------------------------------------
    // Unity lifecycle
    // -------------------------------------------------------
    private void Awake()
    {
        if (cam == null) cam = Camera.main;

        raycaster = new BuildRaycaster(cam, rayDistance, placeMask, blockOnlyMask);
        solver = new BuildPlacementSolver(grid, groundYCell);
        state = new BuildState(initialSelectedObjectID, initialTool);
        rules = new BuildPlacementRules(grid);
        spawner = new BuildSpawner(placedRoot);
        history = new CommandHistory();
        preview = new BuildPreview(grid, previewMaterial);

        WarnIfMissing();
    }

    private void OnEnable()
    {
        EnableActions(true);
        SubscribeActions(true);
    }

    private void OnDisable()
    {
        SubscribeActions(false);
        EnableActions(false);

        preview?.Clear();
        state?.CancelLine();
        state?.CancelMove(); // ★ Move中も安全に解除（BuildStateにCancelMoveがある前提）
    }

    private void Update()
    {
        if (!isActiveAndEnabled) return;

        if (IsPointerOverUI())
        {
            preview?.Clear();
            return;
        }

        UpdatePreview();
    }

    // -------------------------------------------------------
    // Setup helpers
    // -------------------------------------------------------
    private void WarnIfMissing()
    {
        if (!debugLogs) return;

        if (previewMaterial == null) Debug.LogWarning("BuildController: previewMaterial is not assigned.");
        if (grid == null) Debug.LogWarning("BuildController: grid is not assigned.");
        if (database == null) Debug.LogWarning("BuildController: database is not assigned.");
    }

    private void EnableActions(bool enable)
    {
        SetActionEnabled(placeAction, enable);
        SetActionEnabled(removeAction, enable);
        SetActionEnabled(moveAction, enable);
        SetActionEnabled(toggleToolAction, enable);
        SetActionEnabled(undoAction, enable);
        SetActionEnabled(redoAction, enable);
    }

    private void SubscribeActions(bool subscribe)
    {
        HookPerformed(placeAction, subscribe, OnPlacePerformed);
        HookPerformed(removeAction, subscribe, OnRemovePerformed);
        HookPerformed(moveAction, subscribe, OnMovePerformed);
        HookPerformed(toggleToolAction, subscribe, OnToggleToolPerformed);
        HookPerformed(undoAction, subscribe, OnUndoPerformed);
        HookPerformed(redoAction, subscribe, OnRedoPerformed);
    }

    private static void SetActionEnabled(InputActionReference a, bool enable)
    {
        if (a == null || a.action == null) return;
        if (enable) a.action.Enable();
        else a.action.Disable();
    }

    private static void HookPerformed(InputActionReference a, bool subscribe, System.Action<InputAction.CallbackContext> cb)
    {
        if (a == null || a.action == null || cb == null) return;
        if (subscribe) a.action.performed += cb;
        else a.action.performed -= cb;
    }

    private static bool IsPointerOverUI()
    {
        return EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
    }

    // -------------------------------------------------------
    // Input callbacks
    // -------------------------------------------------------
    private void OnUndoPerformed(InputAction.CallbackContext _) => history.Undo(debugLogs);
    private void OnRedoPerformed(InputAction.CallbackContext _) => history.Redo(debugLogs);

    private void OnToggleToolPerformed(InputAction.CallbackContext _)
    {
        state.ToggleTool();
        preview?.Clear();
        Log($"Place Tool: {state.PlaceTool}");
    }

    private void OnPlacePerformed(InputAction.CallbackContext _)
    {
        if (IsPointerOverUI()) return;

        if (!TryGetSelectedData(out var data)) return;
        RaycastHit hit;
        Vector3Int cell;
        if (!TryGetHoverCell(data.SizeXYZ, out cell, out hit)) return;

        if (state.PlaceTool == PlaceToolMode.Single)
        {
            HandleSinglePlace(cell, data);
            return;
        }

        HandleLinePlace(cell, data);
    }

    private void OnRemovePerformed(InputAction.CallbackContext _)
    {
        if (IsPointerOverUI()) return;

        if (!raycaster.RaycastForRemove(out var hit)) return;

        var root = hit.collider.GetComponentInParent<BlockInstance>();
        if (root == null) return;

        RemoveAtCell(root.OriginCell);
    }

    private void OnMovePerformed(InputAction.CallbackContext _)
    {
        if (IsPointerOverUI()) return;

        // 1st click: select target
        if (!state.HasMoveTarget)
        {
            TrySelectMoveTarget();
            return;
        }

        // 2nd click: choose destination and execute
        TryCommitMove();
    }

    // -------------------------------------------------------
    // Place handlers
    // -------------------------------------------------------
    private void HandleSinglePlace(Vector3Int originCell, ObjectData data)
    {
        PlaceSelected(originCell, data);
        preview?.Clear();
    }

    private void HandleLinePlace(Vector3Int cell, ObjectData data)
    {
        if (!rules.CanUseLineTool(data, out var reason))
        {
            Log(reason);
            return;
        }

        // 1st click
        if (!state.HasLineStart)
        {
            state.BeginLine(cell);
            Log($"Line start: {state.LineStartCell}");
            return;
        }

        // 2nd click: compute line cells
        if (!solver.TryGetLineCellsOrthogonal(state.LineStartCell, cell, data.SizeXYZ, out var lineCells))
        {
            state.CancelLine();
            preview?.Clear();
            return;
        }

        // commit
        var group = new CompositeCommand($"Line Place {data.Name}");
        Quaternion rot = Quaternion.identity;

        foreach (var c in lineCells)
            group.Add(new PlaceCommand(grid, spawner, rules, c, data, rot));

        bool ok = history.Do(group, debugLogs);
        if (!ok) Log("Line place failed (rolled back).");

        state.CancelLine();
        preview?.Clear();
    }

    // -------------------------------------------------------
    // Move handlers
    // -------------------------------------------------------
    private void TrySelectMoveTarget()
    {
        if (!raycaster.RaycastForRemove(out var hit)) return;

        var target = hit.collider.GetComponentInParent<BlockInstance>();
        if (target == null) return;

        state.BeginMove(target);
        preview?.Clear();
        Log($"Move target selected: {target.name} origin={target.OriginCell}");
    }

    private void TryCommitMove()
    {
        if (!raycaster.RaycastForPlace(out var hit2)) return;

        var targetBI = state.MoveTarget;
        if (targetBI == null)
        {
            state.CancelMove();
            return;
        }

        if (!solver.TrySolveOriginCell(hit2, targetBI.SizeXYZ, out var toCell)) return;

        var cmd = new MoveCommand(grid, spawner, rules, targetBI, toCell, "Move Block");
        bool ok = history.Do(cmd, debugLogs);
        if (!ok) Log("Move failed.");

        state.CancelMove();
        preview?.Clear();
    }

    // -------------------------------------------------------
    // Place / Remove core
    // -------------------------------------------------------
    private void PlaceSelected(Vector3Int originCell, ObjectData data)
    {
        if (!rules.CanPlace(originCell, data.SizeXYZ, out _)) return;

        Quaternion rot = Quaternion.identity;
        var cmd = new PlaceCommand(grid, spawner, rules, originCell, data, rot);
        history.Do(cmd, debugLogs);
    }

    private void RemoveAtCell(Vector3Int anyCell)
    {
        // TryGetObjectAtCell の戻り値は、必要ならUI表示に使える
        var cmd = new RemoveCommand(grid, spawner, rules, database, anyCell);
        history.Do(cmd, debugLogs);
    }

    private bool TryGetSelectedData(out ObjectData data)
    {
        data = null;
        if (database == null) return false;

        int id = (state != null) ? state.SelectedObjectID : initialSelectedObjectID;
        if (!database.TryGetByID(id, out data)) return false;

        return data != null && data.Prefab != null;
    }

    private bool TryGetHoverCell(Vector3Int placeSize, out Vector3Int hoverCell, out RaycastHit hit)
    {
        hoverCell = default;
        hit = default;

        if (!raycaster.RaycastForPlace(out hit)) return false;
        if (!solver.TrySolveOriginCell(hit, placeSize, out hoverCell)) return false;
        return true;
    }

    // -------------------------------------------------------
    // UI Button Callbacks
    // -------------------------------------------------------
    public void UI_SetToolSingle()
    {
        state.SetTool(PlaceToolMode.Single);
        preview?.Clear();
        Log("Place Tool: Single");
    }

    public void UI_SetToolLine()
    {
        state.SetTool(PlaceToolMode.Line);
        preview?.Clear();
        Log("Place Tool: Line");
    }

    public void UI_NextItem() => StepSelection(+1);
    public void UI_PrevItem() => StepSelection(-1);

    public void SetSelectedObjectID(int id)
    {
        state?.SetSelectedObjectID(id);
        preview?.Clear();
    }

    private void StepSelection(int delta)
    {
        if (database == null || state == null) return;

        int count = database.objectsData.Count;
        state.StepSelection(delta, count);

        Log($"Selected Object ID: {state.SelectedObjectID}");
        preview?.Clear();
    }

    // -------------------------------------------------------
    // Preview
    // -------------------------------------------------------
    private void UpdatePreview()
    {
        if (!TryGetSelectedData(out var data))
        {
            preview?.Clear();
            return;
        }

        if (!TryGetHoverCell(data.SizeXYZ, out var hoverCell, out _))
        {
            preview?.Clear();
            return;
        }

        preview.SetSelected(data);

        if (state.PlaceTool == PlaceToolMode.Single)
        {
            ShowSinglePreview(hoverCell, data.SizeXYZ);
            return;
        }

        ShowLinePreview(hoverCell, data);
    }

    private void ShowSinglePreview(Vector3Int cell, Vector3Int size)
    {
        preview.ShowSingle(cell, size);
        bool canPlace = rules.CanPlace(cell, size, out _);
        preview.SetValid(canPlace);
    }

    private void ShowLinePreview(Vector3Int hoverCell, ObjectData data)
    {
        // Line tool not allowed
        if (!rules.CanUseLineTool(data, out _))
        {
            preview.ShowSingle(hoverCell, data.SizeXYZ);
            preview.SetValid(false);
            return;
        }

        // before start click: single candidate
        if (!state.HasLineStart)
        {
            ShowSinglePreview(hoverCell, data.SizeXYZ);
            return;
        }

        // after start click: show line
        if (!solver.TryGetLineCellsOrthogonal(state.LineStartCell, hoverCell, data.SizeXYZ, out var lineCells) ||
            lineCells == null || lineCells.Count == 0)
        {
            preview.ClearActiveOnly();
            return;
        }

        preview.ShowLine(lineCells, data.SizeXYZ);

        bool allPlaceable = true;
        foreach (var c in lineCells)
        {
            if (!rules.CanPlace(c, data.SizeXYZ, out _))
            {
                allPlaceable = false;
                break;
            }
        }
        preview.SetValid(allPlaceable);
    }

    // -------------------------------------------------------
    private void Log(string msg)
    {
        if (debugLogs && !string.IsNullOrEmpty(msg))
            Debug.Log(msg);
    }
}