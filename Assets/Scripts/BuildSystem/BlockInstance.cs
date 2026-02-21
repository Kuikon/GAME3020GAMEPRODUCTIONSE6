using UnityEngine;

public class BlockInstance : MonoBehaviour
{
    public int ObjectID { get; private set; }
    public Vector3Int OriginCell { get; private set; }
    public Vector3Int SizeXYZ { get; private set; }    

    public void Setup(int objectID, Vector3Int originCell, Vector3Int sizeXYZ)
    {
        ObjectID = objectID;
        OriginCell = originCell;
        SizeXYZ = sizeXYZ;
    }
}
