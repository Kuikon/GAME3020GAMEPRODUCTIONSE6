using UnityEngine;

public class BuildSpawner
{
    private readonly Transform parent;

    public BuildSpawner(Transform parent = null)
    {
        this.parent = parent;
    }

    public GameObject Spawn(GridManager grid, Vector3Int originCell, ObjectData data, Quaternion rotation)
    {
        if (grid == null || data == null || data.Prefab == null)
            return null;

        Vector3Int rotatedSize = GetRotatedSize(data.SizeXYZ, rotation);
        Vector3 desiredCenter = grid.BoxToWorldCenter(originCell, rotatedSize);

        GameObject obj = Object.Instantiate(data.Prefab, desiredCenter, rotation, parent);
        obj.name = $"{data.Name}_ID{data.ID}_{originCell.x}_{originCell.y}_{originCell.z}";

        ForceBlockLayerOnAllChildren(obj);

        var bi = obj.GetComponent<BlockInstance>();
        if (bi == null) bi = obj.AddComponent<BlockInstance>();

        bi.Setup(data.ID, originCell, rotatedSize, rotation);

        return obj;
    }

    public void MoveExisting(GridManager grid, BlockInstance target, Vector3Int originCell, Vector3Int sizeXYZ, Quaternion rot)
    {
        if (grid == null || target == null) return;

        Vector3Int rotatedSize = GetRotatedSize(sizeXYZ, rot);
        Vector3 world = grid.BoxToWorldCenter(originCell, rotatedSize);

        target.transform.position = world;
        target.transform.rotation = rot;

        // BlockInstance ÇÃï€éùèÓïÒÇ‡çXêVÇµÇΩÇ¢Ç»ÇÁ
        target.Setup(target.ObjectID, originCell, rotatedSize, rot);
    }

    private Vector3Int GetRotatedSize(Vector3Int originalSize, Quaternion rot)
    {
        float y = Mathf.Round(rot.eulerAngles.y) % 360f;

        if (Mathf.Approximately(y, 90f) || Mathf.Approximately(y, 270f))
            return new Vector3Int(originalSize.z, originalSize.y, originalSize.x);

        return originalSize;
    }

    private void ForceBlockLayerOnAllChildren(GameObject obj)
    {
        int blockLayer = LayerMask.NameToLayer("Block");
        if (blockLayer < 0) return;

        obj.layer = blockLayer;
        foreach (Transform t in obj.GetComponentsInChildren<Transform>(true))
            t.gameObject.layer = blockLayer;
    }
}