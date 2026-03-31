using UnityEngine;

public class BlockInstance : MonoBehaviour
{
    [field: SerializeField] public int ObjectID { get; private set; }
    [field: SerializeField] public BlockColor Color { get; private set; } = BlockColor.Blue;

    public Vector3Int OriginCell { get; private set; }
    public Vector3Int SizeXYZ { get; private set; }
    public Quaternion Rotation { get; private set; } = Quaternion.identity;

    public void Setup(
        int objectID,
        Vector3Int originCell,
        Vector3Int sizeXYZ,
        Quaternion rotation,
        BlockColor color)
    {
        ObjectID = objectID;
        OriginCell = originCell;
        SizeXYZ = sizeXYZ;
        Rotation = rotation;
        Color = color;
    }

    public void Initialize(
        int objectID,
        Vector3Int originCell,
        Vector3Int sizeXYZ,
        Quaternion rotation,
        BlockColor color)
    {
        Setup(objectID, originCell, sizeXYZ, rotation, color);
    }

    public void SetOriginCell(Vector3Int originCell)
    {
        OriginCell = originCell;
    }

    public void SetRotation(Quaternion rotation)
    {
        Rotation = rotation;
    }

    public void SetColor(BlockColor color)
    {
        Color = color;
    }

    public void ApplyColliderMode(bool isPlay, ObjectsDatabaseSO db)
    {
        if (db == null)
            return;

        if (!db.TryGetByID(ObjectID, out var data) || data == null)
            return;

        bool enableBox = !isPlay || data.IsBoxShape;

        var boxes = GetComponentsInChildren<BoxCollider>(true);
        foreach (var b in boxes)
        {
            if (b != null)
                b.enabled = enableBox;
        }

        var meshes = GetComponentsInChildren<MeshCollider>(true);
        foreach (var m in meshes)
        {
            if (m != null)
                m.enabled = isPlay && !data.IsBoxShape;
        }
    }
}