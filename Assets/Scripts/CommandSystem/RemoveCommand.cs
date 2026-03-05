using UnityEngine;

public class RemoveCommand : IBuildCommand
{
    private readonly GridManager grid;
    private readonly BuildSpawner spawner;
    private readonly BuildPlacementRules rules;
    private readonly ObjectsDatabaseSO database;
    private readonly BlockInstance directTarget;
    private readonly Vector3Int anyCell;

    private int removedID;
    private Vector3Int removedOrigin;
    private Vector3Int removedSize;
    private Quaternion removedRot;

    public string Name => $"Remove @ {anyCell}";

    public RemoveCommand(GridManager grid, BuildSpawner spawner, BuildPlacementRules rules,
                         ObjectsDatabaseSO database, Vector3Int anyCell)
    {
        this.grid = grid;
        this.spawner = spawner;
        this.rules = rules;
        this.database = database;
        this.anyCell = anyCell;
    }

    public bool Execute()
    {
        if (grid == null || spawner == null || rules == null || database == null)
            return false;

        if (!rules.TryGetObjectAtCell(anyCell, out var obj) || obj == null)
            return false;

        var bi = obj.GetComponent<BlockInstance>();
        if (bi == null)
            return false;


        removedID = bi.ObjectID;
        removedOrigin = bi.OriginCell;
        removedSize = bi.SizeXYZ;
        removedRot = bi.Rotation;


        rules.RemoveObjectCells(removedOrigin, removedSize);
        Object.Destroy(bi.gameObject);

        return true;
    }

    public void Undo()
    {
        if (!database.TryGetByID(removedID, out var data) || data == null || data.Prefab == null)
            return;

        if (!rules.CanPlace(removedOrigin, removedSize, out _))
            return;

        var obj = spawner.Spawn(grid, removedOrigin, data, removedRot);
        var bi = obj.GetComponent<BlockInstance>();
        if (bi == null) return;
        rules.RegisterObjectCells(removedOrigin, removedSize, bi);
    }
}