using UnityEngine;

public class BlockInstance : MonoBehaviour
{
    public Vector3Int Cell { get; private set; }

    public void SetCell(Vector3Int cell)
    {
        Cell = cell;
    }
}
