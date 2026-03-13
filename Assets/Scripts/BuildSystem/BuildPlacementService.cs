using UnityEngine;

public class BuildPlacementService
{
    private readonly GridManager grid;
    private readonly BuildRaycaster raycaster;
    private readonly BuildPlacementSolver solver;
    private readonly BuildSpawner spawner;
    private readonly BuildPlacementRules rules;
    private readonly CommandHistory history;

    public BuildPlacementService(
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

    /// <summary>
    /// Place input 1回分を処理する。
    /// Single ならその場で配置。
    /// Line なら「開始」または「確定」。
    /// </summary>
    public bool HandlePlaceInput(BuildState state, ObjectData data, bool debugLogs = false)
    {
        if (state == null || data == null)
            return false;

        Quaternion rot = state.CurrentRotation;
        Vector3Int rotatedSize = solver.GetRotatedSize(data.SizeXYZ, rot);

        if (!solver.TryGetHoverCell(raycaster, rotatedSize, out var cell, out _))
            return false;

        if (state.PlaceTool == BuildController.PlaceToolMode.Single)
        {
            return TryPlaceSingle(cell, data, rot, rotatedSize, debugLogs);
        }

        return HandleLinePlace(state, cell, data, rot, rotatedSize, debugLogs);
    }

    public bool TryPlaceSingle(
        Vector3Int originCell,
        ObjectData data,
        Quaternion rot,
        Vector3Int rotatedSize,
        bool debugLogs = false)
    {
        if (data == null)
            return false;

        if (!rules.CanPlace(originCell, rotatedSize, out _))
            return false;

        var cmd = new PlaceCommand(grid, spawner, rules, originCell, data, rot);
        return history.Do(cmd, debugLogs);
    }

    public bool HandleLinePlace(
        BuildState state,
        Vector3Int cell,
        ObjectData data,
        Quaternion rot,
        Vector3Int rotatedSize,
        bool debugLogs = false)
    {
        if (state == null || data == null)
            return false;

        if (!rules.CanUseLineTool(data, out var reason))
        {
            if (debugLogs && !string.IsNullOrEmpty(reason))
                Debug.Log(reason);
            return false;
        }

        // 1回目クリック: 開始点だけ記録
        if (!state.HasLineStart)
        {
            state.BeginLine(cell);

            if (debugLogs)
                Debug.Log($"Line start: {state.LineStartCell}");

            return true;
        }

        // 2回目クリック: 線を確定
        if (!solver.TryGetLineCellsOrthogonal(state.LineStartCell, cell, rotatedSize, out var lineCells) ||
            lineCells == null || lineCells.Count == 0)
        {
            state.CancelLine();
            return false;
        }

        var group = new CompositeCommand($"Line Place {data.Name}");

        foreach (var c in lineCells)
        {
            group.Add(new PlaceCommand(grid, spawner, rules, c, data, rot));
        }

        bool ok = history.Do(group, debugLogs);

        if (!ok && debugLogs)
            Debug.Log("Line place failed (rolled back).");

        state.CancelLine();
        return ok;
    }
}