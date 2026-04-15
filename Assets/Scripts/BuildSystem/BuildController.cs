using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
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

    [Header("Color Input")]
    [SerializeField] private InputActionReference color1Action;
    [SerializeField] private InputActionReference color2Action;
    [SerializeField] private InputActionReference color3Action;
    [SerializeField] private InputActionReference color4Action;

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
    public bool IsBuildEnabled { get; set; } = true;

    public event Action<int, BlockColor> OnSelectionChanged;

    private void Awake()
    {
        ValidateReferences();

        history = new CommandHistory();

        raycaster = new BuildRaycaster(buildCamera, rayDistance, placeMask, blockOnlyMask);
        solver = new BuildPlacementSolver(grid, 0);
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

        SelectedColor = state.SelectedColor;
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

        BindInput(color1Action, OnColor1Performed);
        BindInput(color2Action, OnColor2Performed);
        BindInput(color3Action, OnColor3Performed);
        BindInput(color4Action, OnColor4Performed);
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

        UnbindInput(color1Action, OnColor1Performed);
        UnbindInput(color2Action, OnColor2Performed);
        UnbindInput(color3Action, OnColor3Performed);
        UnbindInput(color4Action, OnColor4Performed);
    }

    private void Update()
    {
        if (app == null)
            return;

        if (!IsBuildEnabled)
            return;

        if (IsPointerOverUI())
            return;

        app.TickPreview();
    }

    public void SetSelectedObject(int objectId)
    {
        state.SelectedObjectID = objectId;
        app?.RefreshPreview();
        NotifySelectionChanged();
    }

    public void SetSelectedColor(BlockColor color)
    {
        SelectedColor = color;
        state.SetSelectedColor(color);
        app?.RefreshPreview();
        NotifySelectionChanged();

        if (debugLogs)
            Debug.Log($"[BuildController] Selected Color = {color}");
    }

    public void SetTool(BuildTool tool)
    {
        if (state.PlaceTool == tool)
            return;

        // leave old tool cleanly
        if (state.PlaceTool == BuildTool.Line)
            state.CancelLine();

        if (state.PlaceTool == BuildTool.Move || tool != BuildTool.Move)
            app?.CancelMove();

        state.PlaceTool = tool;

        // enter new tool cleanly
        if (tool != BuildTool.Line)
            state.CancelLine();

        if (debugLogs)
            Debug.Log($"[BuildController] Tool changed to {state.PlaceTool}");

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
        if (!IsBuildEnabled)
            return;

        app?.Undo();
        app?.RefreshPreview();
    }

    public void Redo()
    {
        if (!IsBuildEnabled)
            return;

        app?.Redo();
        app?.RefreshPreview();
    }

    public void CancelCurrentOperation()
    {
        if (app != null)
            app.CancelMove();

        if (state != null)
            state.CancelLine();

        if (context != null)
        {
            context.Preview?.Clear();
            context.Drone?.SetIdle();
        }

        if (debugLogs)
            Debug.Log("[BuildController] CancelCurrentOperation called");
    }

    // LMB only
    private void OnPlacePerformed(InputAction.CallbackContext _)
    {
        if (app == null || !IsBuildEnabled || IsPointerOverUI())
            return;

        if (droneCompanion != null && droneCompanion.IsBusy)
            return;

        if (state.PlaceTool == BuildTool.Move)
            app.Move();
        else
            app.Place();

        app.RefreshPreview();
    }

    private void OnRemovePerformed(InputAction.CallbackContext _)
    {
        if (app == null)
            return;

        if (!IsBuildEnabled)
            return;

        if (IsPointerOverUI())
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

        if (!IsBuildEnabled)
            return;

        if (IsPointerOverUI())
            return;

        SetTool(BuildTool.Move);
    }

    private void OnUndoPerformed(InputAction.CallbackContext _)
    {
        if (!IsBuildEnabled)
            return;

        Undo();
    }

    private void OnRedoPerformed(InputAction.CallbackContext _)
    {
        if (!IsBuildEnabled)
            return;

        Redo();
    }

    private void OnRotateCWPerformed(InputAction.CallbackContext _)
    {
        if (!IsBuildEnabled)
            return;

        RotateCW();
    }

    private void OnRotateCCWPerformed(InputAction.CallbackContext _)
    {
        if (!IsBuildEnabled)
            return;

        RotateCCW();
    }

    private void OnToggleToolPerformed(InputAction.CallbackContext _)
    {
        if (!IsBuildEnabled)
            return;

        ToggleTool();
        app?.RefreshPreview();
    }

    private void OnColor1Performed(InputAction.CallbackContext _)
    {
        SetSelectedColor(BlockColor.Blue);
    }

    private void OnColor2Performed(InputAction.CallbackContext _)
    {
        SetSelectedColor(BlockColor.Red);
    }

    private void OnColor3Performed(InputAction.CallbackContext _)
    {
        SetSelectedColor(BlockColor.Yellow);
    }

    private void OnColor4Performed(InputAction.CallbackContext _)
    {
        SetSelectedColor(BlockColor.Green);
    }

    private void ToggleTool()
    {
        switch (state.PlaceTool)
        {
            case BuildTool.Single:
                SetTool(BuildTool.Line);
                break;

            case BuildTool.Line:
                SetTool(BuildTool.Move);
                break;

            case BuildTool.Move:
                SetTool(BuildTool.Single);
                break;

            default:
                SetTool(BuildTool.Single);
                break;
        }
    }

    private void NotifySelectionChanged()
    {
        OnSelectionChanged?.Invoke(state.SelectedObjectID, state.SelectedColor);
    }

    private void ValidateReferences()
    {
        if (grid == null) Debug.LogError("[BuildController] GridManager is missing.", this);
        if (database == null) Debug.LogError("[BuildController] ObjectsDatabaseSO is missing.", this);
        if (buildCamera == null) Debug.LogError("[BuildController] Build Camera is missing.", this);
        if (placedBlocksRoot == null) Debug.LogError("[BuildController] placedBlocksRoot is missing.", this);
        if (previewMaterial == null) Debug.LogError("[BuildController] Preview Material is missing.", this);
    }

    private static void BindInput(InputActionReference actionRef, Action<InputAction.CallbackContext> callback)
    {
        if (actionRef == null || actionRef.action == null)
            return;

        actionRef.action.performed += callback;
        actionRef.action.Enable();
    }

    private static void UnbindInput(InputActionReference actionRef, Action<InputAction.CallbackContext> callback)
    {
        if (actionRef == null || actionRef.action == null)
            return;

        actionRef.action.performed -= callback;
        actionRef.action.Disable();
    }

    private bool IsPointerOverUI()
    {
        if (EventSystem.current == null)
            return false;

        return EventSystem.current.IsPointerOverGameObject();
    }
}