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
    [SerializeField] private InputActionReference toggleToolAction;   // e.g. Tab (Single <-> Line)
    [Header("Undo/Redo")]
    [SerializeField] private InputActionReference undoAction; // Ctrl+Z
    [SerializeField] private InputActionReference redoAction; // Ctrl+Y
    [Header("Database (SO)")]
    [SerializeField] private ObjectsDatabaseSO database;
    [SerializeField] private int initialSelectedObjectID = 0;

    [Header("Ground rule")]
    [SerializeField] private int groundYCell = 0;

    [Header("Preview")]
    [SerializeField] private Material previewMaterial;

    [Header("Debug")]
    [SerializeField] private bool debugLogs = true;

    // Parts (Transform / State / Output)
    private BuildRaycaster raycaster;        // Transform
    private BuildPlacementSolver solver;    // Transform   
    private BuildSpawner spawner;           // Output (instantiate)
    private BuildState state;               // State (tool/selection/line start)
    private BuildPlacementRules rules;      // Judgment (can place? can use line?)
    private CommandHistory history;
    private BuildPreview preview;           // Output (visual)
    private void Awake()
    {
        if (cam == null) cam = Camera.main;

        // Parts
        raycaster = new BuildRaycaster(cam, rayDistance, placeMask, blockOnlyMask);
        solver = new BuildPlacementSolver(grid, groundYCell);
        state = new BuildState(initialSelectedObjectID, initialTool);
        rules = new BuildPlacementRules(grid);
        spawner = new BuildSpawner();
        history = new CommandHistory();
        preview = new BuildPreview(grid, previewMaterial);

        if (previewMaterial == null && debugLogs)
            Debug.LogWarning("BuildController: previewMaterial is not assigned.");
        if (grid == null && debugLogs)
            Debug.LogWarning("BuildController: grid is not assigned.");
        if (database == null && debugLogs)
            Debug.LogWarning("BuildController: database is not assigned.");
    }
    private void OnUndo(InputAction.CallbackContext _) => history.Undo(debugLogs);
    private void OnRedo(InputAction.CallbackContext _) => history.Redo(debugLogs);
    private void OnEnable()
    {
        placeAction?.action.Enable();
        removeAction?.action.Enable();
        toggleToolAction?.action.Enable();
        undoAction?.action.Enable();
        redoAction?.action.Enable();
        if (placeAction != null) placeAction.action.performed += OnPlacePerformed;
        if (removeAction != null) removeAction.action.performed += OnRemovePerformed;
        if (toggleToolAction != null) toggleToolAction.action.performed += OnToggleToolPerformed;
        if (undoAction != null) undoAction.action.performed += OnUndo;
        if (redoAction != null) redoAction.action.performed += OnRedo;
    }

    private void OnDisable()
    {
        if (placeAction != null) placeAction.action.performed -= OnPlacePerformed;
        if (removeAction != null) removeAction.action.performed -= OnRemovePerformed;
        if (toggleToolAction != null) toggleToolAction.action.performed -= OnToggleToolPerformed;
        if (undoAction != null) undoAction.action.performed -= OnUndo;
        if (redoAction != null) redoAction.action.performed -= OnRedo;
        placeAction?.action.Disable();
        removeAction?.action.Disable();
        toggleToolAction?.action.Disable();
        undoAction?.action.Disable();
        redoAction?.action.Disable();

        preview?.Clear();
        state?.CancelLine();
    }

    private void Update()
    {
        if (!isActiveAndEnabled) return;
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
        {
            preview?.Clear();
            return;
        }

        UpdatePreview();
    }

    // -------------------------
    // Tool toggle
    // -------------------------
    private void OnToggleToolPerformed(InputAction.CallbackContext _)
    {
        state.ToggleTool();
        preview?.Clear();
        if (debugLogs) Debug.Log($"Place Tool: {state.PlaceTool}");
    }

    public void UI_SetToolSingle()
    {
        state.SetTool(PlaceToolMode.Single);
        preview?.Clear();
        if (debugLogs) Debug.Log("Place Tool: Single");
    }

    public void UI_SetToolLine()
    {
        state.SetTool(PlaceToolMode.Line);
        preview?.Clear();
        if (debugLogs) Debug.Log("Place Tool: Line");
    }

    // -------------------------
    // Place / Remove input
    // -------------------------
    private void OnPlacePerformed(InputAction.CallbackContext _)
    {
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return;

        if (!TryGetSelectedData(out var data)) return;
        if (!raycaster.RaycastForPlace(out var hit)) return;
        if (!solver.TrySolveOriginCell(hit, data.SizeXYZ, out var cell)) return;

        // Single
        if (state.PlaceTool == PlaceToolMode.Single)
        {
            PlaceSelected(cell, data);
            return;
        }
        if (!rules.CanUseLineTool(data, out var reason))
        {
            if (debugLogs && !string.IsNullOrEmpty(reason)) Debug.Log(reason);
            return;
        }

        // 1st click: remember start
        if (!state.HasLineStart)
        {
            state.BeginLine(cell);
            if (debugLogs) Debug.Log($"Line start: {state.LineStartCell}");
            return;
        }

        // 2nd click => commit line
        if (!solver.TryGetLineCellsOrthogonal(state.LineStartCell, cell, data.SizeXYZ, out var lineCells))
        {
            state.CancelLine();
            preview?.Clear();
            return;
        }
        var group = new CompositeCommand($"Line Place {data.Name}");
        Quaternion rot = Quaternion.identity;
        foreach (var c in lineCells)
            group.Add(new PlaceCommand(grid, spawner, rules, c, data, rot));
        bool ok = history.Do(group, debugLogs);
        if (!ok && debugLogs) Debug.Log("Line place failed (rolled back).");

        state.CancelLine();
        preview?.Clear();
    }

    private void OnRemovePerformed(InputAction.CallbackContext _)
    {
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return;
        if (!raycaster.RaycastForRemove(out var hit)) return;

        // fallback: hit object directly
        var root = hit.collider.GetComponentInParent<BlockInstance>();
        if (root != null)
        {
            // root のどこかのセルを anyCell として渡せば RemoveCommand が同じブロックを引ける
            var anyCell = root.OriginCell;
            RemoveAtCell(anyCell);
            return;
        }

        if (solver.TrySolveRemoveCell(hit, out var cell))
            RemoveAtCell(cell);
    }

    // -------------------------
    // Place / Remove core
    // -------------------------
    private void PlaceSelected(Vector3Int originCell, ObjectData data)
    {
        if (!rules.CanPlace(originCell, data.SizeXYZ, out var rejectReason))return;
        Quaternion rot = Quaternion.identity;
        var cmd = new PlaceCommand(grid, spawner, rules, originCell, data, rot);
        bool ok = history.Do(cmd, debugLogs);
      
    }

    private void RemoveAtCell(Vector3Int anyCell)
    {
        bool has = rules.TryGetObjectAtCell(anyCell, out var obj);
        var cmd = new RemoveCommand(grid, spawner, rules, database, anyCell);
        bool ok = history.Do(cmd, debugLogs);
    }

    private bool TryGetSelectedData(out ObjectData data)
    {
        data = null;
        if (database == null) return false;

        int id = (state != null) ? state.SelectedObjectID : initialSelectedObjectID;
        if (!database.TryGetByID(id, out data)) return false;

        return data != null && data.Prefab != null;
    }

    // -------------------------
    // UI Button Callbacks
    // -------------------------
    public void UI_NextItem() => StepSelection(+1);
    public void UI_PrevItem() => StepSelection(-1);

    private void StepSelection(int delta)
    {
        if (database == null || state == null) return;

        int count = database.objectsData.Count;
        state.StepSelection(delta, count);

        if (debugLogs) Debug.Log($"Selected Object ID: {state.SelectedObjectID}");

        preview?.Clear();
    }

    public void SetSelectedObjectID(int id)
    {
        state?.SetSelectedObjectID(id);
        preview?.Clear();
    }

    // -------------------------
    // Preview
    // -------------------------
    private void UpdatePreview()
    {
        if (!TryGetSelectedData(out var data))
        {
            preview?.Clear();
            return;
        }

        if (!raycaster.RaycastForPlace(out var hit))
        {
            preview?.Clear();
            return;
        }

        if (!solver.TrySolveOriginCell(hit, data.SizeXYZ, out var hoverCell))
        {
            preview?.Clear();
            return;
        }

        // 選択データに応じて preview prefab を準備（BuildPreview側）
        preview.SetSelected(data);

        // ---- Single：いままで通り単体表示 ----
        if (state.PlaceTool == PlaceToolMode.Single)
        {
            preview.ShowSingle(hoverCell, data.SizeXYZ);

            bool canPlace = rules.CanPlace(hoverCell, data.SizeXYZ, out _);
            preview.SetValid(canPlace);
            return;
        }

        // ---- Line：まずLineToolを使えるか ----
        if (!rules.CanUseLineTool(data, out _))
        {
            // 使えない場合でも “単体候補” は見せる（好みで Clear でもOK）
            preview.ShowSingle(hoverCell, data.SizeXYZ);
            preview.SetValid(false);
            return;
        }

        // 1回目クリック前：開始点候補として単体表示
        if (!state.HasLineStart)
        {
            preview.ShowSingle(hoverCell, data.SizeXYZ);

            bool canPlace = rules.CanPlace(hoverCell, data.SizeXYZ, out _);
            preview.SetValid(canPlace);
            return;
        }

        // 1回目クリック後：開始点→hoverまでを複数表示
        bool ok;
        List<Vector3Int> lineCells;
            ok = solver.TryGetLineCellsOrthogonal(state.LineStartCell, hoverCell, data.SizeXYZ, out lineCells);

        if (!ok || lineCells == null || lineCells.Count == 0)
        {
            preview.ClearActiveOnly();
            return;
        }

        preview.ShowLine(lineCells, data.SizeXYZ);

        // 1個でも置けなければ NG 色
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
}