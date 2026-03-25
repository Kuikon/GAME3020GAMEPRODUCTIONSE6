using UnityEngine;

public class RemoveCommand : IBuildCommand
{
    private readonly GridManager grid;
    private readonly BuildSpawner spawner;
    private readonly BuildPlacementRules rules;
    private readonly ObjectsDatabaseSO database;
    private readonly Vector3Int anyCell;
    private readonly DroneCompanionController drone;

    private int removedID;
    private Vector3Int removedOrigin;
    private Vector3Int removedSize;
    private Quaternion removedRot;

    private GameObject removingObject;
    private bool hasSnapshot;

    public string Name => $"Remove @ {anyCell}";

    public RemoveCommand(
        GridManager grid,
        BuildSpawner spawner,
        BuildPlacementRules rules,
        ObjectsDatabaseSO database,
        Vector3Int anyCell,
        DroneCompanionController drone = null)
    {
        this.grid = grid;
        this.spawner = spawner;
        this.rules = rules;
        this.database = database;
        this.anyCell = anyCell;
        this.drone = drone;
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

        removingObject = bi.gameObject;

        removedID = bi.ObjectID;
        removedOrigin = bi.OriginCell;
        removedSize = bi.SizeXYZ;
        removedRot = bi.Rotation;
        hasSnapshot = true;

        rules.RemoveObjectCells(removedOrigin, removedSize);

        DisableColliders(removingObject);

        if (drone != null)
            drone.PlayRemove(removingObject.transform);

        GameObject target = removingObject;
        BuildEffectUtility.PlayDestroyEffect(target, () =>
        {
            if (drone != null)
                drone.SetIdle();

            if (target != null)
                Object.Destroy(target);

            if (removingObject == target)
                removingObject = null;
        });

        return true;
    }

    public void Undo()
    {
        if (grid == null || spawner == null || rules == null || database == null)
            return;

        if (!hasSnapshot)
            return;

        if (!database.TryGetByID(removedID, out var data) || data == null || data.Prefab == null)
            return;

        if (!rules.CanPlace(removedOrigin, removedSize, out _))
            return;

        var obj = spawner.Spawn(grid, removedOrigin, data, removedRot);
        if (obj == null)
            return;

        var bi = obj.GetComponent<BlockInstance>();
        if (bi == null)
        {
            Object.Destroy(obj);
            return;
        }

        rules.RegisterObjectCells(bi.OriginCell, bi.SizeXYZ, bi);

        if (drone != null)
            drone.PlayBuild(obj);

        BuildEffectUtility.PlayBuildEffect(obj);

        removingObject = null;
    }

    private void DisableColliders(GameObject obj)
    {
        if (obj == null)
            return;

        var colliders = obj.GetComponentsInChildren<Collider>();
        for (int i = 0; i < colliders.Length; i++)
            colliders[i].enabled = false;
    }
}