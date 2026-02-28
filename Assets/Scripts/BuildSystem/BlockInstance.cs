using UnityEngine;

public class BlockInstance : MonoBehaviour
{
    [field: SerializeField] public int ObjectID { get; private set; }
    public Vector3Int OriginCell { get; private set; }
    public Vector3Int SizeXYZ { get; private set; }
    public Quaternion Rotation { get; private set; } = Quaternion.identity;
    public void Setup(int objectID, Vector3Int originCell, Vector3Int sizeXYZ, Quaternion rotation)
    {
        ObjectID = objectID;
        OriginCell = originCell;
        SizeXYZ = sizeXYZ;
        Rotation = rotation;
    }
}
