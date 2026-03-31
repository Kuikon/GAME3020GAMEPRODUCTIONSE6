using UnityEngine;
using UnityEngine.InputSystem;

public sealed class BuildController : MonoBehaviour
{
    [Header("Scene References")]
    [SerializeField] private GridManager grid;
    [SerializeField] private ObjectsDatabaseSO database;
    [SerializeField] private DroneCompanionController droneCompanion;
    [SerializeField] private Transform placedBlocksRoot;
    [SerializeField] private Material previewMaterial;

    [Header("Raycast")]
    [SerializeField] private LayerMask placeMask;
    [SerializeField] private LayerMask blockOnlyMask;
    [SerializeField] private float rayDistance = 200f;
    [SerializeField] private Camera buildCamera;
    [Header("State")]
    [SerializeField] private BuildState state = new BuildState();

    [Header("Input")]
    [SerializeField] private InputActionReference placeAction;
    [SerializeField] private InputActionReference removeAction;
    [SerializeField] private InputActionReference moveAction;
    [SerializeField] private InputActionReference undoAction;
    [SerializeField] private InputActionReference redoAction;
    [SerializeField] private InputActionReference rotateCWAction;
    [SerializeField] private InputActionReference rotateCCWAction;
    [SerializeField] private InputActionReference toggleToolAction;

    [Header("Debug")]
    [SerializeField] private bool debugLogs = false;

    private BuildRaycaster raycaster;
    private BuildPlacementSolver solver;
    private BuildSpawner spawner;
    private BuildPlacementRules rules;
    private BuildPreview preview;

    private CommandHistory history;
    private BuildContext context;
    private BuildApplicationService app;

    public BuildState State => state;
    public BuildContext Context => context;
    public BuildSpawner Spawner => spawner;
    public BuildPlacementRules Rules => rules;
    public BlockColor SelectedColor { get; private set; } = BlockColor.Blue;
    private void Awake()
    {
        ValidateReferences();

        history = new CommandHistory();

        raycaster = new BuildRaycaster(buildCamera, rayDistance, placeMask, blockOnlyMask);
        solver = new BuildPlacementSolver(grid,0);
        spawner = new BuildSpawner(placedBlocksRoot);
        rules = new BuildPlacementRules(grid);
        preview = new BuildPreview(grid, previewMaterial);

        DroneService droneService = new DroneService(droneCompanion);

        context = new BuildContext(
            grid,
            raycaster,
            solver,
            spawner,
            rules,
            history,
            database,
            preview,
            droneService);

        app = new BuildApplicationService(context, state, debugLogs);
    }

    private void OnEnable()
    {
        BindInput(placeAction, OnPlacePerformed);
        BindInput(removeAction, OnRemovePerformed);
        BindInput(moveAction, OnMovePerformed);
        BindInput(undoAction, OnUndoPerformed);
        BindInput(redoAction, OnRedoPerformed);
        BindInput(rotateCWAction, OnRotateCWPerformed);
        BindInput(rotateCCWAction, OnRotateCCWPerformed);
        BindInput(toggleToolAction, OnToggleToolPerformed);
    }

    private void OnDisable()
    {
        UnbindInput(placeAction, OnPlacePerformed);
        UnbindInput(removeAction, OnRemovePerformed);
        UnbindInput(moveAction, OnMovePerformed);
        UnbindInput(undoAction, OnUndoPerformed);
        UnbindInput(redoAction, OnRedoPerformed);
        UnbindInput(rotateCWAction, OnRotateCWPerformed);
        UnbindInput(rotateCCWAction, OnRotateCCWPerformed);
        UnbindInput(toggleToolAction, OnToggleToolPerformed);
    }

    private void Update()
    {
        if (app == null)
            return;

        app.TickPreview();
    }

