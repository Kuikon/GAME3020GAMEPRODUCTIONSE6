using UnityEngine;

public class LevelSerializer
{
    // Scene(placedRoot) Å® LevelData
    public LevelData Capture(string levelId, Transform placedRoot)
    {
        var data = new LevelData { levelId = levelId };

        if (!placedRoot) return data;

        var instances = placedRoot.GetComponentsInChildren<BlockInstance>(true);
        foreach (var bi in instances)
        {
            if (!bi) continue;

            data.blocks.Add(new BlockRecord
            {
                objectId = bi.ObjectID,
                originCell = bi.OriginCell,
                euler = bi.transform.rotation.eulerAngles,
                color = bi.Color
            });
        }

        return data;
    }

    // LevelData Å® Scene(placedRoot)
    public void Apply(
        LevelData data,
        Transform placedRoot,
        GridManager grid,
        ObjectsDatabaseSO db,
        BuildSpawner spawner,
        BuildPlacementRules rules)
    {
        if (!placedRoot)
        {
            Debug.LogError("[Apply] placedRoot is null");
            return;
        }

        if (data == null) Debug.LogError("[Apply] data is null");
        if (db == null) Debug.LogError("[Apply] db is null");
        if (grid == null) Debug.LogError("[Apply] grid is null");
        if (spawner == null) Debug.LogError("[Apply] spawner is null");
        if (rules == null) Debug.LogError("[Apply] rules is null");

        if (data == null || db == null || grid == null || spawner == null || rules == null)
            return;

        rules.ClearAll();

        for (int i = placedRoot.childCount - 1; i >= 0; i--)
            Object.DestroyImmediate(placedRoot.GetChild(i).gameObject);

        foreach (var r in data.blocks)
        {
            if (!db.TryGetByID(r.objectId, out var obj) || obj == null)
                continue;

            Quaternion rot = Quaternion.Euler(r.euler);

            Vector3Int rotatedSize = GetRotatedSize(obj.SizeXYZ, rot);

            if (!rules.CanPlace(r.originCell, rotatedSize, out _))
                continue;

            GameObject spawned = spawner.Spawn(grid, r.originCell, obj, rot, r.color);
            if (spawned == null)
                continue;

            var bi = spawned.GetComponent<BlockInstance>();
            if (bi == null)
                continue;

            rules.RegisterObjectCells(r.originCell, rotatedSize, bi);
        }
    }

    private Vector3Int GetRotatedSize(Vector3Int size, Quaternion rotation)
    {
        Vector3 euler = rotation.eulerAngles;
        int y = Mathf.RoundToInt(euler.y) % 360;
        if (y < 0) y += 360;
        if (y == 90 || y == 270)
            return new Vector3Int(size.z, size.y, size.x);

        return size;
    }
}