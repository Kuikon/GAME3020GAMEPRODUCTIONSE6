using UnityEngine;

public class BlockInstance : MonoBehaviour
{
    [field: SerializeField] public int ObjectID { get; private set; }
    public Vector3Int OriginCell { get; private set; }
    public Vector3Int SizeXYZ { get; private set; }
    public Quaternion Rotation { get; private set; } = Quaternion.identity;
    //Initialize the BlockInstance with th value needed to spawn the placed object
    public void Setup(int objectID, Vector3Int originCell, Vector3Int sizeXYZ, Quaternion rotation)
    {
        ObjectID = objectID;
        OriginCell = originCell;
        SizeXYZ = sizeXYZ;
        Rotation = rotation;
    }
    //Toggle between mesh and box collider based on game mode
    public void ApplyColliderMode(bool isPlay, ObjectsDatabaseSO db)
    {
        if (db == null) return;
        if (!db.TryGetByID(ObjectID, out var data) || data == null) return;

        bool enableBox = !isPlay || data.IsBoxShape;
        var boxes = GetComponentsInChildren<BoxCollider>(true);
        foreach (var b in boxes)
            if (b) b.enabled = enableBox;
        var meshes = GetComponentsInChildren<MeshCollider>(true);
        foreach (var m in meshes)
            if (m) m.enabled = (isPlay && !data.IsBoxShape);
       
    }
}
