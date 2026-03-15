using System.Collections.Generic;
using UnityEngine;

public class BuildPlacementService
{
    private readonly GridManager grid;
    private readonly BuildRaycaster raycaster;
    private readonly BuildPlacementSolver solver;
    private readonly BuildSpawner spawner;
    private readonly BuildPlacementRules rules;
    private readonly ObjectsDatabaseSO database;
    private readonly CommandHistory history;

    public BuildPlacementService(
        GridManager grid,
        BuildRaycaster raycaster,
        BuildPlacementSolver solver,
        BuildSpawner spawner,
        BuildPlacementRules rules,
        ObjectsDatabaseSO database,
        CommandHistory history)
    {
        this.grid = grid;
        this.raycaster = raycaster;
        this.solver = solver;
        this.spawner = spawner;
        this.rules = rules;
        this.database = database;
        this.history = history;
    }

    // --------------------------------------------------
    // Entry
    // --------------------------------------------------
    public bool HandlePlaceInput(BuildState state, ObjectData data, bool debugLogs = false)
    {
        if (state == null || data == null)
            return false;

        Quaternion rot = state.CurrentRotation;
        Vector3Int rotatedSize = solver.GetRotatedSize(data.SizeXYZ, rot);

        if (!solver.TryGetHoverCell(raycaster, rotatedSize, out var hoverCell, out _))
            return false;

        if (state.PlaceTool == BuildController.PlaceToolMode.Single)
            return TryPlaceSingle(hoverCell, data, rot, debugLogs);

        return TryHandleLinePlace(state, hoverCell, data, rot, rotatedSize, debugLogs);
    }

    // --------------------------------------------------
    // Single
    // --------------------------------------------------
    public bool TryPlaceSingle(
        Vector3Int originCell,
        ObjectData data,
        Quaternion rot,
        bool debugLogs = false)
    {
        if (data == null)
            return false;

        Vector3Int rotatedSize = solver.GetRotatedSize(data.SizeXYZ, rot);

        if (!rules.CanPlaceObject(data, database, originCell, rotatedSize, out var reason))
        {
            if (debugLogs && !string.IsNullOrEmpty(reason))
                Debug.Log($"[PlaceSingle] Denied: {reason}");
            return false;
        }

        var cmd = new PlaceCommand(grid, spawner, rules, originCell, data, rot, database);
        return history.Do(cmd, debugLogs);
    }

    // --------------------------------------------------
    // Line
    // --------------------------------------------------
    public bool TryHandleLinePlace(
        BuildState state,
        Vector3Int currentCell,
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
                Debug.Log($"[LinePlace] Denied: {reason}");
            return false;
        }

        // 1回目クリック: 始点だけ保存
        if (!state.HasLineStart)
        {
            state.BeginLine(currentCell);

            if (debugLogs)
                Debug.Log($"[LinePlace] Start: {state.LineStartCell}");

            return true;
        }

        // 2回目クリック: ライン配置
        if (!solver.TryGetLineCellsOrthogonal(state.LineStartCell, currentCell, rotatedSize, out var lineCells) ||
            lineCells == null || lineCells.Count == 0)
        {
            if (debugLogs)
                Debug.Log("[LinePlace] Failed to calculate line cells.");

            state.CancelLine();
            return false;
        }

        var group = BuildLinePlaceCommand(lineCells, data, rot);

        bool ok = history.Do(group, debugLogs);

        if (!ok && debugLogs)
            Debug.Log("[LinePlace] Composite place failed. Rolled back.");

        state.CancelLine();
        return ok;
    }

    // --------------------------------------------------
    // Helper
    // --------------------------------------------------
    private CompositeCommand BuildLinePlaceCommand(
        List<Vector3Int> lineCells,
        ObjectData data,
        Quaternion rot)
    {
        var group = new CompositeCommand($"Line Place {data.Name}");

        foreach (var cell in lineCells)
        {
            group.Add(new PlaceCommand(grid, spawner, rules, cell, data, rot, database));
        }

        return group;
    }
}