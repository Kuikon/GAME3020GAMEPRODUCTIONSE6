using UnityEngine;

public class PlaceCommand : IBuildCommand
{
    private readonly GridManager grid;
    private readonly BuildSpawner spawner;
    private readonly BuildPlacementRules rules;
    private readonly ObjectsDatabaseSO database;
    private readonly DroneCompanionController drone;

    private readonly Vector3Int originCell;
    private readonly ObjectData data;
    private readonly Quaternion rotation;
    private readonly Vector3Int rotatedSize;

    private GameObject spawned;

    public GameObject SpawnedObject => spawned;

    public string Name => $"Place {data?.Name} (ID={data?.ID}) @ {originCell} rot={rotation.eulerAngles} size={rotatedSize}";

    public PlaceCommand(
        GridManager grid,
        BuildSpawner spawner,
        BuildPlacementRules rules,
        Vector3Int originCell,
        ObjectData data,
        Quaternion rotation,
        ObjectsDatabaseSO database,
        DroneCompanionController drone = null)
    {
        this.grid = grid;
        this.spawner = spawner;
        this.rules = rules;
        this.originCell = originCell;
        this.data = data;
        this.rotation = rotation;
        this.database = database;
        this.drone = drone;

        rotatedSize = GetRotatedSize(data != null ? data.SizeXYZ : Vector3Int.one, rotation);
    }

    public bool Execute()
    {
        if (grid == null || spawner == null || rules == null || database == null || data == null || data.Prefab == null)
            return false;

        if (!rules.CanPlaceObject(data, database, originCell, rotatedSize, out var reason))
        {
            Debug.Log($"[PlaceCommand] Execute denied: {reason}");
            return false;
        }

        spawned = spawner.Spawn(grid, originCell, data, rotation);
        if (spawned == null)
            return false;

        var bi = spawned.GetComponent<BlockInstance>();
        if (bi == null)
        {
            Object.Destroy(spawned);
            spawned = null;
            return false;
        }

        rules.RegisterObjectCells(bi.OriginCell, bi.SizeXYZ, bi);

        if (drone != null)
            drone.PlayBuild(spawned);

        BuildEffectUtility.PlayBuildEffect(spawned);

        return true;
    }

    public void Undo()
    {
        if (spawned == null)
            return;

        GameObject target = spawned;
        var bi = target.GetComponent<BlockInstance>();

        if (bi != null)
            rules.RemoveObjectCells(bi.OriginCell, bi.SizeXYZ);
        else
            rules.RemoveObjectCells(originCell, rotatedSize);

        DisableColliders(target);

        if (drone != null)
            drone.PlayRemove(target.transform);

        BuildEffectUtility.PlayDestroyEffect(target, () =>
        {
            if (drone != null)
                drone.SetIdle();

            if (target != null)
                Object.Destroy(target);
        });

        spawned = null;
    }

    private void DisableColliders(GameObject obj)
    {
        if (obj == null)
            return;

        var colliders = obj.GetComponentsInChildren<Collider>();
        for (int i = 0; i < colliders.Length; i++)
            colliders[i].enabled = false;
    }

    private Vector3Int GetRotatedSize(Vector3Int originalSize, Quaternion rot)
    {
        float y = Mathf.Round(rot.eulerAngles.y) % 360f;

        if (Mathf.Approximately(y, 90f) || Mathf.Approximately(y, 270f))
            return new Vector3Int(originalSize.z, originalSize.y, originalSize.x);

        return originalSize;
    }
}