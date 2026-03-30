using UnityEngine;

public sealed class BuildMoveService
{
    private readonly BuildContext context;
    private readonly BuildState state;
    private readonly bool debugLogs;

    public BuildMoveService(BuildContext context, BuildState state, bool debugLogs = false)
    {
        this.context = context;
        this.state = state;
        this.debugLogs = debugLogs;
    }

    public bool HandleMove()
    {
        if (!state.HasMoveTarget)
            return TrySelectMoveTarget();

        return TryCommitMove();
    }

    public bool TrySelectMoveTarget()
    {
        if (!CanStartMove())
            return false;

        if (!context.Raycaster.TryGetRemoveTarget(out BlockInstance target))
        {
            Log("[BuildMoveService] TrySelectMoveTarget: remove target not found.");
            return false;
        }

        if (target == null)
        {
            Log("[BuildMoveService] TrySelectMoveTarget: target is null.");
            return false;
        }

        state.BeginMove(target);

        BuildEffectUtility.PlayPickupEffect(target.gameObject);

        if (context.Drone != null)
            context.Drone.BeginCarry(target.transform);

        Log($"[BuildMoveService] Move target selected: {target.name} origin={target.OriginCell}");
        return true;
    }

    public bool TryCommitMove()
    {
        if (!TryGetCurrentMoveTarget(out BlockInstance targetBlock))
        {
            Log("[BuildMoveService] TryCommitMove: move target missing.");
            ResetMoveState(cancelCarry: true);
            return false;
        }

        if (!TryGetMoveDestination(targetBlock, out Vector3Int toCell))
        {
            Log("[BuildMoveService] TryCommitMove: move destination invalid.");
            return false;
        }

        if (!context.Rules.CanPlaceIgnoring(targetBlock, toCell, targetBlock.SizeXYZ, out string reason))
        {
            Log($"[BuildMoveService] TryCommitMove denied: {reason}");
            return false;
        }

        MoveCommand cmd = new MoveCommand(
            context,
            targetBlock,
            toCell,
            "Move Block");

        bool ok = context.History.Do(cmd, debugLogs, playEffects: false);

        if (!ok)
        {
            Log("[BuildMoveService] TryCommitMove: command failed.");
            return false;
        }

        FinishMoveSuccess(targetBlock);

        Log($"[BuildMoveService] Move success: {targetBlock.name} -> {toCell}");
        return true;
    }

    public void CancelMove()
    {
        if (context.Drone != null)
            context.Drone.CancelCarry();

        ResetMoveState(cancelCarry: false);

        Log("[BuildMoveService] Move cancelled.");
    }

    private bool CanStartMove()
    {
        if (context == null)
            return false;

        if (state == null)
            return false;

        if (context.Raycaster == null)
            return false;

        return true;
    }

    private bool TryGetCurrentMoveTarget(out BlockInstance targetBlock)
    {
        targetBlock = state.MoveTarget;

        if (!state.HasMoveTarget)
            return false;

        if (targetBlock != null)
            return true;

        ResetMoveState(cancelCarry: true);
        return false;
    }

    private bool TryGetMoveDestination(BlockInstance targetBlock, out Vector3Int toCell)
    {
        toCell = default;

        if (targetBlock == null)
            return false;

        if (!context.Raycaster.RaycastForBlock(out RaycastHit hit))
            return false;

        return context.Solver.TrySolveOriginCell(hit, targetBlock.SizeXYZ, out toCell);
    }

    private void FinishMoveSuccess(BlockInstance targetBlock)
    {
        if (context.Drone != null && targetBlock != null)
            context.Drone.CommitCarry(targetBlock.transform);

        if (targetBlock != null)
            BuildEffectUtility.PlayDropEffect(targetBlock.gameObject);

        ResetMoveState(cancelCarry: false);
    }

    private void ResetMoveState(bool cancelCarry)
    {
        if (cancelCarry && context.Drone != null)
            context.Drone.CancelCarry();

        state.CancelMove();
        state.PlaceTool = BuildTool.Single;
    }

    private void Log(string message)
    {
        if (debugLogs && !string.IsNullOrEmpty(message))
            Debug.Log(message);
    }
}