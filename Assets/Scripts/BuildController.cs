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
    [SerializeField] private InputActionReference placeAction;
    [SerializeField] private InputActionReference removeAction;
    [SerializeField] private InputActionReference toggleModeAction;

    [Header("Database (SO)")]
    [SerializeField] private ObjectsDatabaseSO database;
    [SerializeField] private int selectedObjectID = 0;

    [Header("Ground rule")]
    [SerializeField] private int groundYCell = 0;

    [Header("Debug")]
    [SerializeField] private bool debugLogs = true;

    // Parts
    private BuildRaycaster raycaster;
    private BuildOccupancy occupancy;
    private BuildSpawner spawner;
    private BuildPlacementSolver solver;

    // Drag state
    private bool isPlacing;
    private bool isRemoving;
    private bool hasLastCell;
    private Vector3Int lastCell;

    private void Awake()
    {
        if (cam == null) cam = Camera.main;
        raycaster = new BuildRaycaster(cam, rayDistance, placeMask, blockOnlyMask);
        occupancy = new BuildOccupancy();
        spawner = new BuildSpawner();
        solver = new BuildPlacementSolver(grid, groundYCell);

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
            placeAction.action.started += _ => { if (mode == BuildInputMode.Drag) isPlacing = true; };
            placeAction.action.canceled += _ => { isPlacing = false; hasLastCell = false; };
        }

        if (removeAction != null)
        {
            removeAction.action.started += _ => { if (mode == BuildInputMode.Drag) isRemoving = true; };
            removeAction.action.canceled += _ => { isRemoving = false; hasLastCell = false; };
        }

        if (toggleModeAction != null) toggleModeAction.action.performed += _ => ToggleMode();
    }

    private void OnDisable()
    {
        if (placeAction != null) placeAction.action.performed -= OnPlaceSingle;
        if (removeAction != null) removeAction.action.performed -= OnRemoveSingle;

        placeAction?.action.Disable();
        removeAction?.action.Disable();
        toggleModeAction?.action.Disable();
    }

    private void Update()
    {
        if (!isActiveAndEnabled) return;
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) { ResetDrag(); return; }
        if (mode != BuildInputMode.Drag) return;

        if (!isPlacing && !isRemoving) { hasLastCell = false; return; }

        if (isPlacing) DragPlace();
        if (isRemoving) DragRemove();
    }

    private void DragPlace()
    {
        if (!TryGetSelectedData(out var data)) return;
        if (!raycaster.RaycastForPlace(out var hit)) return;

        if (!solver.TrySolveOriginCell(hit, data.SizeXYZ, out var originCell)) return;
        if (hasLastCell && originCell == lastCell) return;

        lastCell = originCell; hasLastCell = true;
        PlaceSelected(originCell, data);
    }

    private void DragRemove()
    {
        if (!raycaster.RaycastForRemove(out var hit)) return;

        if (!solver.TrySolveRemoveCell(hit, out var cell)) return;
        if (hasLastCell && cell == lastCell) return;

        lastCell = cell; hasLastCell = true;
        RemoveAtCell(cell);
    }

    private void OnPlaceSingle(InputAction.CallbackContext _)
    {
        if (mode != BuildInputMode.Single) return;
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return;

        if (!TryGetSelectedData(out var data)) return;
        if (!raycaster.RaycastForPlace(out var hit)) return;
        if (!solver.TrySolveOriginCell(hit, data.SizeXYZ, out var originCell)) return;

        PlaceSelected(originCell, data);
    }

    private void OnRemoveSingle(InputAction.CallbackContext _)
    {
        if (mode != BuildInputMode.Single) return;
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return;

        if (!raycaster.RaycastForRemove(out var hit)) return;
        if (!solver.TrySolveRemoveCell(hit, out var cell)) return;

        RemoveAtCell(cell);
    }

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

    private void ToggleMode()
    {
        mode = (mode == BuildInputMode.Single) ? BuildInputMode.Drag : BuildInputMode.Single;
        ResetDrag();
        if (debugLogs) Debug.Log($"Build Input Mode: {mode}");
    }

    private void ResetDrag()
    {
        isPlacing = false;
        isRemoving = false;
        hasLastCell = false;
    }

    private bool TryGetSelectedData(out ObjectData data)
    {
        data = null;
        if (database == null) return false;
        if (!database.TryGetByID(selectedObjectID, out data)) return false;
        return data != null && data.Prefab != null;
    }

    // Optional selection API
    public void SetSelectedObjectID(int id) => selectedObjectID = id;
}