using System.Collections.Generic;
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
    private bool pendingLine;
    private List<Vector3Int> pendingLineCells = new List<Vector3Int>();
    private ObjectData pendingLineData;
    private Quaternion pendingLineRotation;
    private bool pendingLineValid;

    private bool pendingMove;
    private BlockInstance pendingMoveTarget;
    private Vector3 pendingMoveWorldPosition;
    private bool pendingMoveValid;

    private bool pendingRemovePreview;
    private GameObject pendingRemoveObject;
    private Vector3 pendingRemoveWorldPosition;
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

        // 1) Locked single-place preview
        if (pendingPlace && pendingPlaceData != null)
        {
            context.Preview.SetSelected(pendingPlaceData);
            context.Preview.SetValid(true);
            context.Preview.ShowSingle(
                pendingPlaceCell,
                pendingPlaceData.SizeXYZ,
                pendingPlaceRotation);
            return;
        }

        // 2) Locked line preview
        if (pendingLine && pendingLineData != null && pendingLineCells != null && pendingLineCells.Count > 0)
        {
            context.Preview.SetSelected(pendingLineData);
            context.Preview.SetValid(pendingLineValid);
            context.Preview.ShowLine(
                pendingLineCells,
                pendingLineData.SizeXYZ,
                pendingLineRotation);
            return;
        }

        // 3) Locked move preview
        if (pendingMove && pendingMoveTarget != null)
        {
            context.Preview.SetValid(pendingMoveValid);
            context.Preview.ShowMovePreview(
                pendingMoveTarget.gameObject,
                pendingMoveWorldPosition);
            return;
        }

        // 4) Locked remove preview
        if (pendingRemovePreview && pendingRemoveObject != null)
        {
            context.Preview.SetValid(false);
            context.Preview.ShowMovePreview(
                pendingRemoveObject,
                pendingRemoveWorldPosition);
            return;
        }

        if (placementService == null)
            return;

        // Normal live mouse-follow preview
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

        // clear all preview-pending modes first
        pendingPlace = false;
        pendingLine = false;
        pendingMove = false;
        pendingRemove = false;
        pendingRemovePreview = false;
        pendingUndo = false;
        pendingRedo = false;
        operationPending = true;

        // Single placement
        if (state.PlaceTool == BuildTool.Single)
        {
            pendingPlace = true;
            pendingPlaceCell = originCell;
            pendingPlaceData = data;
            pendingPlaceRotation = rotation;
        }
        // Line placement
        else if (state.PlaceTool == BuildTool.Line)
        {
            Vector3Int rotatedSize = context.Solver.GetRotatedSize(data.SizeXYZ, rotation);

            if (!state.HasLineStart ||
                !context.Solver.TryGetLineCellsOrthogonal(
                    state.LineStartCell,
                    originCell,
                    rotatedSize,
                    out List<Vector3Int> lineCells) ||
                lineCells == null ||
                lineCells.Count == 0)
            {
                ClearPending();
                return false;
            }

            // line preview lock
            pendingLine = true;
            pendingLineCells.Clear();
            pendingLineCells.AddRange(lineCells);
            pendingLineData = data;
            pendingLineRotation = rotation;
            pendingLineValid = true;

            // IMPORTANT: still use place commit pipeline
            pendingPlace = true;
            pendingPlaceCell = originCell;
            pendingPlaceData = data;
            pendingPlaceRotation = rotation;
        }

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
        pendingLine = false;
        pendingMove = false;
        pendingRemove = true;
        pendingRemovePreview = true;
        pendingUndo = false;
        pendingRedo = false;
        operationPending = true;

        pendingRemoveTarget = targetBlock;
        pendingRemoveObject = targetBlock.gameObject;
        pendingRemoveWorldPosition = targetBlock.transform.position;

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

        // First click: select move target
        if (!state.HasMoveTarget)
        {
            bool selected = moveService.HandleMove();
            RefreshPreview();
            return selected;
        }

        // Second click: commit move with locked preview
        if (!moveService.TryGetMovePreviewSnapshot(
            out BlockInstance targetBlock,
            out Vector3Int toCell,
            out Vector3 worldPos,
            out bool canPlace))
        {
            RefreshPreview();
            return false;
        }

        if (!canPlace || targetBlock == null)
        {
            RefreshPreview();
            return false;
        }

        pendingMove = true;
        pendingMoveTarget = targetBlock;
        pendingMoveWorldPosition = worldPos;
        pendingMoveValid = true;
        operationPending = true;

        // execute real move now
        bool ok = moveService.TryCommitMove();

        if (!ok)
        {
            ClearPending();
            RefreshPreview();
            return false;
        }

        RefreshPreview();
        return true;
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

        pendingLine = false;
        pendingLineCells.Clear();
        pendingLineData = null;
        pendingLineRotation = Quaternion.identity;
        pendingLineValid = true;

        pendingMove = false;
        pendingMoveTarget = null;
        pendingMoveWorldPosition = default;
        pendingMoveValid = true;

        pendingRemove = false;
        pendingRemoveTarget = null;

        pendingRemovePreview = false;
        pendingRemoveObject = null;
        pendingRemoveWorldPosition = default;

        pendingUndo = false;
        pendingRedo = false;
    }
}