using UnityEngine;

public class PlaceCommand : IBuildCommand
{
    private readonly GridManager grid;
    private readonly BuildSpawner spawner;
    private readonly BuildPlacementRules rules;

    private readonly Vector3Int originCell;
    private readonly ObjectData data;
    private readonly Quaternion rotation;

    private GameObject spawned;

    public string Name => $"Place {data?.Name} (ID={data?.ID}) @ {originCell}";

    public PlaceCommand(GridManager grid, BuildSpawner spawner, BuildPlacementRules rules,
                        Vector3Int originCell, ObjectData data, Quaternion rotation)
    {
        this.grid = grid;
        this.spawner = spawner;
        this.rules = rules;
        this.originCell = originCell;
        this.data = data;
        this.rotation = rotation;
    }

    public bool Execute()
    {
        if (grid == null || spawner == null || rules == null || data == null || data.Prefab == null)
            return false;

        if (!rules.CanPlace(originCell, data.SizeXYZ, out _))
            return false;

        spawned = spawner.Spawn(grid, originCell, data, rotation);
        if (spawned == null) return false;

        rules.RegisterObjectCells(originCell, data.SizeXYZ, spawned);
        return true;
    }

    public void Undo()
    {
        if (spawned == null) return;

        var bi = spawned.GetComponent<BlockInstance>();
        if (bi != null) rules.RemoveObjectCells(bi.OriginCell, bi.SizeXYZ);
        else rules.RemoveObjectCells(originCell, data.SizeXYZ);

        Object.Destroy(spawned);
        spawned = null;
    }
}