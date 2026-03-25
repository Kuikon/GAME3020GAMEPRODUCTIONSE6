using UnityEngine;

public class BuildRemoveService
{
    private readonly GridManager grid;
    private readonly BuildSpawner spawner;
    private readonly BuildPlacementRules rules;
    private readonly ObjectsDatabaseSO database;
    private readonly CommandHistory history;
    private readonly DroneCompanionController drone;

    public BuildRemoveService(
        GridManager grid,
        BuildSpawner spawner,
        BuildPlacementRules rules,
        ObjectsDatabaseSO database,
        CommandHistory history,
        DroneCompanionController drone = null)
    {
        this.grid = grid;
        this.spawner = spawner;
        this.rules = rules;
        this.database = database;
        this.history = history;
        this.drone = drone;
    }

    public bool TryRemoveAtCell(Vector3Int anyCell, bool debugLogs = false)
    {
        var cmd = new RemoveCommand(
            grid,
            spawner,
            rules,
            database,
            anyCell,
            drone);

        return history.Do(cmd, debugLogs);
    }

    public bool TryRemoveBlock(BlockInstance target, bool debugLogs = false)
    {
        if (target == null)
            return false;

        return TryRemoveAtCell(target.OriginCell, debugLogs);
    }
}