    public void SetSelectedObject(int objectId)
    {
        state.SelectedObjectID = objectId;
        app?.RefreshPreview();
    }
    public void SetSelectedColor(BlockColor color)
    {
        SelectedColor = color;
        app?.RefreshPreview();
    }
    public void SetTool(BuildTool tool)
    {
        if (tool != BuildTool.Move)
            app?.CancelMove();

        state.PlaceTool = tool;

        if (tool != BuildTool.Line)
            state.CancelLine();

        app?.RefreshPreview();
    }

    public void RotateCW()
    {
        state.RotateCW();
        app?.RefreshPreview();
    }

    public void RotateCCW()
    {
        state.RotateCCW();
        app?.RefreshPreview();
    }

    public void Undo()
    {
        app?.Undo();
        app?.RefreshPreview();
    }

    public void Redo()
    {
        app?.Redo();
        app?.RefreshPreview();
    }

    public void CancelCurrentOperation()
    {
        app?.CancelMove();
        state.CancelLine();
        context?.Preview?.Clear();
        context?.Drone?.SetIdle();
    }

    private void OnPlacePerformed(InputAction.CallbackContext _)
    {
        if (app == null)
            return;
        if (droneCompanion && droneCompanion != null && droneCompanion.IsBusy)
        {
            if (debugLogs)
                Debug.Log("[Build] Drone is busy. Placement blocked.");
            return;
        }
        app.Place();
        app.RefreshPreview();
    }

    private void OnRemovePerformed(InputAction.CallbackContext _)
    {
        if (app == null)
            return;
        if (droneCompanion != null && droneCompanion.IsBusy)
        {
            if (debugLogs)
                Debug.Log("[Build] Drone is busy. Remove blocked.");
            return;
        }
        app.Remove();
        app.RefreshPreview();
    }

    private void OnMovePerformed(InputAction.CallbackContext _)
    {
        if (app == null)
            return;

        state.PlaceTool = BuildTool.Move;
        app.Move();
        app.RefreshPreview();
    }

    private void OnUndoPerformed(InputAction.CallbackContext _)
    {
        Undo();
    }

    private void OnRedoPerformed(InputAction.CallbackContext _)
    {
        Redo();
    }

    private void OnRotateCWPerformed(InputAction.CallbackContext _)
    {
        RotateCW();
    }

    private void OnRotateCCWPerformed(InputAction.CallbackContext _)
    {
        RotateCCW();
    }

    private void OnToggleToolPerformed(InputAction.CallbackContext _)
    {
        ToggleTool();
        app?.RefreshPreview();
    }

    private void ToggleTool()
    {
        if (state.PlaceTool == BuildTool.Single)
        {
            state.PlaceTool = BuildTool.Line;
            state.CancelLine();
            app?.CancelMove();
            return;
        }

        if (state.PlaceTool == BuildTool.Line)
        {
            state.PlaceTool = BuildTool.Single;
            state.CancelLine();
            app?.CancelMove();
            return;
        }

        state.PlaceTool = BuildTool.Single;
        app?.CancelMove();
    }

    private void ValidateReferences()
    {
        if (grid == null) Debug.LogError("[BuildController] GridManager is missing.", this);
        if (database == null) Debug.LogError("[BuildController] ObjectsDatabaseSO is missing.", this);
        if (buildCamera == null) Debug.LogError("[BuildController] Build Camera is missing.", this);
        if (placedBlocksRoot == null) Debug.LogError("[BuildController] placedBlocksRoot is missing.", this);
        if (previewMaterial == null) Debug.LogError("[BuildController] Preview Material is missing.", this);
    }

    private static void BindInput(InputActionReference actionRef, System.Action<InputAction.CallbackContext> callback)
    {
        if (actionRef == null || actionRef.action == null)
            return;

        actionRef.action.performed += callback;
        actionRef.action.Enable();
    }

    private static void UnbindInput(InputActionReference actionRef, System.Action<InputAction.CallbackContext> callback)
    {
        if (actionRef == null || actionRef.action == null)
            return;

        actionRef.action.performed -= callback;
        actionRef.action.Disable();
    }
}