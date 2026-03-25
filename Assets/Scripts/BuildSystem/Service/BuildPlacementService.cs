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
    private readonly DroneCompanionController drone;
    public BuildPlacementService(
        GridManager grid,
        BuildRaycaster raycaster,
        BuildPlacementSolver solver,
        BuildSpawner spawner,
        BuildPlacementRules rules,
        ObjectsDatabaseSO database,
        CommandHistory history,
        DroneCompanionController drone = null)
    {
        this.grid = grid;
        this.raycaster = raycaster;
        this.solver = solver;
        this.spawner = spawner;
        this.rules = rules;
        this.database = database;
        this.history = history;
        this.drone = drone;
    }

    public bool HandlePlaceInput(
        BuildState state,
        ObjectData data,
        bool debugLogs,
        out GameObject spawnedObject)
    {
        spawnedObject = null;

        if (state == null || data == null || data.Prefab == null)
            return false;

        Quaternion rotation = state.CurrentRotation;
        Vector3Int rotatedSize = solver.GetRotatedSize(data.SizeXYZ, rotation);

        if (!solver.TryGetHoverCell(raycaster, rotatedSize, out Vector3Int hoverCell, out RaycastHit hit))
        {
            Log(debugLogs, "[BuildPlacementService] HandlePlaceInput: hover cell not found.");
            return false;
        }

        if (state.PlaceTool == BuildController.PlaceToolMode.Single)
            return TryPlaceSingle(state, data, hoverCell, rotation, debugLogs, out spawnedObject);

        if (state.PlaceTool == BuildController.PlaceToolMode.Line)
            return TryPlaceLine(state, data, hoverCell, rotation, rotatedSize, debugLogs, out spawnedObject);

        return false;
    }

    private bool TryPlaceSingle(
        BuildState state,
        ObjectData data,
        Vector3Int hoverCell,
        Quaternion rotation,
        bool debugLogs,
        out GameObject spawnedObject)
    {
        spawnedObject = null;

        var cmd = new PlaceCommand(
            grid,
            spawner,
            rules,
            hoverCell,
            data,
            rotation,
            database,
            drone);

        bool ok = history.Do(cmd, debugLogs);
        if (!ok)
        {
            Log(debugLogs, $"[BuildPlacementService] TryPlaceSingle failed at {hoverCell}");
            return false;
        }

        spawnedObject = cmd.SpawnedObject;
        return spawnedObject != null;
    }

    private bool TryPlaceLine(
     BuildState state,
     ObjectData data,
     Vector3Int hoverCell,
     Quaternion rotation,
     Vector3Int rotatedSize,
     bool debugLogs,
     out GameObject spawnedObject)
    {
        spawnedObject = null;

        if (!rules.CanUseLineTool(data, out var reason))
        {
            Log(debugLogs, $"[BuildPlacementService] TryPlaceLine denied: {reason}");
            return false;
        }

        if (!state.HasLineStart)
        {
            state.BeginLine(hoverCell);
            Log(debugLogs, $"[BuildPlacementService] Line start set: {hoverCell}");
            return false;
        }

        if (!solver.TryGetLineCellsOrthogonal(
                state.LineStartCell,
                hoverCell,
                rotatedSize,
                out List<Vector3Int> lineCells) ||
            lineCells == null ||
            lineCells.Count == 0)
        {
            Log(debugLogs, "[BuildPlacementService] TryPlaceLine: line cells solve failed.");
            state.CancelLine();
            return false;
        }

        // Åö Ç±Ç±Ç©ÇÁïœçX
        var composite = new CompositeCommand("Place Line");

        GameObject firstSpawned = null;
        int placedCount = 0;

        foreach (var cell in lineCells)
        {
            var cmd = new PlaceCommand(
                grid,
                spawner,
                rules,
                cell,
                data,
                rotation,
                database,
                drone);

            composite.Add(cmd);
        }

        bool ok = history.Do(composite, debugLogs);

        if (!ok)
        {
            Log(debugLogs, "[BuildPlacementService] Line placement failed (composite).");
            state.CancelLine();
            return false;
        }

        // Executeå„Ç…SpawnedObjectÇèEÇ§
        foreach (var cell in lineCells)
        {
            if (rules.TryGetObjectAtCell(cell, out var bi) && bi != null)
            {
                firstSpawned = bi.gameObject;
                break;
            }
        }

        placedCount = lineCells.Count;

        state.CancelLine();

        if (placedCount <= 0)
        {
            Log(debugLogs, "[BuildPlacementService] TryPlaceLine: nothing placed.");
            return false;
        }

        spawnedObject = firstSpawned;
        return spawnedObject != null;
    }

    private void Log(bool debugLogs, string msg)
    {
        if (debugLogs && !string.IsNullOrEmpty(msg))
            Debug.Log(msg);
    }
}