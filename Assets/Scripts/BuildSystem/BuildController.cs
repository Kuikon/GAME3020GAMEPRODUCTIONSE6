// BuildController.cs (Single + Line only, Drag removed)
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

    [Header("Database (SO)")]
    [SerializeField] private ObjectsDatabaseSO database;
    [SerializeField] private int initialSelectedObjectID = 0;

    [Header("Ground rule")]
    [SerializeField] private int groundYCell = 0;

    [Header("Preview")]
    [SerializeField] private Material previewMaterial;

    [Header("Line Tool")]
    [SerializeField] private bool diagonalAllowed = true;
    [SerializeField] private bool lineTool1x1Only = true;

    [Header("Debug")]
    [SerializeField] private bool debugLogs = true;

    // Parts
    private BuildRaycaster raycaster;
    private BuildOccupancy occupancy;
    private BuildSpawner spawner;
    private BuildPlacementSolver solver;
    private BuildPreview preview;

    // NEW: State
    private BuildState state;

    private void Awake()
    {
        if (cam == null) cam = Camera.main;

        // Init Parts
        raycaster = new BuildRaycaster(cam, rayDistance, placeMask, blockOnlyMask);
        occupancy = new BuildOccupancy();
        spawner = new BuildSpawner();
        solver = new BuildPlacementSolver(grid, groundYCell);
        preview = new BuildPreview(grid, previewMaterial);

        // Init State
        state = new BuildState(initialSelectedObjectID, initialTool);

        if (previewMaterial == null && debugLogs)
            Debug.LogWarning("BuildController: previewMaterial is not assigned.");
    }

    private void OnEnable()
    {
        placeAction?.action.Enable();
        removeAction?.action.Enable();
        toggleToolAction?.action.Enable();

        if (placeAction != null) placeAction.action.performed += OnPlacePerformed;
        if (removeAction != null) removeAction.action.performed += OnRemovePerformed;
        if (toggleToolAction != null) toggleToolAction.action.performed += OnToggleToolPerformed;
    }

    private void OnDisable()
    {
        if (placeAction != null) placeAction.action.performed -= OnPlacePerformed;
        if (removeAction != null) removeAction.action.performed -= OnRemovePerformed;
        if (toggleToolAction != null) toggleToolAction.action.performed -= OnToggleToolPerformed;

        placeAction?.action.Disable();
        removeAction?.action.Disable();
        toggleToolAction?.action.Disable();

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

        if (state.PlaceTool == PlaceToolMode.Single)
        {
            PlaceSelected(cell, data);
            return;
        }

        if (!state.HasLineStart)
        {
            state.BeginLine(cell);
            if (debugLogs) Debug.Log($"Line start: {state.LineStartCell}");
            return;
        }

        // 2nd click => commit line
        if (diagonalAllowed)
        {
            if (!solver.TryGetLineCellsDiagonal(state.LineStartCell, cell, out var lineCells))
            {
                state.CancelLine();
                preview?.Clear();
                return;
            }

            foreach (var c in lineCells)
                PlaceSelected(c, data);
        }
        else
        {
            if (!solver.TryGetLineCellsOrthogonal(state.LineStartCell, cell, data.SizeXYZ, out var lineCells))
            {
                state.CancelLine();
                preview?.Clear();
                return;
            }

            foreach (var c in lineCells)
                PlaceSelected(c, data);
        }

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
            occupancy.RemoveObjectCells(grid, root.OriginCell, root.SizeXYZ);
            Destroy(root.gameObject);
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
        if (!occupancy.ValidateBox(grid, originCell, data.SizeXYZ, out var rejectReason))
        {
            if (debugLogs) Debug.Log($"Place rejected origin={originCell} size={data.SizeXYZ} reason={rejectReason}");
            return;
        }

        var obj = spawner.Spawn(grid, originCell, data);
        occupancy.RegisterObjectCells(grid, originCell, data.SizeXYZ, obj);
    }

    private void RemoveAtCell(Vector3Int anyCell)
    {
        if (!occupancy.TryGetObjectAtCell(anyCell, out var obj))
            return;

        if (obj == null)
        {
            occupancy.ClearCell(anyCell);
            return;
        }

        var bi = obj.GetComponent<BlockInstance>();
        if (bi != null) occupancy.RemoveObjectCells(grid, bi.OriginCell, bi.SizeXYZ);
        else occupancy.ClearCell(anyCell);

        Destroy(obj);
    }

    private bool TryGetSelectedData(out ObjectData data)
    {
        data = null;
        if (database == null) return false;

        int id = state != null ? state.SelectedObjectID : initialSelectedObjectID;
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

        if (!solver.TrySolveOriginCell(hit, data.SizeXYZ, out var originCell))
        {
            preview?.Clear();
            return;
        }

        preview.SetSelected(data);
        preview.UpdatePose(originCell, data.SizeXYZ);

        bool canPlace = occupancy.ValidateBox(grid, originCell, data.SizeXYZ, out _);
        preview.SetValid(canPlace);
    }
}