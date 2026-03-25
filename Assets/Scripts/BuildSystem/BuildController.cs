using System.Collections;
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

    [Header("Drone Companion")]
    [SerializeField] private bool useDroneCompanion = true;
    [SerializeField] private DroneCompanionController droneCompanion;
    [SerializeField] private float removeDroneDelay = 0.25f;

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
    private BuildPlacementService placementService;
    private BuildMoveService moveService;
    private BuildRemoveService removeService;
    private BuildPreviewService previewService;

    private Coroutine removeRoutine;

    private void Awake()
    {
        if (cam == null)
            cam = Camera.main;

        raycaster = new BuildRaycaster(cam, rayDistance, placeMask, blockOnlyMask);
        solver = new BuildPlacementSolver(grid, groundYCell);
        state = new BuildState(initialSelectedObjectID, initialTool);
        rules = new BuildPlacementRules(grid);
        spawner = new BuildSpawner(placedRoot);
        history = new CommandHistory();
        preview = new BuildPreview(grid, previewMaterial);

        placementService = new BuildPlacementService(
            grid,
            raycaster,
            solver,
            spawner,
            rules,
            database,
            history,
            useDroneCompanion ? droneCompanion : null
        );

        moveService = new BuildMoveService(
            grid,
            raycaster,
            solver,
            spawner,
            rules,
            history
        );

        removeService = new BuildRemoveService(
            grid,
            spawner,
            rules,
            database,
            history,
            useDroneCompanion ? droneCompanion : null
        );

        previewService = new BuildPreviewService(
            raycaster,
            solver,
            rules,
            preview
        );

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

        if (removeRoutine != null)
        {
            StopCoroutine(removeRoutine);
            removeRoutine = null;
        }

        preview?.Clear();
        state?.CancelLine();
        state?.CancelMove();
    }

    private void Update()
    {
        if (!isActiveAndEnabled)
            return;

        if (IsDroneBusy())
        {
            preview?.Clear();
            return;
        }

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
        if (!debugLogs)
            return;

        if (previewMaterial == null)
            Debug.LogWarning("BuildController: previewMaterial is not assigned.");

        if (grid == null)
            Debug.LogWarning("BuildController: grid is not assigned.");

        if (database == null)
            Debug.LogWarning("BuildController: database is not assigned.");

        if (cam == null)
            Debug.LogWarning("BuildController: cam is not assigned.");

        if (useDroneCompanion && droneCompanion == null)
            Debug.LogWarning("BuildController: droneCompanion is not assigned.");
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
        if (a == null || a.action == null)
            return;

        if (enable) a.action.Enable();
        else a.action.Disable();
    }

    private static void HookPerformed(
        InputActionReference a,
        bool subscribe,
        System.Action<InputAction.CallbackContext> cb)
    {
        if (a == null || a.action == null || cb == null)
            return;

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
    private void OnUndoPerformed(InputAction.CallbackContext _)
    {
        history.Undo(debugLogs);
    }

    private void OnRedoPerformed(InputAction.CallbackContext _)
    {
        history.Redo(debugLogs);
    }

    private void OnToggleToolPerformed(InputAction.CallbackContext _)
    {
        state.ToggleTool();
        preview?.Clear();
    }

    private void OnRotateCWPerformed(InputAction.CallbackContext _)
    {
        if (IsPointerOverUI())
            return;

        state.RotateCW();
        preview?.Clear();
    }

    private void OnRotateCCWPerformed(InputAction.CallbackContext _)
    {
        if (IsPointerOverUI())
            return;

        state.RotateCCW();
        preview?.Clear();
    }

    private void OnPlacePerformed(InputAction.CallbackContext _)
    {
        if (IsPointerOverUI())
            return;

        if (!TryGetSelectedData(out var data))
            return;

        GameObject spawnedObject;
        bool ok = placementService.HandlePlaceInput(state, data, debugLogs, out spawnedObject);

        if (ok)
        {
            previewService.Clear();
            NotifyDroneBuild(spawnedObject);
        }
        else
        {
            // Line開始だけのときは false になることあり
            NotifyDroneIdle();
        }
    }

    private void OnRemovePerformed(InputAction.CallbackContext _)
    {
        if (IsPointerOverUI())
            return;

        if (!raycaster.TryGetRemoveTarget(out var targetTransform))
            return;

        if (targetTransform == null)
            return;

        BlockInstance block = targetTransform.GetComponent<BlockInstance>();
        if (block == null)
            block = targetTransform.GetComponentInParent<BlockInstance>();

        if (block == null)
        {
            if (debugLogs)
                Debug.LogWarning("[Remove] BlockInstance not found on remove target.");
            return;
        }

        if (removeRoutine != null)
        {
            StopCoroutine(removeRoutine);
            removeRoutine = null;
        }

        removeRoutine = StartCoroutine(CoRemoveWithDrone(block));
    }

    private IEnumerator CoRemoveWithDrone(BlockInstance targetBlock)
    {
        if (targetBlock == null)
            yield break;

        Transform targetTransform = targetBlock.transform;

        if (useDroneCompanion && droneCompanion != null && targetTransform != null)
        {
            droneCompanion.PlayRemove(targetTransform);

            // ❗ここで削除しないと永遠に終わらない
            yield return new WaitForSeconds(0.1f); // 少しだけ動く時間
        }

        // ★ここで削除
        removeService.TryRemoveBlock(targetBlock, debugLogs);

        // ★Remove完了まで待つ
        if (useDroneCompanion && droneCompanion != null)
        {
            yield return new WaitWhile(() => droneCompanion.IsRemoving);
        }
    }

    private void OnMovePerformed(InputAction.CallbackContext _)
    {
        if (IsPointerOverUI())
            return;

        bool ok = moveService.HandleMoveInput(state, debugLogs);

        if (ok && !state.HasMoveTarget)
            previewService.Clear();
    }

    // -------------------------------------------------------
    // Selected object
    // -------------------------------------------------------
    private bool TryGetSelectedData(out ObjectData data)
    {
        data = null;

        if (database == null)
            return false;

        int id = (state != null) ? state.SelectedObjectID : initialSelectedObjectID;

        if (!database.TryGetByID(id, out data))
            return false;

        return data != null && data.Prefab != null;
    }

    public void SetSelectedObject(int objectID)
    {
        state?.SetSelectedObjectID(objectID);
        preview?.Clear();
    }

    // -------------------------------------------------------
    // Drone
    // -------------------------------------------------------
    private void NotifyDroneBuild(GameObject spawnedObject)
    {
        if (!useDroneCompanion || droneCompanion == null)
            return;

        if (spawnedObject == null)
        {
            droneCompanion.SetIdle();
            return;
        }

        droneCompanion.PlayBuild(spawnedObject);
    }

    private void NotifyDroneIdle()
    {
        if (!useDroneCompanion || droneCompanion == null)
            return;

        if (droneCompanion.IsBusy)
            return;

        droneCompanion.SetIdle();
    }

    private bool IsDroneBusy()
    {
        return useDroneCompanion &&
               droneCompanion != null &&
               droneCompanion.IsBusy;
    }

    // -------------------------------------------------------
    // Preview
    // -------------------------------------------------------
    private void UpdatePreview()
    {
        // Move previewを使うならここを有効化
        // if (state != null && state.HasMoveTarget && state.MoveTarget != null)
        // {
        //     UpdateMovePreview();
        //     return;
        // }

        if (!TryGetSelectedData(out var data))
        {
            preview?.Clear();
            if (!IsDroneBusy())
                NotifyDroneIdle();
            return;
        }

        previewService.UpdatePreview(state, data);
    }

    private void UpdateMovePreview()
    {
        var moveTarget = state.MoveTarget;
        if (moveTarget == null)
        {
            preview?.Clear();
            if (!IsDroneBusy())
                NotifyDroneIdle();
            return;
        }

        if (!raycaster.TryGetPlaceHit(out var hit))
        {
            preview?.Clear();
            return;
        }

        if (!solver.TrySolveOriginCell(hit, moveTarget.SizeXYZ, out var previewCell))
        {
            preview?.Clear();
            return;
        }

        string reason;
        bool canPlace = rules.CanPlaceIgnoring(moveTarget, previewCell, moveTarget.SizeXYZ, out reason);

        Vector3 worldPos = grid.BoxToWorldCenter(previewCell, moveTarget.SizeXYZ);

        preview.ShowMovePreview(moveTarget.gameObject, worldPos);
        preview.SetValid(canPlace);

        if (debugLogs && !canPlace && !string.IsNullOrEmpty(reason))
            Debug.Log($"[MovePreview] invalid: {reason}");
    }

    // -------------------------------------------------------
    private void Log(string msg)
    {
        if (debugLogs && !string.IsNullOrEmpty(msg))
            Debug.Log(msg);
    }
}