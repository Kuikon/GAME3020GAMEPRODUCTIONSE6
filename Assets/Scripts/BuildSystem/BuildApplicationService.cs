using UnityEngine;

public sealed class BuildApplicationService
{
    private readonly BuildContext context;
    private readonly BuildState state;
    private readonly bool debugLogs;

    private readonly BuildPlacementService placementService;
    private readonly BuildRemoveService removeService;
    private readonly BuildMoveService moveService;

    private bool operationPending;

    private bool pendingPlace;
    private Vector3Int pendingPlaceCell;
    private ObjectData pendingPlaceData;
    private Quaternion pendingPlaceRotation;

    private bool pendingRemove;
    private BlockInstance pendingRemoveTarget;

    public BuildApplicationService(BuildContext context, BuildState state, bool debugLogs = false)
    {
        this.context = context;
        this.state = state;
        this.debugLogs = debugLogs;

        placementService = new BuildPlacementService(context, state);
        removeService = new BuildRemoveService(context, state);
        moveService = new BuildMoveService(context, state);

        if (context.Drone != null)
            context.Drone.SequenceFinished += OnDroneSequenceFinished;
    }

    public void TickPreview()
    {
        if (context == null)
            return;

        if (context.Preview == null)
            return;

        if (placementService == null)
            return;

        placementService.UpdatePreview();
    }

    public void RefreshPreview()
    {
        TickPreview();
    }

    public bool Place()
    {
        return RequestPlace();
    }

    public bool Remove()
    {
        return RequestRemove();
    }

    public bool RequestPlace()
    {
        if (operationPending)
        {
            return false;
        }

        if (context.Drone != null && context.Drone.IsBusy)
        {
            return false;
        }

        if (!placementService.TryCreatePlacementRequest(
            out var originCell,
            out var data,
            out var rotation,
            debugLogs))
        {
            return false;
        }

        if (data == null)
        {
            return false;
        }

        pendingPlace = true;
        pendingRemove = false;
        operationPending = true;

        pendingPlaceCell = originCell;
        pendingPlaceData = data;
        pendingPlaceRotation = rotation;

        if (context.Drone != null)
        {
            Vector3 previewWorldPos = context.Grid.BoxToWorldCenter(originCell, data.SizeXYZ);
            context.Drone.PlayBuildAt(previewWorldPos);
        }
        else
        {
            CommitPendingPlace();
        }

        return true;
    }

    public bool RequestRemove()
    {
        if (operationPending)
        {
            return false;
        }

        if (context.Drone != null && context.Drone.IsBusy)
        {
            return false;
        }

        if (!removeService.TryCreateRemoveRequest(out var targetBlock, debugLogs))
        {
            return false;
        }

        if (targetBlock == null)
        {
            return false;
        }

        pendingPlace = false;
        pendingRemove = true;
        operationPending = true;

        pendingRemoveTarget = targetBlock;

        if (context.Drone != null)
        {
            context.Drone.PlayRemove(targetBlock.transform);
        }
        else
        {
            CommitPendingRemove();
        }

        return true;
    }

    public bool Move()
    {
        if (operationPending)
        {
            return false;
        }

        bool ok = moveService.HandleMove();
        RefreshPreview();
        return ok;
    }

    public void CancelMove()
    {
        moveService.CancelMove();
        RefreshPreview();
    }

    public void Undo()
    {
        if (operationPending)
        {
            return;
        }

        context.History.Undo(debugLogs);
        RefreshPreview();
    }

    public void Redo()
    {
        if (operationPending)
        {
            return;
        }

        context.History.Redo(debugLogs);
        RefreshPreview();
    }

    private void OnDroneSequenceFinished()
    {
        if (!operationPending)
            return;

        if (pendingPlace)
        {
            CommitPendingPlace();
            return;
        }

        if (pendingRemove)
        {
            CommitPendingRemove();
            return;
        }

        ClearPending();
    }

    private void CommitPendingPlace()
    {
        bool success = placementService.TryPlaceReserved(
            pendingPlaceCell,
            pendingPlaceData,
            pendingPlaceRotation,
            debugLogs);


        ClearPending();
        RefreshPreview();
    }

    private void CommitPendingRemove()
    {
        bool success = removeService.TryRemoveReserved(
            pendingRemoveTarget,
            debugLogs);

    

        ClearPending();
        RefreshPreview();
    }

    private void ClearPending()
    {
        operationPending = false;

        pendingPlace = false;
        pendingPlaceCell = default;
        pendingPlaceData = null;
        pendingPlaceRotation = Quaternion.identity;

        pendingRemove = false;
        pendingRemoveTarget = null;
    }
}