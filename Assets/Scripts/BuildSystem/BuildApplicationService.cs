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

    private Vector3Int pendingPlaceCell;
    private ObjectData pendingPlaceData;
    private Quaternion pendingPlaceRotation;
    private bool pendingPlace;
    private bool pendingRemove;
    private BlockInstance pendingRemoveTarget;

    private bool pendingUndo;
    private bool pendingRedo;

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
            return false;

        if (context.Drone != null && (context.Drone.IsBusy || context.Drone.IsCarrying))
            return false;

        if (!placementService.TryCreatePlacementRequest(
            out var originCell,
            out var data,
            out var rotation,
            debugLogs))
        {
            return false;
        }

        if (data == null)
            return false;

        pendingPlace = true;
        pendingRemove = false;
        pendingUndo = false;
        pendingRedo = false;
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
            return false;

        if (context.Drone != null && (context.Drone.IsBusy || context.Drone.IsCarrying))
            return false;

        if (!removeService.TryCreateRemoveRequest(out var targetBlock, debugLogs))
            return false;

        if (targetBlock == null)
            return false;

        pendingPlace = false;
        pendingRemove = true;
        pendingUndo = false;
        pendingRedo = false;
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
            return false;

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
            return;

        if (context.Drone != null && (context.Drone.IsBusy || context.Drone.IsCarrying))
            return;

        IBuildCommand cmd = context.History.PeekUndo();
        if (cmd == null)
            return;

        if (TryStartAnimatedUndo(cmd))
            return;

        context.History.Undo(debugLogs, playEffects: true);
        RefreshPreview();
    }

    public void Redo()
    {
        if (operationPending)
            return;

        if (context.Drone != null && (context.Drone.IsBusy || context.Drone.IsCarrying))
            return;

        IBuildCommand cmd = context.History.PeekRedo();
        if (cmd == null)
            return;

        if (TryStartAnimatedRedo(cmd))
            return;

        context.History.Redo(debugLogs, playEffects: true);
        RefreshPreview();
    }

    private bool TryStartAnimatedUndo(IBuildCommand cmd)
    {
        if (context.Drone == null)
            return false;

        if (cmd is MoveCommand moveCmd)
        {
            BlockInstance block = moveCmd.TargetBlock;
            if (block == null)
                return false;

            operationPending = true;
            pendingUndo = true;
            pendingRedo = false;
            pendingPlace = false;
            pendingRemove = false;

            context.Drone.BeginCarry(block.transform);
            context.Drone.CommitCarry(block.transform);
            return true;
        }

        return false;
    }

    private bool TryStartAnimatedRedo(IBuildCommand cmd)
    {
        if (context.Drone == null)
            return false;

        if (cmd is MoveCommand moveCmd)
        {
            BlockInstance block = moveCmd.TargetBlock;
            if (block == null)
                return false;

            operationPending = true;
            pendingRedo = true;
            pendingUndo = false;
            pendingPlace = false;
            pendingRemove = false;

            context.Drone.BeginCarry(block.transform);
            context.Drone.CommitCarry(block.transform);
            return true;
        }

        return false;
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

        if (pendingUndo)
        {
            CommitPendingUndo();
            return;
        }

        if (pendingRedo)
        {
            CommitPendingRedo();
            return;
        }

        ClearPending();
    }

    private void CommitPendingPlace()
    {
        placementService.TryPlaceReserved(
            pendingPlaceCell,
            pendingPlaceData,
            pendingPlaceRotation,
            debugLogs);

        ClearPending();
        RefreshPreview();
    }

    private void CommitPendingRemove()
    {
        removeService.TryRemoveReserved(
            pendingRemoveTarget,
            debugLogs);

        ClearPending();
        RefreshPreview();
    }

    private void CommitPendingUndo()
    {
        context.History.Undo(debugLogs, playEffects: true);
        ClearPending();
        RefreshPreview();
    }

    private void CommitPendingRedo()
    {
        context.History.Redo(debugLogs, playEffects: true);
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

        pendingUndo = false;
        pendingRedo = false;
    }
}