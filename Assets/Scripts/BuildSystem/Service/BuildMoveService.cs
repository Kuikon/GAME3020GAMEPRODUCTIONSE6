using UnityEngine;

public class BuildMoveService
{
    private readonly GridManager grid;
    private readonly BuildRaycaster raycaster;
    private readonly BuildPlacementSolver solver;
    private readonly BuildSpawner spawner;
    private readonly BuildPlacementRules rules;
    private readonly CommandHistory history;

    public BuildMoveService(
        GridManager grid,
        BuildRaycaster raycaster,
        BuildPlacementSolver solver,
        BuildSpawner spawner,
        BuildPlacementRules rules,
        CommandHistory history)
    {
        this.grid = grid;
        this.raycaster = raycaster;
        this.solver = solver;
        this.spawner = spawner;
        this.rules = rules;
        this.history = history;
    }

    public bool HandleMoveInput(BuildState state, bool debugLogs = false)
    {
        if (state == null)
            return false;

        if (!state.HasMoveTarget)
        {
            return TrySelectMoveTarget(state, debugLogs);
        }

        return TryCommitMove(state, debugLogs);
    }

    public bool TrySelectMoveTarget(BuildState state, bool debugLogs = false)
    {
        if (state == null)
            return false;

        if (!raycaster.TryGetRemoveTarget(out var target))
            return false;

        if (target == null)
            return false;

        state.BeginMove(target);

        if (debugLogs)
            Debug.Log($"Move target selected: {target.name} origin={target.OriginCell}");

        return true;
    }

    public bool TryCommitMove(BuildState state, bool debugLogs = false)
    {
        if (state == null || !state.HasMoveTarget)
            return false;

        var targetBI = state.MoveTarget;
        if (targetBI == null)
        {
            state.CancelMove();
            return false;
        }

        if (!raycaster.RaycastForBlock(out var hit))
            return false;

        if (!solver.TrySolveOriginCell(hit, targetBI.SizeXYZ, out var toCell))
            return false;

        var cmd = new MoveCommand(grid, spawner, rules, targetBI, toCell, "Move Block");
        bool ok = history.Do(cmd, debugLogs);

        if (!ok && debugLogs)
            Debug.Log("Move failed.");

        state.CancelMove();
        return ok;
    }

    public void CancelMove(BuildState state)
    {
        if (state == null) return;
        state.CancelMove();
    }
}