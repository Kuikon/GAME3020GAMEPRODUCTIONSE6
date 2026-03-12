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
    [SerializeField] private InputActionReference toggleToolAction;   // e.g. Tab
    [SerializeField] private InputActionReference rotateCWAction;     // e.g. E
    [SerializeField] private InputActionReference rotateCCWAction;    // e.g. Q

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

    public int SelectedObjectID => state != null ? state.SelectedObjectID : initialSelectedObjectID;
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
        state?.CancelMove();
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
        if (cam == null) Debug.LogWarning("BuildController: cam is not assigned.");
    }

    private void EnableActions(bool enable)
    {
        SetActionEnabled(placeAction, enable);
        SetActionEnabled(removeAction, enable);
        SetActionEnabled(moveAction, enable);
        SetActionEnabled(toggleToolAction, enable);
        SetActionEnabled(rotateCWAction, enable);
        SetActionEnabled(rotateCCWAction, enable);
        SetActionEnabled(undoAction, enable);
        SetActionEnabled(redoAction, enable);
    }

    private void SubscribeActions(bool subscribe)
    {
        HookPerformed(placeAction, subscribe, OnPlacePerformed);
        HookPerformed(removeAction, subscribe, OnRemovePerformed);
        HookPerformed(moveAction, subscribe, OnMovePerformed);
        HookPerformed(toggleToolAction, subscribe, OnToggleToolPerformed);
        HookPerformed(rotateCWAction, subscribe, OnRotateCWPerformed);
        HookPerformed(rotateCCWAction, subscribe, OnRotateCCWPerformed);
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

    private void OnRotateCWPerformed(InputAction.CallbackContext _)
    {
        if (IsPointerOverUI()) return;

        state.RotateCW();
        preview?.Clear();
        Log($"Rotate CW: {state.CurrentRotation.eulerAngles}");
    }

    private void OnRotateCCWPerformed(InputAction.CallbackContext _)
    {
        if (IsPointerOverUI()) return;

        state.RotateCCW();
        preview?.Clear();
        Log($"Rotate CCW: {state.CurrentRotation.eulerAngles}");
    }

    private void OnPlacePerformed(InputAction.CallbackContext _)
    {
        if (IsPointerOverUI()) return;

        if (!TryGetSelectedData(out var data)) return;

        Quaternion rot = GetCurrentRotation();
        Vector3Int rotatedSize = GetRotatedSize(data.SizeXYZ, rot);

        RaycastHit hit;
        Vector3Int cell;
        if (!TryGetHoverCell(rotatedSize, out cell, out hit)) return;

        if (state.PlaceTool == PlaceToolMode.Single)
        {
            HandleSinglePlace(cell, data, rot, rotatedSize);
            return;
        }

        HandleLinePlace(cell, data, rot, rotatedSize);
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

        if (!state.HasMoveTarget)
        {
            TrySelectMoveTarget();
            return;
        }

        TryCommitMove();
    }

    // -------------------------------------------------------
    // Place handlers
    // -------------------------------------------------------
    private void HandleSinglePlace(Vector3Int originCell, ObjectData data, Quaternion rot, Vector3Int rotatedSize)
    {
        PlaceSelected(originCell, data, rot, rotatedSize);
        preview?.Clear();
    }

    private void HandleLinePlace(Vector3Int cell, ObjectData data, Quaternion rot, Vector3Int rotatedSize)
    {
        if (!rules.CanUseLineTool(data, out var reason))
        {
            Log(reason);
            return;
        }

        if (!state.HasLineStart)
        {
            state.BeginLine(cell);
            Log($"Line start: {state.LineStartCell}");
            return;
        }

        if (!solver.TryGetLineCellsOrthogonal(state.LineStartCell, cell, rotatedSize, out var lineCells))
        {
            state.CancelLine();
            preview?.Clear();
            return;
        }

        var group = new CompositeCommand($"Line Place {data.Name}");

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
    private void PlaceSelected(Vector3Int originCell, ObjectData data, Quaternion rot, Vector3Int rotatedSize)
    {
        if (!rules.CanPlace(originCell, rotatedSize, out _)) return;

        var cmd = new PlaceCommand(grid, spawner, rules, originCell, data, rot);
        history.Do(cmd, debugLogs);
    }

    private void RemoveAtCell(Vector3Int anyCell)
    {
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

    public void UI_RotateCW()
    {
        state?.RotateCW();
        preview?.Clear();
        Log("Rotate CW");
    }

    public void UI_RotateCCW()
    {
        state?.RotateCCW();
        preview?.Clear();
        Log("Rotate CCW");
    }

    public void UI_NextItem() => StepSelection(+1);
    public void UI_PrevItem() => StepSelection(-1);

    private void StepSelection(int delta)
    {
        if (database == null || state == null) return;

        int count = database.objectsData.Count;
        state.StepSelection(delta, count);

        Log($"Selected Object ID: {state.SelectedObjectID}");
        preview?.Clear();
    }

    public void SetSelectedObject(int objectID)
    {
        state?.SetSelectedObjectID(objectID);
        preview?.Clear();
        Log($"[BuildController] SelectedObjectID = {objectID}");
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

        Quaternion rot = GetCurrentRotation();
        Vector3Int rotatedSize = GetRotatedSize(data.SizeXYZ, rot);

        if (!TryGetHoverCell(rotatedSize, out var hoverCell, out _))
        {
            preview?.Clear();
            return;
        }

        preview.SetSelected(data);

        if (state.PlaceTool == PlaceToolMode.Single)
        {
            ShowSinglePreview(hoverCell, rotatedSize, rot);
            return;
        }

        ShowLinePreview(hoverCell, data, rotatedSize, rot);
    }

    private void ShowSinglePreview(Vector3Int cell, Vector3Int size, Quaternion rot)
    {
        preview.ShowSingle(cell, size, rot);

        bool canPlace = rules.CanPlace(cell, size, out _);
        preview.SetValid(canPlace);
    }

    private void ShowLinePreview(Vector3Int hoverCell, ObjectData data, Vector3Int rotatedSize, Quaternion rot)
    {
        if (!rules.CanUseLineTool(data, out _))
        {
            preview.ShowSingle(hoverCell, rotatedSize, rot);
            preview.SetValid(false);
            return;
        }

        if (!state.HasLineStart)
        {
            ShowSinglePreview(hoverCell, rotatedSize, rot);
            return;
        }

        if (!solver.TryGetLineCellsOrthogonal(state.LineStartCell, hoverCell, rotatedSize, out var lineCells) ||
            lineCells == null || lineCells.Count == 0)
        {
            preview.ClearActiveOnly();
            return;
        }

        preview.ShowLine(lineCells, rotatedSize, rot);

        bool allPlaceable = true;
        foreach (var c in lineCells)
        {
            if (!rules.CanPlace(c, rotatedSize, out _))
            {
                allPlaceable = false;
                break;
            }
        }

        preview.SetValid(allPlaceable);
    }

    // -------------------------------------------------------
    // Rotation helpers
    // -------------------------------------------------------
    private Quaternion GetCurrentRotation()
    {
        return state != null ? state.CurrentRotation : Quaternion.identity;
    }

    private Vector3Int GetRotatedSize(Vector3Int originalSize, Quaternion rot)
    {
        float y = Mathf.Round(rot.eulerAngles.y) % 360f;

        if (Mathf.Approximately(y, 90f) || Mathf.Approximately(y, 270f))
            return new Vector3Int(originalSize.z, originalSize.y, originalSize.x);

        return originalSize;
    }

    // -------------------------------------------------------
    private void Log(string msg)
    {
        if (debugLogs && !string.IsNullOrEmpty(msg))
            Debug.Log(msg);
    }
}