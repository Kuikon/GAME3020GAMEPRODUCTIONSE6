using UnityEngine;

public sealed class PlaceCommand : IBuildCommand
{
    private readonly BuildContext context;
    private readonly Vector3Int originCell;
    private readonly ObjectData data;
    private readonly Quaternion rotation;
    private readonly Vector3Int rotatedSize;

    private GameObject spawned;

    public GameObject SpawnedObject => spawned;
    public string Name => $"Place {data?.Name} @ {originCell}";

    public PlaceCommand(
        BuildContext context,
        Vector3Int originCell,
        ObjectData data,
        Quaternion rotation)
    {
        this.context = context;
        this.originCell = originCell;
        this.data = data;
        this.rotation = rotation;
        this.rotatedSize = context != null && context.Solver != null
            ? context.Solver.GetRotatedSize(data != null ? data.SizeXYZ : Vector3Int.one, rotation)
            : (data != null ? data.SizeXYZ : Vector3Int.one);
    }

    public bool Do(bool debugLogs = false)
    {
        if (context == null || data == null || data.Prefab == null)
            return false;

        if (!context.Rules.CanPlaceObject(data, context.Database, originCell, rotatedSize, out string reason))
        {
            if (debugLogs)
                Debug.Log($"[PlaceCommand] CanPlaceObject failed @ {originCell} reason={reason}");
            return false;
        }

        spawned = context.Spawner.Spawn(context.Grid, originCell, data, rotation);
        if (spawned == null)
        {
            if (debugLogs)
                Debug.Log("[PlaceCommand] Spawn failed.");
            return false;
        }

        RegisterSpawned(spawned);
        context.Drone?.PlayBuild(spawned);
        return true;
    }

    public void Undo(bool debugLogs = false)
    {
        if (spawned == null)
            return;

        UnregisterSpawned();

        if (debugLogs)
            Debug.Log($"[PlaceCommand] Undo @ {originCell}");
    }

    private void RegisterSpawned(GameObject go)
    {
        if (go == null)
            return;

        BlockInstance block = go.GetComponent<BlockInstance>();
        if (block == null)
            block = go.AddComponent<BlockInstance>();

        block.Initialize(data.ID, originCell, rotatedSize, rotation);
        context.Rules.RegisterObjectCells(originCell, rotatedSize, block);
    }

    private void UnregisterSpawned()
    {
        if (spawned == null)
            return;

        BlockInstance block = spawned.GetComponent<BlockInstance>();
        if (block == null)
            return;

        context.Rules.RemoveObjectCells(block.OriginCell, block.SizeXYZ);
    }
}