using UnityEngine;

public class BuildSpawner
{
    public GameObject Spawn(GridManager grid, Vector3Int originCell, ObjectData data)
    {
        Vector3 desiredCenter = grid.BoxToWorldCenter(originCell, data.SizeXYZ);

        GameObject obj = Object.Instantiate(data.Prefab, desiredCenter, Quaternion.identity);
        obj.name = $"{data.Name}_ID{data.ID}_{originCell.x}_{originCell.y}_{originCell.z}";

        ForceBlockLayerOnAllChildren(obj);

        // PivotÇ∏ÇÍï‚ê≥ÅFCollideríÜêSÇ desiredCenter Ç…çáÇÌÇπÇÈ
        var col = obj.GetComponentInChildren<Collider>(true);
        if (col != null)
        {
            Vector3 delta = desiredCenter - col.bounds.center;
            obj.transform.position += delta;
        }

        var bi = obj.GetComponent<BlockInstance>();
        if (bi == null) bi = obj.AddComponent<BlockInstance>();
        bi.Setup(data.ID, originCell, data.SizeXYZ);

        return obj;
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