using UnityEngine;

public class PlaceCommand : IBuildCommand
{
    private readonly GridManager grid;
    private readonly BuildSpawner spawner;
    private readonly BuildPlacementRules rules;

    private readonly Vector3Int originCell;
    private readonly ObjectData data;
    private readonly Quaternion rotation;
    private readonly Vector3Int rotatedSize;

    private GameObject spawned;

    public string Name => $"Place {data?.Name} (ID={data?.ID}) @ {originCell} rot={rotation.eulerAngles} size={rotatedSize}";

    public PlaceCommand(
        GridManager grid,
        BuildSpawner spawner,
        BuildPlacementRules rules,
        Vector3Int originCell,
        ObjectData data,
        Quaternion rotation)
    {
        this.grid = grid;
        this.spawner = spawner;
        this.rules = rules;
        this.originCell = originCell;
        this.data = data;
        this.rotation = rotation;
        this.rotatedSize = GetRotatedSize(data != null ? data.SizeXYZ : Vector3Int.one, rotation);
    }

    public bool Execute()
    {
        if (grid == null || spawner == null || rules == null || data == null || data.Prefab == null)
            return false;

        // âÒì]å„ÉTÉCÉYÇ≈îªíË
        if (!rules.CanPlace(originCell, rotatedSize, out _))
            return false;

        spawned = spawner.Spawn(grid, originCell, data, rotation);
        if (spawned == null) return false;

        var bi = spawned.GetComponent<BlockInstance>();
        if (bi == null) return false;

        // âÒì]å„ÉTÉCÉYÇ≈ìoò^
        rules.RegisterObjectCells(originCell, rotatedSize, bi);
        return true;
    }

    public void Undo()
    {
        if (spawned == null) return;

        var bi = spawned.GetComponent<BlockInstance>();

        if (bi != null)
            rules.RemoveObjectCells(bi.OriginCell, bi.SizeXYZ);
        else
            rules.RemoveObjectCells(originCell, rotatedSize);

        Object.Destroy(spawned);
        spawned = null;
    }

    private Vector3Int GetRotatedSize(Vector3Int originalSize, Quaternion rot)
    {
        float y = Mathf.Round(rot.eulerAngles.y) % 360f;

        if (Mathf.Approximately(y, 90f) || Mathf.Approximately(y, 270f))
            return new Vector3Int(originalSize.z, originalSize.y, originalSize.x);

        return originalSize;
    }
}